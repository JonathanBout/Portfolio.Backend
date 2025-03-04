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

			options.Events.OnTokenValidated += ctx =>
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

				var token = user.RefreshTokens.FirstOrDefault(t => t.Id == tokenId);

				if (token?.IsExpired() is not false)
				{
					ctx.Fail(FailureMessage);
					return Task.CompletedTask;
				}

				return Task.CompletedTask;
			};
		}
	}
}
