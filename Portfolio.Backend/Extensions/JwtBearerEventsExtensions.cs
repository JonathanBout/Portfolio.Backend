using Microsoft.AspNetCore.Authentication.JwtBearer;
using Portfolio.Backend.Services.Implementation;
using System.Diagnostics;
using System.Security.Claims;

namespace Portfolio.Backend.Extensions
{
	public static class JwtBearerEventsExtensions
	{
		/// <summary>
		/// Validates the refresh token and user id from the token.
		/// </summary>
		public static void AddRefreshTokenValidator(this JwtBearerOptions options)
		{
			const string FailureMessage = "invalid refresh token or user id";

			options.Events ??= new JwtBearerEvents();

			options.Events.OnTokenValidated += (TokenValidatedContext ctx) =>
			{
				if (ctx.Principal is null)
					return Task.CompletedTask;

				var claimedRefreshTokenId = ctx.Principal.FindFirst(CustomClaimTypes.RefreshTokenId)?.Value;
				var claimedUserId = ctx.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				var database = ctx.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();

				if (!uint.TryParse(claimedRefreshTokenId, out uint tokenId) || !uint.TryParse(claimedUserId, out uint userId))
				{
					ctx.Fail(FailureMessage);
					return Task.CompletedTask;
				}

				var user = database.Users.Find(userId);

				if (user is null)
				{
					ctx.Fail(FailureMessage);
					return Task.CompletedTask;
				}

				if (!user.RefreshTokens.Any(t => t.Id == tokenId))
				{
					ctx.Fail(FailureMessage);
					return Task.CompletedTask;
				}

				// TODO: Check if the refresh token was already used.
				// If that's the case, the token should be invalidated as this is probably a replay attack.
				// Before that, we need to keep all previous refresh tokens in the database.

				return Task.CompletedTask;
			};
		}
	}
}
