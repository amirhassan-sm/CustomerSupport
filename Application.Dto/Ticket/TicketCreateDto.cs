using Domain.Customer.Enums;

namespace Application.Dto.Ticket
{
    public class TicketCreateDto
    {
        public int CustomerId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    }
}
