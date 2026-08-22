using Domain.Customer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Ticket
{
    public class TicketResultDto
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
    }
}
