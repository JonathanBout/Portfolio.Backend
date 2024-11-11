namespace Portfolio.Backend.Services
{
	public interface ISkillIconsRetriever
	{
		Task<byte[]> Get(string icon, string theme);
	}
}
