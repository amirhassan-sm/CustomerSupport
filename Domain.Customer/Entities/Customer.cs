using Domain.Customer.Enums;

namespace Domain.Customer.Entities
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public CustomerType Type { get; set; } = CustomerType.Individual;

        public CustomerStatus Status { get; set; } = CustomerStatus.Active;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        public ICollection<Ticket> Tickets { get; set; }
            = new List<Ticket>();
    }
}
