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

				entity.HasMany(u => u.RefreshTokens).WithOne(t => t.Owner).HasForeignKey(t => t.OwnerId).OnDelete(DeleteBehavior.Cascade);

				entity.Property(u => u.NameSlug).HasComputedColumnSql("lower(regexp_replace(full_name, E'[^a-zA-Z0-9_]+', '-', 'gi'))", stored: true);

				entity.HasData(new User
				{
					Id = 1,
					Email = "me@jonathanbout.com",
					FullName = "Jonathan Bout",
					Description = "Yes. It's me.",
				});
			});

			modelBuilder.Entity<RefreshToken>(entity =>
			{
				entity.HasKey(e => e.Id);
				entity.Property(e => e.ExpirationDate).IsRequired();
				entity.Property(e => e.CreationDate).IsRequired();
				entity.HasMany(e => e.HistoricalValues).WithOne(v => v.ReferringToken).OnDelete(DeleteBehavior.Cascade);
			});
		}
	}
}
