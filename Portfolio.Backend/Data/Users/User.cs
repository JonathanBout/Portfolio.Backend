﻿using Microsoft.EntityFrameworkCore;
using Portfolio.Backend.Extensions;

namespace Portfolio.Backend.Data.Users
{
	public class User
	{
		public uint Id { get; set; }
		public required string FullName { get; set; }

		public string NameSlug { get; set; } = "";

		public string Description { get; set; } = "";
		/// <summary>
		/// The profile image of the user. If null, the system will try to use the user's Gravatar.
		/// </summary>
		public byte[]? ProfileImage { get; set; } = null;

		private string _email = "";

		public required string Email
		{
			get => _email;
			set => _email = value.Trim().ToLower();
		}

		public byte[] PasswordHash { get; set; } = [];

		public byte[] PasswordResetTokenHash { get; set; } = [];
		public DateTimeOffset PasswordResetExpiration { get; set; } = default;
		public DateTimeOffset LastPasswordResetRequest { get; set; } = default;

		public virtual IList<RefreshToken> RefreshTokens { get; set; } = [];
	}

	public class RefreshToken
	{
		public uint Id { get; set; }
		public virtual User Owner { get; set; } = null!;
		public uint OwnerId { get; set; }
		public byte[] TokenHash { get; set; } = [];

		public DateTimeOffset ExpirationDate { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
		public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
	}
}