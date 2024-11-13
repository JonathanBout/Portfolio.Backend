using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class CacheConfiguration
	{
		[Range(1, int.MaxValue)]
		public int GravatarCacheHours { get; set; } = 12;
		[Range(1, int.MaxValue)]
		public int TopLanguagesCacheHours { get; set; } = 8;
		[Range(1, int.MaxValue)]
		public int SkillIconsCacheHours { get; set; } = 48;

		[Range(1, int.MaxValue)]
		public int HealthCheckCacheSeconds { get; set; } = 90;
	}
}
