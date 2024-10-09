namespace Portfolio.Backend.Controllers.CustomTypedResults
{
	public class BadGateway : IResult
	{
		public Task ExecuteAsync(HttpContext httpContext)
		{
			httpContext.Response.StatusCode = StatusCodes.Status502BadGateway;
			return Task.CompletedTask;
		}
	}
}
