using Domain.Customer.Enums;

namespace Domain.Customer.Entities
{
    public class Ticket
    {
        public int TicketId { get; set; }

        public int CustomerId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        public string? AssignedAgentId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        public Customer Customer { get; set; } = null!;

        public ICollection<TicketMessage> Messages { get; set; }
            = new List<TicketMessage>();

        public ICollection<TicketStatusHistory> StatusHistory { get; set; }
            = new List<TicketStatusHistory>();
    }
}
