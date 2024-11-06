using Microsoft.Extensions.Options;
using Octokit.GraphQL;
using Portfolio.Backend.Configuration;

namespace Portfolio.Backend.Services.Implementation
{
	public class ConfigurationCredentialStore(IOptionsMonitor<GitHubConfiguration> options) : ICredentialStore
	{
		private GitHubConfiguration Config => options.CurrentValue;

		public Task<string> GetCredentials(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(Config.AccessToken);
		}
	}
}
