using Domain.Customer.Enums;

namespace Domain.Customer.Entities
{
    public class TicketStatusHistory
    {
        public int TicketStatusHistoryId { get; set; }

        public int TicketId { get; set; }

        public TicketStatus? FromStatus { get; set; }

        public TicketStatus ToStatus { get; set; }

        public string ChangedById { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; }

        public string? Description { get; set; }

        public Ticket Ticket { get; set; } = null!;
    }
}
