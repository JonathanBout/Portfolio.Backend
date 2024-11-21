using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;
using Portfolio.Backend.Services;
using System.Diagnostics;

namespace Portfolio.Backend.Health
{
	public class SkillIconsHealth(ISkillIconsRetriever retriever, IOptionsSnapshot<HealthConfiguration> options) : IHealthCheck
	{
		private readonly ISkillIconsRetriever _retriever = retriever;
		private readonly HealthConfiguration _config = options.Value;
		public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
		{
			var start = Stopwatch.GetTimestamp();
			try
			{
				var health = await _retriever.Get("cs", "light", true);
			} catch (Exception e)
			{
				return HealthCheckResult.Unhealthy("Failed to connect to SkillIcons", e);
			}


			var elapsed = Stopwatch.GetElapsedTime(start);

			var diff = _config.MaxHealthyDurationMilliseconds - elapsed.TotalMilliseconds;

			if (diff < 0)
			{
				return HealthCheckResult.Degraded($"SkillIcons request took {elapsed.TotalMilliseconds}ms, {-diff}ms more than acceptable.");
			}

			return HealthCheckResult.Healthy();
		}
	}
}
