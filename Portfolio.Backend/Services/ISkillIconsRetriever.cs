using Portfolio.Backend.Services.Caching;

namespace Portfolio.Backend.Services
{
	public interface ISkillIconsRetriever : ICacheServiceOutput<ISkillIconsRetriever.RetrieveModel, byte[]>
	{
		public record RetrieveModel(string Icon, string Theme);
	}
}
