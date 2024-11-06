
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Backend.Controllers.CustomTypedResults;
using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Extensions;
using Portfolio.Backend.Services;
using Portfolio.Backend.Services.Implementation;
using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/auth")]
	public class AuthenticationController(IAuthenticator authenticator, IUserService users) : ControllerBase
	{
		private readonly IAuthenticator _authenticator = authenticator;
		private readonly IUserService _users = users;

		[HttpGet("refresh")]
		public Results<Ok<CreatedAccessTokenResponse>, UnauthorizedHttpResult> Refresh([FromQuery] string email, [FromQuery] uint refreshTokenId, [FromQuery] string refreshToken)
		{
			var token = _authenticator.GetAccessToken(email, refreshTokenId, refreshToken);
			return token is null
				? TypedResults.Unauthorized()
				: TypedResults.Ok(new CreatedAccessTokenResponse(token));
		}
		public record CreatedAccessTokenResponse(string AccessToken);


		[HttpPost("login")]
		public Results<Ok<LoginResponse>, UnauthorizedHttpResult> Login([FromBody] LoginRequest request)
		{
			var generatedToken = _authenticator.GetRefreshToken(request.Email, request.Password);

			return generatedToken is not (string token, uint id)
				? TypedResults.Unauthorized()
				: TypedResults.Ok(new LoginResponse(token, id));
		}

		public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);
		public record LoginResponse(string RefreshToken, uint TokenId);


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
