﻿using System.ComponentModel.DataAnnotations;

namespace Portfolio.Backend.Configuration
{
	public class AuthenticationConfiguration
	{
		[Required]
		public string Secret { get; set; } = null!;
		[Required]
		public string Issuer { get; set; } = null!;
		[Required]
		public string Audience { get; set; } = null!;

		[Range(1, int.MaxValue)]
		public int AccessTokenExpirationMinutes { get; set; } = 15;
		[Range(1, int.MaxValue)]
		public int RefreshTokenExpirationHours { get; set; } = 24 * 14;
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
