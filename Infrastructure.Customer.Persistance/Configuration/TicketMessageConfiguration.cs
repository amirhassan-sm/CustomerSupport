using Domain.Customer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Customer.Persistance.Configuration
{
    public class TicketMessageConfiguration : IEntityTypeConfiguration<TicketMessage>
    {
        public void Configure(EntityTypeBuilder<TicketMessage> builder)
        {
            builder.ToTable("TicketMessages");

            builder.HasKey(x => x.TicketMessageId);

            builder.Property(x => x.SenderType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Message)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.TicketId);
            builder.HasIndex(x => new { x.SenderType, x.SenderId });
        }
    }
}
