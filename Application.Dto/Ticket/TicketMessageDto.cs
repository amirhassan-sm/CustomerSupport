using Domain.Customer.Enums;

namespace Application.Dto.Ticket
{
    public class TicketMessageDto
    {
        public int TicketMessageId { get; set; }

        public MessageSenderType SenderType { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
