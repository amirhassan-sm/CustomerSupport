using Microsoft.EntityFrameworkCore;
using Domain.Customer.Entities;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Infrastructure.Customer.Persistence
{
    public class CustomerContext : DbContext
    {
        public CustomerContext(DbContextOptions<CustomerContext> options)
            : base(options)
        {
        }

        public DbSet<CustomerEntity> Customers { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
        public DbSet<TicketStatusHistory> TicketStatusHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}
