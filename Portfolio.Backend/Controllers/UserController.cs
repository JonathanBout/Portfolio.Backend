using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Extensions;
using Portfolio.Backend.Services;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/users")]
	[Authorize]
	public class UserController(IUserService userService) : ControllerBase
	{
		private readonly IUserService _userService = userService;

		[HttpGet("me")]
		public Ok<UserResponse> GetMe()
		{
			var user = HttpContext.GetCurrentUser()!;
			return TypedResults.Ok(UserResponse.FromUser(user));
		}


		[HttpPatch("me")]
		public Results<Ok<UserResponse>, BadRequest> UpdateMe([FromBody] UpdateUserRequest request)
		{
			var user = HttpContext.GetCurrentUser()!;
			if (string.IsNullOrWhiteSpace(request.FullName))
				return TypedResults.BadRequest();

			if (!string.IsNullOrWhiteSpace(request.Description))
				user.Description = request.Description;

			if (!string.IsNullOrWhiteSpace(request.FullName))
				user.FullName = request.FullName;

			_userService.UpdateUser(user);

			return TypedResults.Ok(UserResponse.FromUser(user));
		}

		[HttpPut("me/image")]
		public Results<Ok, BadRequest> UpdateMeImage([FromBody] byte[] image)
		{
			var user = HttpContext.GetCurrentUser()!;
			user.ProfileImage = image;
			_userService.UpdateUser(user);
			return TypedResults.Ok();
		}

		[HttpDelete("me/image")]
		public Results<NoContent, BadRequest> DeleteMeImage()
		{
			var user = HttpContext.GetCurrentUser()!;
			user.ProfileImage = null;
			_userService.UpdateUser(user);
			return TypedResults.NoContent();
		}

		public record UpdateUserRequest(string? FullName = null, string? Description = null);

		[HttpGet("{slug}")]
		[AllowAnonymous]
		public Results<Ok<UserResponse>, NotFound> GetUser(string slug)
		{
			var user = _userService.GetUserBySlug(slug);
			return user is null
				? TypedResults.NotFound()
				: TypedResults.Ok(UserResponse.FromUser(user));
		}

		[HttpGet("{slug}/image")]
		[AllowAnonymous]
		public Results<FileContentHttpResult, NotFound, RedirectHttpResult> GetUserImage(string slug, [Range(1, 2048)] ushort size = 256)
		{
			var user = _userService.GetUserBySlug(slug);

			if (user is null)
				return TypedResults.NotFound();

			if (user.ProfileImage is null)
			{
				// redirect to Gravatar
				var emailHash = SHA256.HashData(Encoding.UTF8.GetBytes(user.Email));

				var hashString = Convert.ToHexString(emailHash).ToLower();

				// d=retro to get a retro default image
				return TypedResults.Redirect($"https://www.gravatar.com/avatar/{hashString}?d=retro&s={size}");
			}

			return TypedResults.File(user.ProfileImage);
		}

		public record UserResponse(string Email, string FullName, string Description)
		{
			public static UserResponse FromUser(User user) => new(user.Email, user.FullName, user.Description);
		}
	}
}
