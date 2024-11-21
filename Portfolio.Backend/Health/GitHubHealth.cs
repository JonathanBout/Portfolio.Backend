using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Octokit.GraphQL;
using Portfolio.Backend.Configuration;
using System.Diagnostics;

namespace Portfolio.Backend.Health
{
	public class GitHubHealth(IConnection gitHub, IOptionsSnapshot<HealthConfiguration> options) : IHealthCheck
	{
		private readonly IConnection _gitHub = gitHub;
		private readonly HealthConfiguration _config = options.Value;

		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			var start = Stopwatch.GetTimestamp();

			try
			{
				var health = await _gitHub.Run(new Query().User("JonathanBout").Status.Select(s => new { s.Message }), cancellationToken: cancellationToken);

			} catch (Exception e)
			{
				return HealthCheckResult.Unhealthy("Failed to connect to GitHub", e);
			}


			var elapsed = Stopwatch.GetElapsedTime(start);

			var diff = _config.MaxHealthyDurationMilliseconds - elapsed.TotalMilliseconds;

			if (diff < 0)
			{
				return HealthCheckResult.Degraded($"GitHub request took {elapsed.TotalMilliseconds}ms, {-diff}ms more than acceptable.");
			}

			return HealthCheckResult.Healthy();
		}
	}
}
