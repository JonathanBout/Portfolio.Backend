using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class GitHubConfiguration
	{
		[Required]
		public required string Username { get; set; }
		[Required]
		public required string AccessToken { get; set; }
	}
}
