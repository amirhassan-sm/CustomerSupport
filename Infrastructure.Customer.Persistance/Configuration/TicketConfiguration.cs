using Domain.Customer.Entities;
using Domain.Customer.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Customer.Persistance.Configuration
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.ToTable("Tickets");

            builder.HasKey(x => x.TicketId);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .HasDefaultValue(TicketStatus.Open);

            builder.Property(x => x.Priority)
                .HasConversion<int>()
                .HasDefaultValue(TicketPriority.Medium);

            builder.Property(x => x.AssignedAgentId)
                .HasMaxLength(450);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Tickets)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Messages)
                .WithOne(x => x.Ticket)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.StatusHistory)
                .WithOne(x => x.Ticket)
                .HasForeignKey(x => x.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.CustomerId);
            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.AssignedAgentId);
        }
    }
}
