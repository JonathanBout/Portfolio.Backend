
namespace Portfolio.Backend.Controllers.CustomTypedResults
{
	public class Ratelimited(TimeSpan retryAfter = default) : IResult
	{
		private readonly TimeSpan _retryAfter = retryAfter;

		public Task ExecuteAsync(HttpContext httpContext)
		{
			httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

			if (_retryAfter != default)
			{
				httpContext.Response.Headers.Append("Retry-After", _retryAfter.TotalSeconds.ToString("F0"));
			}

			return Task.CompletedTask;
		}
	}
}
