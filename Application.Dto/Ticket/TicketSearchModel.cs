using Application.Framework.SearchBaseModel;
using Domain.Customer.Enums;

namespace Application.Dto.Ticket
{
    public class TicketSearchModel : PageModel
    {
        public string? Phrase { get; set; }

        public int? CustomerId { get; set; }

        public string? AssignedAgentId { get; set; }

        public bool UnassignedOnly { get; set; }

        public TicketStatus? Status { get; set; }

        public TicketPriority? Priority { get; set; }

        public DateTime? CreatedFrom { get; set; }

        public DateTime? CreatedTo { get; set; }
    }
}
