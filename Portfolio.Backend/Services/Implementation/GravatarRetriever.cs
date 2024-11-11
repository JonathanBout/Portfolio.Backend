using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Backend.Services.Implementation
{
	public class GravatarRetriever(IHttpClientFactory clientFactory, IMemoryCache cache, IOptionsMonitor<CacheConfiguration> cacheOptions) : IGravatarRetriever
	{
		private readonly IMemoryCache _cache = cache;
		private readonly IHttpClientFactory _clientFactory = clientFactory;
		private CacheConfiguration CacheOptions => cacheOptions.CurrentValue;

		const string CACHE_KEY_PREFIX = "user-gravatar-";

		public async Task<byte[]> Get(string email, uint size)
		{
			return await _cache.GetOrCreateAsync(GetCacheKey(email, size), async entry =>
			{
				var emailHash = SHA256.HashData(Encoding.UTF8.GetBytes(email));

				var hashString = Convert.ToHexString(emailHash).ToLower();

				// d=retro to get a retro default image
				var url = $"https://www.gravatar.com/avatar/{hashString}?d=retro&s={size}";
				var response = await _clientFactory.CreateClient("gravatar").GetAsync(url);

				if (!response.IsSuccessStatusCode)
				{
					entry.AbsoluteExpirationRelativeToNow = TimeSpan.Zero;
					return [];
				}

				return await response.Content.ReadAsByteArrayAsync();
			}, new()
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(CacheOptions.GravatarCacheHours),
			}) ?? []; // return empty array if cache is null
		}

		private static string GetCacheKey(string email, uint size) => $"{CACHE_KEY_PREFIX}{email}@{size}px";
	}
}
