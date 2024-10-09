using Microsoft.Extensions.Options;
using Octokit.GraphQL;
using Portfolio.Backend.Configuration;

namespace Portfolio.Backend.Services
{
	public class ConfigurationCredentialStore(IOptionsSnapshot<GitHubConfiguration> options) : ICredentialStore
	{
		private readonly GitHubConfiguration _config = options.Value;

		public Task<string> GetCredentials(CancellationToken cancellationToken = default)
		{
			return Task.FromResult(_config.AccessToken);
		}
	}
}
