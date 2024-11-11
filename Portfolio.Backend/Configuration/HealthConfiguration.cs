using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class HealthConfiguration
	{
		[Range(1, int.MaxValue)]
		public int MaxHealthyDurationMilliseconds { get; set; } = 500;
	}
}
