using Portfolio.Backend.Services.Caching;

namespace Portfolio.Backend.Services
{
	public interface IGravatarRetriever : ICacheServiceOutput<IGravatarRetriever.RetrieveModel, byte[]>
	{
		public record RetrieveModel(string Email, uint Size);
	}
}
