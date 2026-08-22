using Domain.Customer.Enums;

namespace Domain.Customer.Entities
{
    public class TicketMessage
    {
        public int TicketMessageId { get; set; }

        public int TicketId { get; set; }

        public MessageSenderType SenderType { get; set; }

        public string SenderId { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public Ticket Ticket { get; set; } = null!;
    }
}
