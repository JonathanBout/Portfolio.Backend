using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;

namespace Portfolio.Backend.Services.Caching
{
	public interface ICacheServiceOutput<TModel, TResult>
	{
		/// <summary>
		/// Get the result from the cache. If the result is not in the cache, it will be fetched and stored in the cache.
		/// </summary>
		/// <param name="model">The arguments to use when fetching</param>
		/// <param name="forceFetch">Wheter a fetch should be forced</param>
		Task<TResult?> Get(TModel model, bool forceFetch = false);
	}

	public abstract class CacheServiceOutput<TModel, TResult> : ICacheServiceOutput<TModel, TResult>
	{
		/// <summary>
		/// The prefix to use for the cache key. This is the name of the derived class.
		/// </summary>
		private readonly string CacheKeyPrefix;

		/// <summary>
		/// The cache to use for storing the data.
		/// </summary>
		protected IMemoryCache Cache { get; }

		/// <summary>
		/// The expiration time for a cache entry.
		/// </summary>
		abstract protected TimeSpan EntryExpiration { get; }


		protected CacheServiceOutput(IMemoryCache cache)
		{
			Cache = cache;
			CacheKeyPrefix = GetType().Name + "_";
		}

		public async Task<TResult?> Get(TModel model, bool forceFetch = false)
		{
			string cacheKey = CacheKeyPrefix + GetCacheKey(model);

			var entry = Cache.Get<TResult?>(cacheKey);

			if (forceFetch || entry is null)
			{
				var newEntry = Cache.CreateEntry(cacheKey);
				var newValue = await Fetch(newEntry, model);
				if (newValue is null)
				{
					newEntry.Dispose();
					return default;
				}

				newEntry.Value = newValue;
				newEntry.AbsoluteExpirationRelativeToNow = EntryExpiration;
				return newValue;
			}

			return entry;
		}

		protected abstract Task<TResult?> Fetch(ICacheEntry entry, TModel model);
		protected abstract string GetCacheKey(TModel model);
	}
}
