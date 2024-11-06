using Microsoft.EntityFrameworkCore;
using Portfolio.Backend.Data.Users;

namespace Portfolio.Backend.Services.Implementation
{
	public class DatabaseContext(DbContextOptions options) : DbContext(options)
	{
		public required DbSet<User> Users { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.UseIdentityAlwaysColumns();

			modelBuilder.Entity<User>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.HasIndex(e => e.Email).IsUnique();
				entity.Property(e => e.FullName).IsRequired();
				entity.Property(e => e.Email).IsRequired();
				entity.Property(e => e.PasswordHash).IsRequired();

				entity.OwnsMany(u => u.RefreshTokens, t =>
				{
					t.WithOwner(t => t.Owner);
					t.HasKey(t => t.Id);
					t.Property(t => t.TokenHash).IsRequired();
				});

				entity.Property(u => u.NameSlug).HasComputedColumnSql("""lower(regexp_replace(full_name, E'[^a-zA-Z0-9_]+', '-', 'gi'))""", stored: true);

				entity.HasData(new User
				{
					Id = 1,
					Email = "me@jonathanbout.com",
					FullName = "Jonathan Bout",
					Description = "Yes. It's me.",
				});
			});
		}
	}
}
