using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Portfolio.Backend.Health;
using System.Net.Mime;
using System.Text.Json;

namespace Portfolio.Backend.Extensions
{
	public static class WebApplicationExtensions
	{
		public static WebApplication AddHealthChecks(this WebApplication app)
		{
			var healthGroup = app.MapGroup("/api/_health");
			// Health check for all services.
			healthGroup.MapHealthChecks("", new HealthCheckOptions
			{
				AllowCachingResponses = true,
				ResultStatusCodes =
				{
					[HealthStatus.Healthy] = StatusCodes.Status200OK,
					[HealthStatus.Degraded] = StatusCodes.Status200OK,
					[HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
				},
				ResponseWriter = WriteResponse
			});

			// Health check for services we manage ourselves.
			healthGroup.MapHealthChecks("first-party", new HealthCheckOptions
			{
				AllowCachingResponses = true,
				Predicate = (check) => !check.Tags.Contains(HealthCheckerTags.ThirdParty),
				ResultStatusCodes =
				{
					[HealthStatus.Healthy] = StatusCodes.Status200OK,
					[HealthStatus.Degraded] = StatusCodes.Status200OK,
					[HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
				},
				ResponseWriter = WriteResponse
			});

			healthGroup.CacheOutput("health-checks");

			return app;
		}

		private static Task WriteResponse(HttpContext context, HealthReport report)
		{
			if (context.Request.Query.TryGetValue("simple", out var value) && value.Contains("true", StringComparer.OrdinalIgnoreCase))
			{
				context.Response.ContentType = MediaTypeNames.Text.Plain;
				return context.Response.WriteAsync(report.Status.ToString().ToLower());
			}

			context.Response.ContentType = MediaTypeNames.Application.Json;

			var json = JsonSerializer.Serialize(new
			{
				Status = report.Status.ToString(),
				TotalDuration = report.TotalDuration.TotalMilliseconds,
				Entries = report.Entries.Select(e => new
				{
					e.Key,
					Value = e.Value.Status.ToString(),
					Elapsed = e.Value.Duration.TotalMilliseconds,
					e.Value.Data,
					e.Value.Tags
				})
			});

			return context.Response.WriteAsync(json);
		}
	}
}
