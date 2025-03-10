﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Portfolio.Backend.Services.Implementation
{
	public class AuthenticationManager(
		DatabaseContext database,
		ICryptoHelper crypto,
		IOptionsSnapshot<AuthenticationConfiguration> authenticationOptions,
		IEmailer emailer,
		IIntruderDetector intruderDetector) : IAuthenticator
	{
		private readonly DatabaseContext _database = database;
		private readonly ICryptoHelper _crypto = crypto;
		private readonly IEmailer _emailer = emailer;
		private readonly AuthenticationConfiguration _authenticationConfig = authenticationOptions.Value;
		private readonly IIntruderDetector _intruderDetector = intruderDetector;

		private DateTime AccessTokenExpiration => DateTime.UtcNow.AddMinutes(_authenticationConfig.AccessTokenExpirationMinutes);

		public TimeSpan? BeginPasswordReset(User user)
		{
			// make sure we have a tracked version of the user
			user = _database.Attach(user).Entity;

			var timeLeft = user.LastPasswordResetRequest + TimeSpan.FromMinutes(_authenticationConfig.PasswordResetCooldown) - DateTimeOffset.Now;

			if (timeLeft > TimeSpan.Zero)
				return timeLeft;

			var resetToken = _crypto.GenerateRandomString(_authenticationConfig.PasswordResetTokenLength, ICryptoHelper.LowerAlphaCharacters + ICryptoHelper.NumericCharacters);

			user.PasswordResetTokenHash = _crypto.Hash(resetToken);
			user.PasswordResetExpiration = DateTimeOffset.UtcNow.AddMinutes(_authenticationConfig.PasswordResetTokenExpirationMinutes);
			user.LastPasswordResetRequest = DateTimeOffset.UtcNow;

			_database.SaveChanges();

			_emailer.SendEmailAsync<IResetPasswordEmailProvider>(user, provider => provider.ResetToken = resetToken);

			return null;
		}

		public bool CompletePasswordReset(User user, string token, string newPassword)
		{
			if (!ValidatePasswordStrength(newPassword))
				return false;

			if (user.PasswordResetTokenHash is not { Length: > 0 })
				return false;

			if (user.PasswordResetExpiration < DateTimeOffset.Now)
			{
				// reset token has expired, so we clear it
				user.PasswordResetTokenHash = [];
				_database.SaveChanges();
				return false;
			}

			if (_crypto.Verify(token, user.PasswordResetTokenHash) == VerificationResult.Failed)
			{
				return false;
			}

			user.PasswordHash = _crypto.Hash(newPassword);

			user.PasswordResetTokenHash = [];
			user.PasswordResetExpiration = default;

			_database.SaveChanges();

			return true;
		}

		public (string accessToken, RefreshTokenData newRefreshToken)? GetAccessToken(string email, uint refreshTokenId, string refreshToken)
		{
			var user = _database.Users.FirstOrDefault(u => u.Email == email);

			if (user is null)
				return null;

			// check if the refresh token exists and is not expired
			var token = user.RefreshTokens.FirstOrDefault(t => t.Id == refreshTokenId);

			if (token is not { ActiveToken: not null } || token.IsExpired())
				return null;

			// verify the tokens match
			switch (_crypto.Verify(refreshToken, token.ActiveToken.TokenHash))
			{
				case VerificationResult.Failed:
					_intruderDetector.EnqueueInvalidAccessTokenUsage(refreshTokenId, refreshToken);
					return null;
				case VerificationResult.Success:
					break;
				case VerificationResult.SuccessRehashNeeded:
					token.ActiveToken.TokenHash = _crypto.Hash(refreshToken);
					_database.SaveChanges();
					break;
			}

			// create the access token
			var jwt = new JwtSecurityToken(
				issuer: _authenticationConfig.Issuer,
				audience: _authenticationConfig.Audience,
				claims: [
					new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
					new Claim(ClaimTypes.Email, user.Email),
					new Claim(ClaimTypes.Name, user.FullName),
					new Claim(CustomClaimTypes.RefreshTokenId, token.Id.ToString())
				],
				// The expiration date is the minimum of the access token expiration and the refresh token expiration
				expires: new DateTime(long.Min(AccessTokenExpiration.Ticks, token.ExpirationDate.Ticks)),
				signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authenticationConfig.GetSecret())), SecurityAlgorithms.HmacSha256)
			);

			// encrypt and write the accessToken
			var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

			// create a new refresh token. This will replace the old one
			var newRefreshToken = GetRefreshToken(user, token);

			return (accessToken, newRefreshToken);
		}

		public RefreshTokenData? GetRefreshToken(string email, string password)
		{
			var user = _database.Users.FirstOrDefault(u => u.Email == email);

			if (user is null)
				return null;

			switch (_crypto.Verify(password, user.PasswordHash))
			{
				case VerificationResult.Failed:
					return null;
				case VerificationResult.Success:
					break;
				case VerificationResult.SuccessRehashNeeded:
					user.PasswordHash = _crypto.Hash(password);
					_database.SaveChanges();
					break;
			}

			return GetRefreshToken(user);
		}

		/// <summary>
		/// Generates a new refresh token for the user. If a token is provided, it will be updated with a new value.
		/// </summary>
		private RefreshTokenData GetRefreshToken(User user, RefreshToken? token = null)
		{
			var expiration = DateTimeOffset.UtcNow.AddHours(_authenticationConfig.RefreshTokenExpirationHours);

			if (token is null)
			{
				token = _database.CreateProxy<RefreshToken>(v => v.Owner = user);

				user.RefreshTokens.Add(token);
			} else
			{
				// make sure we have a tracked version of the token
				token = _database.Attach(token).Entity;
			}

			// Generate a random string which will be the refresh token
			var secretToken = _crypto.GenerateRandomString(_authenticationConfig.RefreshTokenLength, ICryptoHelper.AlphaNumericCharacters);

			var newValue = _database.CreateProxy<RefreshTokenValue>(v =>
			{
				v.TokenHash = _crypto.Hash(secretToken);
				v.ReferringToken = token;
				v.ExpirationDate = expiration;
				v.CreationDate = DateTimeOffset.UtcNow;
			});

			token.NewValue(newValue);

			_database.SaveChanges();

			return (secretToken, token.Id, expiration);
		}

		public IEnumerable<RefreshToken> GetRefreshTokens(User user)
		{
			return user.RefreshTokens;
		}

		/// <summary>
		/// Revokes a refresh token from the user.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the token was revoked, <see langword="false"/> if the token was not found.
		/// </returns>
		public bool RevokeRefreshToken(User owner, uint tokenId)
		{
			var token = owner.RefreshTokens.FirstOrDefault(t => t.Id == tokenId);

			if (token is null)
				return false;

			owner.RefreshTokens.Remove(token);

			_database.SaveChanges();

			return true;
		}

		private static bool ValidatePasswordStrength(ReadOnlySpan<char> password)
		{
			return password.Length > 6 && password.ContainsAnyInRange('a', 'z') && password.ContainsAnyInRange('A', 'Z') && password.ContainsAnyInRange('0', '9');
		}

		public void RevokeAllRefreshTokens(User owner)
		{
			owner.RefreshTokens.Clear();
			_database.SaveChanges();
		}
	}
}
