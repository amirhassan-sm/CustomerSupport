using Applicatio.Freamwork.SearchBaseModel;
using Application.Contrast.QueryServices;
using Application.Dto.Ticket;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Customer.Persistance.Query
{
    public class TicketQueryService : ITicketQueryServices
    {
        private readonly CustomerContext _db;

        public TicketQueryService(CustomerContext db)
        {
            _db = db;
        }

        public async Task<GenericComplexresult<TicketSearchModel, TicketResultDto>> Search(TicketSearchModel sm)
        {
            sm ??= new TicketSearchModel();

            var query = _db.Tickets.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(sm.Phrase))
            {
                var phrase = sm.Phrase.Trim();

                query = query.Where(t =>
                    t.Title.Contains(phrase) ||
                    t.Description.Contains(phrase));
            }

            if (sm.CustomerId is > 0)
                query = query.Where(t => t.CustomerId == sm.CustomerId.Value);

            if (sm.UnassignedOnly)
            {
                query = query.Where(t => t.AssignedAgentId == null);
            }
            else if (!string.IsNullOrWhiteSpace(sm.AssignedAgentId))
            {
                var agentId = sm.AssignedAgentId.Trim();
                query = query.Where(t => t.AssignedAgentId == agentId);
            }

            if (sm.Status.HasValue)
                query = query.Where(t => t.Status == sm.Status.Value);

            if (sm.Priority.HasValue)
                query = query.Where(t => t.Priority == sm.Priority.Value);

            if (sm.CreatedFrom.HasValue)
                query = query.Where(t => t.CreatedAt >= sm.CreatedFrom.Value);

            if (sm.CreatedTo.HasValue)
                query = query.Where(t => t.CreatedAt <= sm.CreatedTo.Value);

            sm.RecordCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenBy(t => t.TicketId)
                .Skip((sm.pageIndex - 1) * sm.pageSize)
                .Take(sm.pageSize)
                .Select(t => new TicketResultDto
                {
                    TicketId = t.TicketId,
                    CustomerId = t.CustomerId,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    AssignedAgentId = t.AssignedAgentId,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    ClosedAt = t.ClosedAt
                })
                .ToListAsync();

            return new GenericComplexresult<TicketSearchModel, TicketResultDto>
            {
                SearchModel = sm,
                ListIteams = items
            };
        }
    }
}
