using Domain.Customer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Dto.Customer
{
    public class CustomerResultDto
    {
        public int CustomerId { get; set; }

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string? CompanyName { get; set; }

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public CustomerType Type { get; set; }

        public CustomerStatus Status { get; set; }

    }
}
