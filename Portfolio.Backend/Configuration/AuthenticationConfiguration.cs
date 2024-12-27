using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class AuthenticationConfiguration
	{
		public string? Secret { get; set; }
		public string? SecretFilePath { get; set; }

		public string GetSecret()
		{
			if (Secret is not null) return Secret;
			if (SecretFilePath is not null) return File.ReadAllText(SecretFilePath);

			throw new InvalidOperationException("Either Secret or SecretFilePath should be configured.");
		}

		[Required]
		public string Issuer { get; set; } = null!;
		[Required]
		public string Audience { get; set; } = null!;

		[Range(1, int.MaxValue)]
		public int AccessTokenExpirationMinutes { get; set; } = 1;
		/// <summary>
		/// The expiration date of a single refresh token
		/// </summary>
		[Range(1, int.MaxValue)]
		public int RefreshTokenExpirationHours { get; set; } = 72;
		[Range(1, int.MaxValue)]
		public int RefreshTokenLength { get; set; } = 64;

		[Range(1, int.MaxValue)]
		public int PasswordResetTokenLength { get; set; } = 6;

		[Range(1, int.MaxValue)]
		public int PasswordResetCooldown { get; set; } = 2;

		[Range(1, int.MaxValue)]
		public int PasswordResetTokenExpirationMinutes { get; set; } = 15;
	}
}
