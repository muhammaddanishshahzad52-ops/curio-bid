using CurioBid.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CurioBid.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Listing> Listings => Set<Listing>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Listing>(e =>
            {
                e.Property(l => l.StartingPrice).HasPrecision(18, 2);

                // Store the status as readable text ("Pending") instead of 0/1/2/3
                e.Property(l => l.Status).HasConversion<string>().HasMaxLength(20);

                // Two foreign keys to the same table. Restrict = a user who has
                // listings on record can't be silently deleted along with them.
                e.HasOne(l => l.Seller).WithMany()
                    .HasForeignKey(l => l.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(l => l.Reviewer).WithMany()
                    .HasForeignKey(l => l.ReviewerId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(l => new { l.ReviewerId, l.Status });
            });
        }
    }
}
