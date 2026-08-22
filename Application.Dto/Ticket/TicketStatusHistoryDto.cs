using Domain.Customer.Enums;

namespace Application.Dto.Ticket
{
    public class TicketStatusHistoryDto
    {
        public int TicketStatusHistoryId { get; set; }

        public TicketStatus? FromStatus { get; set; }

        public TicketStatus ToStatus { get; set; }

        public string ChangedById { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; }

        public string? Description { get; set; }
    }
}
