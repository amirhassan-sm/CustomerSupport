using Domain.Customer.Enums;

namespace Application.Dto.Ticket
{
    public class ChangeTicketStatusDto
    {
        public TicketStatus Status { get; set; }

        public string? Description { get; set; }
    }
}
