
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Backend.Controllers.CustomTypedResults;
using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Extensions;
using Portfolio.Backend.Services;
using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthenticationController(IAuthenticator authenticator, IUserService users) : ControllerBase
	{
		private readonly IAuthenticator _authenticator = authenticator;
		private readonly IUserService _users = users;

		const string RefreshTokenCookieName = "jbpf-refresh-token";

		[HttpGet("check")]
		[Authorize]
		public Ok CheckLoggedIn()
		{
			return TypedResults.Ok();
		}

		[HttpGet("refresh")]
		public Results<Ok<CreatedAccessTokenResponse>, UnauthorizedHttpResult> Refresh(string email)
		{
			if (GetRefreshToken() is not (string refreshToken, uint refreshTokenId))
			{
				return TypedResults.Unauthorized();
			}

			if (_authenticator.GetAccessToken(email, refreshTokenId, refreshToken)
					is not (string accessToken, RefreshTokenData newRefreshToken))
			{
				return TypedResults.Unauthorized();
			}

			UpdateRefreshToken(newRefreshToken.token, newRefreshToken.id, newRefreshToken.expiration);

			return TypedResults.Ok(new CreatedAccessTokenResponse(accessToken));
		}
		public record CreatedAccessTokenResponse(string AccessToken);


		[HttpPost("login")]
		public Results<Ok, UnauthorizedHttpResult> Login([FromBody] LoginRequest request)
		{
			var generatedToken = _authenticator.GetRefreshToken(request.Email, request.Password);

			if (generatedToken is not (string token, uint id, DateTimeOffset expiration))
			{
				return TypedResults.Unauthorized();
			}

			UpdateRefreshToken(token, id, expiration);

			return TypedResults.Ok();
		}

		[HttpPost("logout")]
		[Authorize]
		public Results<Ok, UnauthorizedHttpResult> Logout()
		{
			var token = GetRefreshToken();
			var user = HttpContext.GetCurrentUser();

			if (token is not (_, uint tokenId) || user is null)
			{
				return TypedResults.Unauthorized();
			}

			_authenticator.RevokeRefreshToken(user, tokenId);

			HttpContext.Response.Cookies.Delete(RefreshTokenCookieName);

			return TypedResults.Ok();
		}

		private void UpdateRefreshToken(string token, uint id, DateTimeOffset expiration)
		{
			var combinedToken = $"{id}\t{token}";

			HttpContext.Response.Cookies.Append(RefreshTokenCookieName, combinedToken, new CookieOptions
			{
				HttpOnly = true,
				Expires = expiration,
				Secure = true,
				SameSite = SameSiteMode.None,
				Path = "/api/auth/refresh;/api/auth/logout",
				IsEssential = true,
				MaxAge = expiration - DateTimeOffset.Now,
			});
		}

		private (string token, uint tokenId)? GetRefreshToken()
		{
			if (!HttpContext.Request.Cookies.TryGetValue(RefreshTokenCookieName, out string? combinedToken))
			{
				return null;
			}
			var split = combinedToken.Split('\t');
			return (split[1], uint.Parse(split[0]));
		}

		public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

		[Authorize]
		[HttpGet("tokens")]
		public Ok<RefreshTokenResponse[]> GetRefreshTokens()
		{
			var tokens = _authenticator.GetRefreshTokens(HttpContext.GetCurrentUser()!);
			return TypedResults.Ok(tokens.Select(t => new RefreshTokenResponse(t.Id, t.ExpirationDate, t.CreationDate)).ToArray());
		}

		public record RefreshTokenResponse(uint Id, DateTimeOffset Expiration, DateTimeOffset Generated);

		[Authorize]
		[HttpDelete("tokens/{tokenId}")]
		public Results<Ok, NotFound> RevokeRefreshToken(uint tokenId)
		{
			if (!_authenticator.RevokeRefreshToken(HttpContext.GetCurrentUser()!, tokenId))
				return TypedResults.NotFound();

			return TypedResults.Ok();
		}

		[Authorize]
		[HttpDelete("tokens")]
		public Results<Ok, NotFound> RevokeAllRefreshTokens()
		{
			_authenticator.RevokeAllRefreshTokens(HttpContext.GetCurrentUser()!);
			return TypedResults.Ok();
		}

		[HttpPost("reset-password")]
		public Results<Ok, BadRequest, Ratelimited> ResetPassword([FromBody] ResetPasswordRequest request)
		{
			User? user;
			if (request is { Email: not null })
			{
				user = _users.GetUserByEmail(request.Email);
			} else
			{
				if (HttpContext.GetCurrentUser() is User loggedInUser)
				{
					user = loggedInUser;
				} else
				{
					return TypedResults.BadRequest();
				}
			}

			// if user is null here, we are not authenticated.
			// Return Ok because we don't want to expose if the user exists or not.
			if (user is null)
			{
				return TypedResults.Ok();
			}

			if (_authenticator.BeginPasswordReset(user) is TimeSpan cooldownLeft)
			{
				return ExtraTypedResults.Ratelimited(cooldownLeft);
			}

			return TypedResults.Ok();
		}

		public record ResetPasswordRequest(string Email);


		[HttpPost("change-password")]
		public Results<Ok, UnauthorizedHttpResult> ChangePassword(ChangePasswordRequest request)
		{
			var (email, token, newPassword, revokeAllTokens) = request;

			User? user = _users.GetUserByEmail(email);

			if (user is null && HttpContext.GetCurrentUser() is User loggedInUser)
			{
				user = loggedInUser;
			}

			if (user is null)
			{
				return TypedResults.Unauthorized();
			}

			if (_authenticator.CompletePasswordReset(user, token, newPassword))
			{
				if (revokeAllTokens)
				{
					_authenticator.RevokeAllRefreshTokens(user);
				}
				return TypedResults.Ok();
			} else
			{
				return TypedResults.Unauthorized();
			}
		}

		public record ChangePasswordRequest(string Email, string Token, string NewPassword, bool RevokeAllTokens);
	}
}
