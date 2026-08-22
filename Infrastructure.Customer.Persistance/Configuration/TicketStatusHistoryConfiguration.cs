using Domain.Customer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Customer.Persistance.Configuration
{
    public class TicketStatusHistoryConfiguration : IEntityTypeConfiguration<TicketStatusHistory>
    {
        public void Configure(EntityTypeBuilder<TicketStatusHistory> builder)
        {
            builder.ToTable("TicketStatusHistories");

            builder.HasKey(x => x.TicketStatusHistoryId);

            builder.Property(x => x.FromStatus)
                .HasConversion<int>();

            builder.Property(x => x.ToStatus)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.ChangedById)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.ChangedAt)
                .IsRequired();

            builder.HasIndex(x => x.TicketId);
            builder.HasIndex(x => x.ChangedAt);
        }
    }
}
