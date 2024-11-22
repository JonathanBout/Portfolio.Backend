using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class GitHubConfiguration
	{
		[Required]
		public required string Username { get; set; }
		public string? AccessToken { get; set; }

		public string? AccessTokenFilePath { get; set; }

		public string GetAccessToken()
		{
			if (AccessToken is not null) return AccessToken;
			if (AccessTokenFilePath is not null) return File.ReadAllText(AccessTokenFilePath);

			throw new InvalidOperationException("Either AccessToken or AccessTokenFilePath should be configured.");
		}
	}
}
