using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portfolio.Backend.Tests
{
	internal class OptionsMock<T>(T value) : IOptionsSnapshot<T>, IOptionsMonitor<T>, IOptions<T> where T : class
	{
		public T Value => value;
		public T CurrentValue => Value;
		public T Get(string? name) => value;
		public IDisposable? OnChange(Action<T, string?> listener) => new DummyDisposable();
	}

	internal class DummyDisposable : IDisposable
	{
		public void Dispose() { }
	}
}
