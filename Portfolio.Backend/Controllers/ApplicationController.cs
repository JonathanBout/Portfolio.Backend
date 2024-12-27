using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Portfolio.Backend.Controllers
{
	[ApiController]
	[Route("api/application")]
	//[Authorize]
	public class ApplicationController : ControllerBase
	{
		[HttpGet("cache")]
		public Results<Ok<CacheStatistics>, UnauthorizedHttpResult> Get(IMemoryCache cache)
		{
			return TypedResults.Ok(CacheStatistics.FromStatistics(cache.GetCurrentStatistics()));
		}

		[HttpDelete("cache")]
		[HttpGet("cache/del")]
		public Results<Ok, StatusCodeHttpResult, UnauthorizedHttpResult> Clear(IMemoryCache cache, ILogger<ApplicationController> logger)
		{
			if (cache is MemoryCache mc)
			{
				mc.Clear();
				return TypedResults.Ok();
			}

			logger.LogWarning("Cannot clear cache, as it is not a MemoryCache instance but '{actualType}' instead", cache.GetType().Name);

			return TypedResults.StatusCode(StatusCodes.Status501NotImplemented);
		}

		public record CacheStatistics(long Entries, long Size, long Hits, long Misses)
		{
			public static CacheStatistics FromStatistics(MemoryCacheStatistics? stats)
			{
				return new CacheStatistics(stats?.CurrentEntryCount ?? 0, stats?.CurrentEstimatedSize ?? -1, stats?.TotalHits ?? 0, stats?.TotalMisses ?? 0);
			}
		}
	}
}
