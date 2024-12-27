using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Services.Caching;
using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Backend.Services.Implementation
{
	using static IGravatarRetriever;
	public class GravatarRetriever(IHttpClientFactory clientFactory, IMemoryCache cache, IOptionsMonitor<CacheConfiguration> cacheOptions)
		: CacheServiceOutput<RetrieveModel, byte[]>(cache), IGravatarRetriever
	{
		private readonly IHttpClientFactory _clientFactory = clientFactory;

		protected override TimeSpan EntryExpiration => TimeSpan.FromHours(cacheOptions.CurrentValue.GravatarCacheHours);

		protected override async Task<byte[]?> Fetch(ICacheEntry entry, RetrieveModel model)
		{
			var (email, size) = (model.Email, model.Size);

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

			var bytes = await response.Content.ReadAsByteArrayAsync();

			entry.SetSize(bytes.Length);

			return bytes;
		}

		override protected string GetCacheKey(RetrieveModel model) => $"{model.Email}@{model.Size}px";
	}
}
