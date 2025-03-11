using Konscious.Security.Cryptography;
using Microsoft.Extensions.Options;
using Portfolio.Backend.Configuration;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Portfolio.Backend.Services.Implementation
{
	internal class CryptoHelper(IOptionsMonitor<CryptoConfiguration> cryptoOptions) : ICryptoHelper
	{
		// - all numbers are written in little-endian
		// - all hashes start with a 32-bit integer version number

		private readonly CryptoConfiguration _cryptoConfig = cryptoOptions.CurrentValue;

		//! update this when changing the hash format
		const int LATEST_HASH_VERSION = 1;

		public byte[] Hash(ReadOnlySpan<byte> input)
		{
			if (input.Length == 0)
				return [];

			return Hash_v1(input);
		}

		public VerificationResult Verify(ReadOnlySpan<byte> input, ReadOnlySpan<byte> hash)
		{
			if (input is { Length: 0 } || hash is { Length: 0 })
				return VerificationResult.Failed;

			var version = GetHashVersion(hash);

			var result = version switch
			{
				1 => Verify_v1(input, hash),
				_ => throw new InvalidOperationException($"Hash version {version} not implemented")
			};

			// if the hash was generated with a different version, we need to rehash
			if (result == VerificationResult.Success && version != LATEST_HASH_VERSION)
				return VerificationResult.SuccessRehashNeeded;

			return result;
		}

		/// <summary>
		/// Verifies the hash using the v1 hash system.
		/// </summary>
		public VerificationResult Verify_v1(ReadOnlySpan<byte> input, ReadOnlySpan<byte> hash)
		{
			var (keySize, saltSize, key, salt, iterations, memory, parallelism) = ParseHash_v1(hash);
			try
			{
				using var argon2 = CreateArgon2(input, iterations, memory, parallelism, salt);

				Span<byte> hashToCompare = new byte[keySize];

				argon2.GetBytes(keySize).CopyTo(hashToCompare);

				if (!SecureCompare(key, hashToCompare))
					return VerificationResult.Failed;

				// if the hash was generated with different parameters, we need to rehash
				// as it is likely not strong enough anymore
				if (iterations != _cryptoConfig.Iterations
					|| memory != _cryptoConfig.MemorySize
					|| parallelism != _cryptoConfig.Parallelism
					|| keySize != _cryptoConfig.KeySize
					|| saltSize != _cryptoConfig.SaltSize)
				{
					return VerificationResult.SuccessRehashNeeded;
				}

				return VerificationResult.Success;
			} finally
			{
				CryptographicOperations.ZeroMemory(key);
				CryptographicOperations.ZeroMemory(salt);
			}
		}

		/// <summary>
		/// Build an Argon2 instance with the given parameters.
		/// </summary>
		/// <param name="input">The input, for example the password</param>
		/// <param name="iterations">The iterations to use</param>
		/// <param name="memory">The amount of memory to use in kB</param>
		/// <param name="parallelism">The amount of parallelism to use</param>
		/// <param name="salt">The optional salt to use. If not specified, a random salt is generated.</param>
		/// <returns>The built Argon2 instance</returns>
		private Argon2id CreateArgon2(ReadOnlySpan<byte> input, int iterations, int memory, int parallelism, byte[]? salt = null)
		{
			var argon2 = new Argon2id(input.ToArray())
			{
				Salt = salt ?? GenerateSalt(),
				DegreeOfParallelism = parallelism,
				Iterations = iterations,
				MemorySize = memory
			};

			return argon2;
		}

		/// <summary>
		/// Compare two byte arrays in constant time. This is used to prevent timing attacks.
		/// The time is constant based on the length of the second array.
		/// </summary>
		public bool SecureCompare(ReadOnlySpan<byte> a, ReadOnlySpan<byte> b)
		{
			// don't do any short-circuiting here, we want to compare all the bytes,
			// or pretend to if the lengths are different.
			// this is a constant time comparison to prevent timing attacks,
			// where the time taken to compare can leak information about the
			// password hash.

			var equal = true;

			for (var i = 0; i < b.Length; i++)
			{
				if (a.Length <= i)
				{
					equal = false;
					continue;
				}

				equal &= a[i] == b[i];
			}

			return equal;
		}

		/// <summary>
		/// Parse the hash into its components.
		/// </summary>
		/// <param name="hash">The hash to parse</param>
		/// <returns>The hash components</returns>
		private static int GetHashVersion(ReadOnlySpan<byte> hash)
		{
			return BinaryPrimitives.ReadInt32LittleEndian(hash[..sizeof(int)]);
		}

		/// <summary>
		/// Hash the input using the v1 hash system.
		/// </summary>
		internal byte[] Hash_v1(ReadOnlySpan<byte> input)
		{
			/*
			 * v1 hash format:
			 * <version><key size><salt size><key><salt><iterations><memory><parallelism>
			 * Number conversions are all little-endian.
			 */

			using var argon2 = CreateArgon2(input, _cryptoConfig.Iterations, _cryptoConfig.MemorySize, _cryptoConfig.Parallelism);

			// efficiently allocate the exact size needed for the hash
			// keySize + saltSize + key + salt + iterations + memory + parallelism
			var resultHash = new byte[sizeof(int) * 3 + _cryptoConfig.KeySize + _cryptoConfig.SaltSize + sizeof(int) * 3];
			Span<byte> resultSpan = resultHash;

			// fill the resultHash with the hash data

			var offset = 0;

			// write the hash format version
			BinaryPrimitives.WriteInt32LittleEndian(resultSpan.Slice(offset, sizeof(int)), 1);

			offset += sizeof(int);

			// write the key size
			BinaryPrimitives.WriteInt32LittleEndian(resultSpan.Slice(offset, sizeof(int)), _cryptoConfig.KeySize);

			offset += sizeof(int);

			// write the salt size
			BinaryPrimitives.WriteInt32LittleEndian(resultSpan.Slice(offset, sizeof(int)), _cryptoConfig.SaltSize);

			offset += sizeof(int);

			// write the key
			argon2.GetBytes(_cryptoConfig.KeySize).CopyTo(resultSpan.Slice(offset, _cryptoConfig.KeySize));

			offset += _cryptoConfig.KeySize;

			// write the salt
			argon2.Salt.CopyTo(resultSpan.Slice(offset, _cryptoConfig.SaltSize));

			offset += _cryptoConfig.SaltSize;

			// write the iterations
			BinaryPrimitives.WriteInt32LittleEndian(resultSpan.Slice(offset, sizeof(int)), _cryptoConfig.Iterations);

			offset += sizeof(int);

			// write the memory
			BinaryPrimitives.WriteInt32LittleEndian(resultSpan.Slice(offset, sizeof(int)), _cryptoConfig.MemorySize);

			offset += sizeof(int);

			// write the parallelism
			BinaryPrimitives.WriteInt32LittleEndian(resultSpan.Slice(offset, sizeof(int)), _cryptoConfig.Parallelism);

			return resultHash;
		}

		/// <summary>
		/// Parse the hash in the v1 format to its components.
		/// </summary>
		internal static (int keySize, int saltSize, byte[] hash, byte[] salt, int iterations, int memory, int parallelism) ParseHash_v1(ReadOnlySpan<byte> hash)
		{
			var offset = sizeof(int); // skip the version number

			// read the key size
			var keySize = BinaryPrimitives.ReadInt32LittleEndian(hash.Slice(offset, sizeof(int)));
			offset += sizeof(int);

			// read the salt size
			var saltSize = BinaryPrimitives.ReadInt32LittleEndian(hash.Slice(offset, sizeof(int)));
			offset += sizeof(int);

			// read the key
			var key = hash.Slice(offset, keySize).ToArray();
			offset += keySize;

			// read the salt
			var salt = hash.Slice(offset, saltSize).ToArray();
			offset += saltSize;

			// read the iterations
			var iterations = BinaryPrimitives.ReadInt32LittleEndian(hash.Slice(offset, sizeof(int)));
			offset += sizeof(int);

			// read the memory
			var memory = BinaryPrimitives.ReadInt32LittleEndian(hash.Slice(offset, sizeof(int)));
			offset += sizeof(int);

			// read the parallelism
			var parallelism = BinaryPrimitives.ReadInt32LittleEndian(hash.Slice(offset, sizeof(int)));
			return (keySize, saltSize, key, salt, iterations, memory, parallelism);
		}

		internal byte[] GenerateSalt()
		{
			var salt = new byte[_cryptoConfig.SaltSize];
			RandomNumberGenerator.Fill(salt.AsSpan());
			return salt;
		}

		public string GenerateStrongPassword()
		{
			return GenerateRandomString(_cryptoConfig.StrongPasswordLength, _cryptoConfig.PasswordCharacters);
		}

		public string GenerateRandomString(int length, string characters = ICryptoHelper.RandomStringCharacters)
		{
			return RandomNumberGenerator.GetString(characters, length);
		}
	}
}
