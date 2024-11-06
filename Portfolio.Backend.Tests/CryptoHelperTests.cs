using Portfolio.Backend.Configuration;
using Portfolio.Backend.Services;
using Portfolio.Backend.Services.Implementation;
using System.Text;

namespace Portfolio.Backend.Tests
{
	public class CryptoHelperTests
	{
		private static CryptoHelper Create(int stress)
		{
			return new CryptoHelper(new OptionsMock<CryptoConfiguration>(new()
			{
				Iterations = stress,
				MemorySize = (4 * stress) + stress,
				KeySize = 32 + stress,
				SaltSize = 16 + stress,
				Parallelism = stress,
				PasswordCharacters = ICryptoHelper.LowerAlphaCharacters + ICryptoHelper.UpperAlphaCharacters + ICryptoHelper.NumericCharacters + ICryptoHelper.SpecialCharacters,
				StrongPasswordLength = 8
			}));
		}

		[Test]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void Test_Hash_v1_Compare(int stress)
		{
			var helper = Create(stress);

			var input = "Hello, World!";

			var bytes = Encoding.UTF8.GetBytes(input);

			var hash = helper.Hash_v1(bytes);

			var result = helper.Verify_v1(bytes, hash);

			Assert.That(result, Is.EqualTo(VerificationResult.Success));
		}

		[Test]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void Test_Hash_Compare_v1_Fail(int stress)
		{
			var helper = Create(stress);
			var input = "Hello, World!";
			var bytes = Encoding.UTF8.GetBytes(input);
			var hash = helper.Hash_v1(bytes);
			bytes[^1]++;
			var result = helper.Verify_v1(bytes, hash);
			Assert.That(result, Is.EqualTo(VerificationResult.Failed));
		}

		[Test]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void Test_Hash_Compare_v1_Rehash(int stress)
		{
			var helper = Create(stress);
			var input = "Hello, World!";
			var bytes = Encoding.UTF8.GetBytes(input);
			var hash = helper.Hash_v1(bytes);
			helper = Create(stress + 1);
			var result = helper.Verify_v1(bytes, hash);
			Assert.That(result, Is.EqualTo(VerificationResult.SuccessRehashNeeded));
		}

		[Test]
		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		public void Test_Hash_Compare_v1_Rehash_Fail(int stress)
		{
			var helper = Create(stress);
			var input = "Hello, World!";
			var bytes = Encoding.UTF8.GetBytes(input);
			var hash = helper.Hash_v1(bytes);
			helper = Create(stress + 1);
			bytes[^1]++;
			var result = helper.Verify_v1(bytes, hash);
			Assert.That(result, Is.EqualTo(VerificationResult.Failed));
		}

		[Test]
		public void Test_Hash_Empty_Input()
		{
			var helper = Create(1);
			var bytes = Array.Empty<byte>();

			var hash = helper.Hash(bytes);

			Assert.That(hash, Has.Length.EqualTo(0));
		}

		[Test]
		public void Test_Verify_Empty_Input()
		{
			var helper = Create(1);
			var bytes = Array.Empty<byte>();
			var hash = helper.Hash(bytes);
			var result = helper.Verify(bytes, hash);
			Assert.That(result, Is.EqualTo(VerificationResult.Failed));
		}
	}
}