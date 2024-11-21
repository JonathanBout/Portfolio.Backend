
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit.GraphQL.Model;
using Portfolio.Backend.Configuration;
using System.Web;

namespace Portfolio.Backend.Services.Implementation
{
	public class SkillIconsRetriever(IMemoryCache cache, IHttpClientFactory clientFactory, IOptionsMonitor<CacheConfiguration> cacheOptions) : ISkillIconsRetriever
	{
		private readonly IMemoryCache _cache = cache;
		private readonly IHttpClientFactory _clientFactory = clientFactory;
		private CacheConfiguration CacheOptions => cacheOptions.CurrentValue;

		const string CACHE_KEY_PREFIX = "skill-icon-";

		public async Task<byte[]> Get(string icon, string theme, bool forceFetch = false)
		{
			if (forceFetch)
			{
				_cache.Remove(GetCacheKey(icon, theme));
			}

			return await _cache.GetOrCreateAsync(GetCacheKey(icon, theme), entry => Fetch(entry, icon, theme), new()
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheOptions.SkillIconsCacheHours)
			}) ?? [];
		}

		private async Task<byte[]> Fetch(ICacheEntry entry, string icon, string theme)
		{
			icon = HttpUtility.UrlEncode(icon);
			theme = HttpUtility.UrlEncode(theme);

			var url = $"https://skillicons.dev/icons?i={icon}";

			if (!string.IsNullOrWhiteSpace(theme))
				url += $"&t={theme}";

			var client = _clientFactory.CreateClient("skill-icons");

			var response = await client.GetAsync(url);

			if (!response.IsSuccessStatusCode)
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.Zero;

				entry.SetSize(0);

				return [];
			}

			var bytes = await response.Content.ReadAsByteArrayAsync();
			entry.SetSize(bytes.LongLength);
			return bytes;
		}

		private static string GetCacheKey(string icon, string theme) => $"{CACHE_KEY_PREFIX}{icon}-{theme}";
	}
}
