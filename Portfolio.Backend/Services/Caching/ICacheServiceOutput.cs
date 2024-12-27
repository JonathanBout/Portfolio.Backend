using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;

namespace Portfolio.Backend.Services.Caching
{
	public interface ICacheServiceOutput<TModel, TResult>
	{
		Task<TResult?> Get(TModel model, bool forceFetch = false);
	}

	public abstract class CacheServiceOutput<TModel, TResult>(IMemoryCache cache) : ICacheServiceOutput<TModel, TResult>
	{
		protected IMemoryCache Cache => cache;

		abstract protected TimeSpan EntryExpiration { get; }

		public Task<TResult?> Get(TModel model, bool forceFetch = false)
		{
			string cacheKey = GetCacheKey(model);
			if (forceFetch)
			{
				Cache.Remove(cacheKey);
			}

			return Cache.GetOrCreateAsync(cacheKey, e => Fetch(e, model), new()
			{
				AbsoluteExpirationRelativeToNow = EntryExpiration
			});
		}

		protected abstract Task<TResult?> Fetch(ICacheEntry entry, TModel model);
		protected abstract string GetCacheKey(TModel model);
	}
}
