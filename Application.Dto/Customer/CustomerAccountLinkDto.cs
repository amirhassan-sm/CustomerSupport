namespace Application.Dto.Customer
{
    public class CustomerAccountLinkDto
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public int? CustomerId { get; set; }
    }
}
