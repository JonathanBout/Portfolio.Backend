using System.ComponentModel.DataAnnotations.Schema;

namespace Portfolio.Backend.Data.Users
{
	public class RefreshToken
	{
		public uint Id { get; set; }
		public virtual User Owner { get; set; } = null!;
		public uint OwnerId { get; set; }

		[NotMapped]
		public RefreshTokenValue? ActiveToken => Values.FirstOrDefault(v => !v.IsInvalidated());

		public virtual IList<RefreshTokenValue> Values { get; } = [];

		[NotMapped]
		public DateTimeOffset LastUpdatedDate => Values.Max(v => v.CreationDate);
		[NotMapped]
		public DateTimeOffset ExpirationDate => Values.Max(v => v.ExpirationDate);
		[NotMapped]
		public DateTimeOffset CreationDate => Values.Min(v => v.CreationDate);

		public bool IsExpired() => DateTimeOffset.UtcNow > ExpirationDate || ActiveToken is null;

		public void Invalidate()
		{
			foreach (var value in Values.Where(v => !v.IsInvalidated()))
			{
				value.Invalidate();
			}
		}

		public void NewValue(RefreshTokenValue newToken)
		{
			if (ActiveToken is not null && !newToken.IsInvalidated())
			{
				ActiveToken?.Invalidate();
			}

			Values.Add(newToken);
		}
	}

	public class RefreshTokenValue
	{
		public uint Id { get; set; }
		public uint ReferringTokenId { get; set; }
		public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
		public required DateTimeOffset ExpirationDate { get; set; }
		public virtual RefreshToken ReferringToken { get; set; } = null!;
		public byte[] TokenHash { get; set; } = [];

		public bool IsInvalidated()
		{
			return DateTimeOffset.UtcNow > ExpirationDate;
		}

		public void Invalidate()
		{
			ExpirationDate = DateTimeOffset.UtcNow;
		}
	}
}
