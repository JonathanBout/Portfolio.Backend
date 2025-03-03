namespace Portfolio.Backend.Data.Users
{
	public class RefreshToken
	{
		public uint Id { get; set; }
		public virtual User Owner { get; set; } = null!;
		public uint OwnerId { get; set; }
		public uint ValueId { get; set; }
		public RefreshTokenValue? Value
		{
			get
			{
				return HistoricalValues.FirstOrDefault(v => v.Id == ValueId);
			}
			set
			{
				if (value is null)
				{
					ValueId = 0;
				} else
				{
					ValueId = value.Id;
					HistoricalValues.Add(value);
				}
			}
		}

		public virtual IList<RefreshTokenValue> HistoricalValues { get; set; } = [];

		public DateTimeOffset LastUpdatedDate { get; set; } = DateTimeOffset.UtcNow;
		public DateTimeOffset ExpirationDate { get; set; } = DateTimeOffset.UtcNow.AddDays(30);
		public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

		public bool IsExpired() => DateTimeOffset.UtcNow > ExpirationDate || Value is null;
	}

	public class RefreshTokenValue
	{
		public uint Id { get; set; }
		public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
		public virtual RefreshToken ReferringToken { get; set; } = null!;
		public byte[] TokenHash { get; set; } = [];
	}
}
