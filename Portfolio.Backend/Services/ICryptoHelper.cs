using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Backend.Services
{
	/// <summary>
	/// Provides cryptographically secure hashing and comparison methods.
	/// </summary>
	public interface ICryptoHelper
	{
		public const string RandomStringCharacters = AlphaNumericCharacters + SpecialCharacters;
		public const string AlphaNumericCharacters = NumericCharacters + AlphaCharacters;
		public const string NumericCharacters = "1234567890";
		public const string LowerAlphaCharacters = "abcdefghijklmnopqrstuvwxyz";
		public const string UpperAlphaCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const string AlphaCharacters = LowerAlphaCharacters + UpperAlphaCharacters;
		public const string SpecialCharacters = "!@#$&";

		/// <summary>
		/// Hashes the input data.
		/// </summary>
		/// <param name="input">The value to hash</param>
		/// <returns>A hashed representation of <paramref name="input"/>.</returns>
		byte[] Hash(ReadOnlySpan<byte> input);

		/// <summary>
		/// Hashes the input data and compares it to the provided hash in constant time.
		/// </summary>
		/// <param name="input">The not hashed value to compare</param>
		/// <param name="hash">The hashed value</param>
		/// <returns>
		/// - <see cref="VerificationResult.Failed"/> if the values are not equal.
		/// <br/>
		/// - <see cref="VerificationResult.SuccessRehashNeeded"/> if the values are equal, but the hash is outdated and should be rehashed.
		/// <br/>
		/// - <see cref="VerificationResult.Success"/> if the values are equal.
		/// </returns>
		VerificationResult Verify(ReadOnlySpan<byte> input, ReadOnlySpan<byte> hash);

		/// <summary>
		/// Compares two values in constant time, based on the length of <paramref name="b"/>.
		/// </summary>
		/// <returns>Wether the values are equal.</returns>
		bool SecureCompare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b);

		string GenerateStrongPassword();
		string GenerateRandomString(int length, string characters = RandomStringCharacters);
	}

	public enum VerificationResult : byte
	{
		/// <summary>
		/// The values are equal.
		/// </summary>
		Success,
		/// <summary>
		/// The values are not equal.
		///	</summary>
		Failed,
		/// <summary>
		/// The values are equal, but the hash is outdated and should be rehashed.
		/// </summary>
		SuccessRehashNeeded
	}

	public static class CryptoHelperExtensions
	{
		/// <inheritdoc cref="ICryptoHelper.Hash(ReadOnlySpan{byte})"/>
		public static byte[] Hash(this ICryptoHelper hasher, string input)
		{
			var size = Encoding.UTF8.GetByteCount(input);
			Span<byte> inputSpan = stackalloc byte[size];
			Encoding.UTF8.GetBytes(input, inputSpan);
			try
			{
				return [.. hasher.Hash(inputSpan)];
			} finally
			{
				CryptographicOperations.ZeroMemory(inputSpan);
			}
		}

		/// <inheritdoc cref="ICryptoHelper.Verify(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
		public static VerificationResult Verify(this ICryptoHelper hasher, string input, Span<byte> hash)
		{
			var size = Encoding.UTF8.GetByteCount(input);
			Span<byte> inputSpan = stackalloc byte[size];
			Encoding.UTF8.GetBytes(input, inputSpan);

			try
			{
				return hasher.Verify(inputSpan, hash);

			} finally
			{
				CryptographicOperations.ZeroMemory(inputSpan);
			}
		}

		/// <inheritdoc cref="ICryptoHelper.SecureCompare(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
		public static bool SecureCompare(this ICryptoHelper hasher, string a, string b)
		{
			var aSize = Encoding.UTF8.GetByteCount(a);
			Span<byte> aSpan = stackalloc byte[aSize];
			Encoding.UTF8.GetBytes(a, aSpan);

			var bSize = Encoding.UTF8.GetByteCount(b);
			Span<byte> bSpan = stackalloc byte[bSize];
			Encoding.UTF8.GetBytes(b, bSpan);

			try
			{
				return hasher.SecureCompare(aSpan, bSpan);
			} finally
			{
				CryptographicOperations.ZeroMemory(aSpan);
				CryptographicOperations.ZeroMemory(bSpan);
			}
		}
	}
}
