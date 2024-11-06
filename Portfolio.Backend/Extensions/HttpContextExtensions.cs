using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Services.Implementation;
using System.Security.Claims;

namespace Portfolio.Backend.Extensions
{
	public static class HttpContextExtensions
	{
		public static User? GetCurrentUser(this HttpContext context)
		{
			if (context.User.Identity?.IsAuthenticated ?? false)
			{
				var id = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (id != null && uint.TryParse(id, out var userId))
				{
					var dbContext = context.RequestServices.GetRequiredService<DatabaseContext>();

					return dbContext.Users.FirstOrDefault(u => u.Id == userId);
				}
			}
			return null;
		}
	}
}
