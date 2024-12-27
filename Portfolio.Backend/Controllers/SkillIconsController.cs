using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Backend.Services;
using System.Net.Mime;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/skill-icons")]
	public class SkillIconsController(ISkillIconsRetriever retriever) : ControllerBase
	{
		private readonly ISkillIconsRetriever _retriever = retriever;

		const int HOUR = 60 * 60;
		const int DAY = 24 * HOUR;

		[HttpGet("{icon}")]
		[ResponseCache(Duration = 3 * DAY)]
		public async Task<Results<NotFound, FileContentHttpResult>> GetIcon(string icon, string theme = "")
		{
			icon = icon switch
			{
				"csharp" => "c#",
				string v => v
			};

			var bytes = await _retriever.Get(new(icon, theme));

			if (bytes is { Length: > 0 })
			{
				return TypedResults.File(bytes, MediaTypeNames.Image.Svg);
			} else
			{
				return TypedResults.NotFound();
			}
		}
	}
}
