using Domain.Customer.Entities;
using Domain.Customer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Customer.DomainServiceContract.Services
{
    public interface ITicketRepository:BaseInterface.ICrudBaseInterface<Ticket,int>
    {
        Task SaveChangesAsync();
        Task<bool> ExistsAsync(int ticketId);
        Task<Ticket?> GetByIdWithDetailsAsync(int ticketId);
        Task<IReadOnlyList<Ticket>> GetByCustomerIdAsync(int customerId);
        Task<IReadOnlyList<Ticket>> GetByAssignedAgentIdAsync(string assignedAgentId);
        Task<IReadOnlyList<Ticket>> GetByStatusAsync(TicketStatus status);

        Task AddMessageAsync(TicketMessage message);

        Task AddStatusHistoryAsync(TicketStatusHistory history);
    }
}
