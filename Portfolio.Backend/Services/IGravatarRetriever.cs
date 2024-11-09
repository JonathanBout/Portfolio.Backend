namespace Portfolio.Backend.Services
{
	public interface IGravatarRetriever
	{
		public Task<byte[]> Get(string email, uint size);
	}
}
