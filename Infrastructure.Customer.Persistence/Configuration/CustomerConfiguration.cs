using Domain.Customer.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Infrastructure.Customer.Persistence.Configuration
{
    public class CustomerConfiguration : IEntityTypeConfiguration<CustomerEntity>
    {
        public void Configure(EntityTypeBuilder<CustomerEntity> builder)
        {
            builder.ToTable("Customers");

            builder.HasKey(x => x.CustomerId);

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.CompanyName)
                .HasMaxLength(200);

            builder.Property(x => x.PhoneNumber)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Type)
                .HasConversion<int>()
                .HasDefaultValue(CustomerType.Individual);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(CustomerStatus.Active);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            builder.HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.HasIndex(x => x.PhoneNumber);
        }
    }
}
