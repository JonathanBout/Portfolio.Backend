using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Backend.Data.Users;
using Portfolio.Backend.Extensions;
using Portfolio.Backend.Services;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/users")]
	[Authorize]
	public class UserController(IUserService userService) : ControllerBase
	{
		private readonly IUserService _userService = userService;

		const int HOUR = 60 * 60;

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

		[HttpGet("me/image")]
		public RedirectToActionResult GetMeImage([Range(1, 2048)] ushort size = 256)
		{
			var currentUser = HttpContext.GetCurrentUser()!;

			return RedirectToAction(nameof(GetUserImage), new { slug = currentUser.NameSlug, size });
		}

		[HttpPut("me/image")]
		public Results<Ok, BadRequest> UpdateMeImage([FromForm] IFormFile image)
		{
			var user = HttpContext.GetCurrentUser()!;

			var parts = image.ContentType.Split("/", 2);

			if (parts.Length < 2 || parts[0] != "image")
			{
				return TypedResults.BadRequest();
			}

			string newMediaType;

			switch (parts[1].ToLower())
			{
				case "png":
					newMediaType = MediaTypeNames.Image.Png;
					break;
				case "jpg" or "jpeg":
					newMediaType = MediaTypeNames.Image.Jpeg;
					break;
				case "webp":
					newMediaType = MediaTypeNames.Image.Webp;
					break;
				default:
					return TypedResults.BadRequest();
			}

			user.ProfileImageFormat = newMediaType;

			using var stream = image.OpenReadStream();

			var imageBytes = new byte[stream.Length];
			var imageSpan = imageBytes.AsSpan();

			stream.ReadExactly(imageSpan);

			user.ProfileImage = imageBytes;

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
		[ResponseCache(Duration = 3 * HOUR)]
		public async Task<Results<FileContentHttpResult, FileStreamHttpResult, NotFound>> GetUserImage(IGravatarRetriever gravatar, string slug, [Range(1, 2048)] ushort size = 256)
		{
			var user = _userService.GetUserBySlug(slug);

			if (user is null)
				return TypedResults.NotFound();

			if (user.ProfileImage is null)
			{
				return TypedResults.File(await gravatar.Get(new(user.Email, size)) ?? [], MediaTypeNames.Image.Jpeg);
			}

			return TypedResults.File(user.ProfileImage, user.ProfileImageFormat);
		}

		public record UserResponse(string Email, string FullName, string Description)
		{
			public static UserResponse FromUser(User user) => new(user.Email, user.FullName, user.Description);
		}
	}
}
