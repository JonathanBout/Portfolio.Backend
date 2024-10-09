using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit.GraphQL;
using Octokit.GraphQL.Model;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Controllers.CustomTypedResults;
using IConnection = Octokit.GraphQL.IConnection;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/top-languages")]
	public class TopLanguagesController(IOptionsSnapshot<GitHubConfiguration> options) : ControllerBase
	{
		private readonly GitHubConfiguration _config = options.Value;

		const string TOP_LANGUAGES_CACHE_KEY = "top-languages";

		[HttpGet]
		public async Task<Results<Ok<Dictionary<string, LanguageResult>>, BadGateway>> GetTopLanguagesGraphQL([FromServices] IConnection gh, [FromServices] IMemoryCache cache, [FromQuery(Name = "exclude_langs")] string excludeLangs = "")
		{
			var result = await cache.GetOrCreateAsync(TOP_LANGUAGES_CACHE_KEY, async _ =>
			{
#pragma warning disable CS9236 // Compiling requires binding the lambda expression many times. Consider declaring the lambda expression with explicit parameter types, or if the containing method call is generic, consider using explicit type arguments.
				var query = new Query()
					.User(_config.Username)
					.Repositories(first: 100, isFork: false, affiliations: new Octokit.GraphQL.Core.Arg<IEnumerable<RepositoryAffiliation?>>([RepositoryAffiliation.Owner]))
					.Nodes
					.Select((Repository node) => new
					{
						node.Name,
						Languages = node.Languages(10, null, null, null, new LanguageOrder { Field = LanguageOrderField.Size, Direction = OrderDirection.Desc })
										.Select((LanguageConnection l) => l.Edges.Select((LanguageEdge e) => new
										{
											e.Size,
											Node = e.Node.Select((Language lang) => new
											{
												lang.Name,
												lang.Color
											}).Single()
										}).ToList()).Single()
					});
#pragma warning restore CS9236

				return (await gh.Run(query)).ToList();
			}, new MemoryCacheEntryOptions
			{
				AbsoluteExpiration = DateTimeOffset.Now.AddHours(6),
			});


			if (result is not { Count: > 0 })
			{
				return TypedResults.Ok(new Dictionary<string, LanguageResult>());
			}

			var langsToExclude = excludeLangs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			var langDict = new Dictionary<string, LanguageResult>();

			foreach (var repo in result)
			{
				foreach (var lang in repo.Languages)
				{
					var langName = lang.Node.Name;
					if (langsToExclude.Contains(langName, StringComparer.OrdinalIgnoreCase))
					{
						continue;
					}

					langDict.TryGetValue(langName, out var language);

					language.Name = langName;
					language.Color = lang.Node.Color;
					language.Size += lang.Size;

					langDict[langName] = language;
				}
			}

			var ordered = langDict.OrderByDescending((kv) => kv.Value.Size);

			return TypedResults.Ok(ordered.ToDictionary());
		}

		public record struct LanguageResult(string Name, string Color, long Size);
	}
}
