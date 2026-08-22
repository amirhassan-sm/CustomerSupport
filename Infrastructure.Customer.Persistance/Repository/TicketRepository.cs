using Customer.DomainServiceContract.Services;
using Domain.Customer.Entities;
using Domain.Customer.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Customer.Persistance.Repository
{
    public class TicketRepository : ITicketRepository
    {
        private readonly CustomerContext _db;

        public TicketRepository(CustomerContext db)
        {
            _db = db;
        }

        public async Task<Ticket?> GetByIdAsync(int id)
        {
            return await _db.Tickets.FirstOrDefaultAsync(t => t.TicketId == id);
        }

        public async Task<Ticket?> GetByIdWithDetailsAsync(int ticketId)
        {
            return await _db.Tickets
                .Include(t => t.Customer)
                .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
                .Include(t => t.StatusHistory.OrderBy(h => h.ChangedAt))
                .FirstOrDefaultAsync(t => t.TicketId == ticketId);
        }

        public async Task<bool> ExistsAsync(int ticketId)
        {
            return await _db.Tickets.AnyAsync(t => t.TicketId == ticketId);
        }

        public async Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(int customerId)
        {
            return await _db.Tickets
                .Where(t => t.CustomerId == customerId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.TicketId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Ticket>> GetByAssignedAgentIdAsync(string assignedAgentId)
        {
            return await _db.Tickets
                .Where(t => t.AssignedAgentId == assignedAgentId)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.TicketId)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Ticket>> GetByStatusAsync(TicketStatus status)
        {
            return await _db.Tickets
                .Where(t => t.Status == status)
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.TicketId)
                .ToListAsync();
        }

        public async Task AddAsync(Ticket model)
        {
            await _db.Tickets.AddAsync(model);
        }

        public async Task AddMessageAsync(TicketMessage message)
        {
            await _db.TicketMessages.AddAsync(message);
        }

        public async Task AddStatusHistoryAsync(TicketStatusHistory history)
        {
            await _db.TicketStatusHistories.AddAsync(history);
        }

        public void Update(Ticket model)
        {
            _db.Tickets.Update(model);
        }

        public void Delete(Ticket model)
        {
            _db.Tickets.Remove(model);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
