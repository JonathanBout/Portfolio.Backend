
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Services.Caching;
using System.Web;

namespace Portfolio.Backend.Services.Implementation
{
	using static ISkillIconsRetriever;
	public class SkillIconsRetriever(IMemoryCache cache, IHttpClientFactory clientFactory, IOptionsMonitor<CacheConfiguration> cacheOptions)
		: CacheServiceOutput<RetrieveModel, byte[]>(cache), ISkillIconsRetriever
	{
		private readonly IHttpClientFactory _clientFactory = clientFactory;
		protected override TimeSpan EntryExpiration => TimeSpan.FromHours(cacheOptions.CurrentValue.SkillIconsCacheHours);

		protected override async Task<byte[]?> Fetch(ICacheEntry entry, RetrieveModel model)
		{
			var (icon, theme) = (model.Icon, model.Theme);

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

		protected override string GetCacheKey(RetrieveModel model) => $"{model.Icon}-{model.Theme}";
	}
}
