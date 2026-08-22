using Application.Dto.Customer;

namespace Application.Dto.Ticket
{
    public class TicketDetailsDto : TicketResultDto
    {
        public CustomerResultDto Customer { get; set; } = null!;

        public IReadOnlyList<TicketMessageDto> Messages { get; set; }
            = Array.Empty<TicketMessageDto>();

        public IReadOnlyList<TicketStatusHistoryDto> StatusHistory { get; set; }
            = Array.Empty<TicketStatusHistoryDto>();
    }
}
