namespace Portfolio.Backend.Controllers.CustomTypedResults
{
	public static class ExtraTypedResults
	{
		public static BadGateway BadGateway() => new();
		public static Ratelimited Ratelimited() => new();
		public static Ratelimited Ratelimited(TimeSpan timeLeft) => new(timeLeft);
	}
}
