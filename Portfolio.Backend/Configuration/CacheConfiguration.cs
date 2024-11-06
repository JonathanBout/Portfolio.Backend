namespace Portfolio.Backend.Configuration
{
	public class CacheConfiguration
	{
		const int MINUTE = 60;
		const int HOUR = 60 * MINUTE;

		public int LongCacheDuration { get; set; } = 25 * HOUR;
		public int MediumCacheDuration { get; set; } = 5 * HOUR;
		public int ShortCacheDuration { get; set; } = 1 * HOUR;
	}
}
