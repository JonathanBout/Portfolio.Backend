namespace Portfolio.Backend.Services
{
	public interface IIntruderDetector
	{
		public void EnqueueInvalidAccessTokenUsage(uint tokenId, string usedToken);
	}
}
