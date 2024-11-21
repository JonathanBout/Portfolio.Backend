using Castle.Components.DictionaryAdapter.Xml;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit.GraphQL.Model;

namespace Portfolio.Backend.Services.Caching
{
	public sealed class MemoryCacheWrapper(IOptions<MemoryCacheOptions> options, ILoggerFactory logger) : IMemoryCache, IOutputCacheStore
	{
		private readonly MemoryCache _innerCache = new(options, logger);
		private readonly ILogger<MemoryCacheWrapper> _logger = logger.CreateLogger<MemoryCacheWrapper>();
		private readonly IOptions<MemoryCacheOptions> _options = options;

		private readonly Dictionary<string, ICacheEntry> _entriesByTag = [];

		private readonly Dictionary<object, (int hits, int misses)> _statsByKey = [];

		public ICacheEntry CreateEntry(object key)
		{
			var entry = _innerCache.CreateEntry(key);

			entry.RegisterPostEvictionCallback(Evict);

			return entry;
		}

		public void Dispose() => _innerCache.Dispose();
		public void Remove(object key) => _innerCache.Remove(key);
		public bool TryGetValue(object key, out object? value)
		{
			if (!_statsByKey.TryGetValue(key, out var stats))
			{
				stats = (0, 0);
			}

			bool exists = _innerCache.TryGetValue(key, out value);

			if (exists)
				stats.hits++;
			else
				stats.misses++;

			_statsByKey[key] = stats;

			return exists;
		}

		public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken)
		{
			var entry = CreateEntry(key);

			entry.Value = value;

			entry.AbsoluteExpirationRelativeToNow = validFor;

			foreach (var tag in tags ?? [])
			{
				_entriesByTag[tag] = entry;
			}

			entry.SetSize(value.LongLength);

			return ValueTask.CompletedTask;
		}

		public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
		{
			var entry = _entriesByTag[tag];

			entry.AbsoluteExpiration = DateTimeOffset.Now;
			return ValueTask.CompletedTask;
		}

		public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken)
		{
			if (this.TryGetValue<byte[]>(key, out var value))
				return ValueTask.FromResult(value);

			return ValueTask.FromResult<byte[]?>(null);
		}

		private void Evict(object key, object? value, EvictionReason reason, object? state)
		{
			// remove the tagged entries from the dictionary
			var byTag = _entriesByTag.Where(x => x.Value.Key == key);

			foreach (var (k, _) in byTag)
			{
				_entriesByTag.Remove(k);
			}
		}

		public MemoryCacheStatistics? GetCurrentStatistics()
		{
			if (!_options.Value.TrackStatistics) return null;

			var stats = _innerCache.GetCurrentStatistics();

			if (stats is null) return null;

			var (hits, misses) = _statsByKey.Select(p => p.Value).Aggregate((hits: 0L, misses: 0L), (acc, cur) => (acc.hits + cur.hits, acc.misses + cur.misses));


			var ourStats = new MemoryCacheStatistics
			{
				CurrentEntryCount = _statsByKey.Count,
				TotalHits = hits,
				TotalMisses = misses,
				CurrentEstimatedSize = stats.CurrentEstimatedSize
			};

			_logger.LogInformation("Inner cache logging stats: ({innerCount}, {innerHits}, {innerMisses}, {innerSize})\nOur logging stats: ({count}, {hits}, {misses}, {size})",
				stats.CurrentEntryCount,
				stats.TotalHits,
				stats.TotalMisses,
				stats.CurrentEstimatedSize,
				ourStats.CurrentEntryCount,
				ourStats.TotalHits,
				ourStats.TotalMisses,
				ourStats.CurrentEstimatedSize);

			return ourStats;
		}
	}
}
