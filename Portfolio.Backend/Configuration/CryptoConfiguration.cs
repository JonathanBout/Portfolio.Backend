using Portfolio.Backend.Services;
using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class CryptoConfiguration
	{
		[Range(16, int.MaxValue)]
		public int SaltSize { get; set; } = 16;

		[Range(32, int.MaxValue)]
		public int KeySize { get; set; } = 32;

		[Range(4, int.MaxValue)]
		public int MemorySize { get; set; } = 8;

		[Range(1, int.MaxValue)]
		public int Iterations { get; set; } = 1;

		[Range(1, int.MaxValue)]
		public int Parallelism { get; set; } = 1;

		[Range(4, int.MaxValue)]
		public int StrongPasswordLength { get; set; } = 8;

		public string PasswordCharacters { get; set; } = ICryptoHelper.RandomStringCharacters;
	}
}
