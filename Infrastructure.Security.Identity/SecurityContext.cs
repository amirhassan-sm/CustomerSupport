using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.Identity
{
    public class SecurityContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public SecurityContext(DbContextOptions<SecurityContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.RefreshToken)
                    .HasMaxLength(500);

                entity.Property(x => x.IsDeleted)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(x => x.CreatedAt)
                    .IsRequired();

                entity.HasIndex(x => x.CustomerId);
            });

            builder.Entity<ApplicationRole>(entity =>
            {
                entity.Property(x => x.Description)
                    .HasMaxLength(200);
            });
        }
    }
}
