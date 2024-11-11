using System.Diagnostics;

namespace Portfolio.Backend.Health
{
	public struct Timing
	{
		private long _startTime;
		private long _endTime;

		public readonly TimeSpan Duration => Stopwatch.GetElapsedTime(_startTime, _endTime);

		public IDisposable Time()
		{
			_startTime = Stopwatch.GetTimestamp();
			return new TimingScope(this);
		}

		private void EndTime()
		{
			_endTime = Stopwatch.GetTimestamp();
		}


		struct TimingScope(Timing t) : IDisposable
		{
			public readonly void Dispose() => t.EndTime();
		}
	}

}
