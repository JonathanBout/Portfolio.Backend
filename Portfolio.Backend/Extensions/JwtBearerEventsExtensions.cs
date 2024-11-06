using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Services.Implementation;
using System.Diagnostics;
using System.Security.Claims;

namespace Portfolio.Backend.Extensions
{
	public static class JwtBearerEventsExtensions
	{
		public static void AddRefreshTokenValidator(this JwtBearerOptions options)
		{
			const string FailureMessage = "invalid refresh token or user id";

			options.Events ??= new JwtBearerEvents();

			options.Events.OnTokenValidated += (TokenValidatedContext ctx) =>
			{
				Debug.WriteLine("ddd");

				if (ctx.Principal is null)
					return Task.CompletedTask;

				var refreshTokenId = ctx.Principal.FindFirst(CustomClaimTypes.RefreshTokenId)?.Value;
				var userId = ctx.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

				var database = ctx.HttpContext.RequestServices.GetRequiredService<DatabaseContext>();

				if (!uint.TryParse(refreshTokenId, out uint rtId) || !uint.TryParse(userId, out uint uId))
				{
					ctx.Fail(FailureMessage);
					return Task.CompletedTask;
				}

				var user = database.Users.Find(uId);

				if (user is null || !user.RefreshTokens.Any(t => t.Id == rtId))
				{
					ctx.Fail(FailureMessage);
					return Task.CompletedTask;
				}

				return Task.CompletedTask;
			};
		}
	}
}
