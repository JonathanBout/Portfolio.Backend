using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class CacheConfiguration
	{
		[Range(1, int.MaxValue)]
		public int GravatarCacheHours { get; set; } = 12;
		[Range(1, int.MaxValue)]
		public int TopLanguagesCacheHours { get; set; } = 8;
	}
}
