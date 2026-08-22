namespace Application.Dto.Security
{
    public class UserProfileDto
    {
        public string UserId { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    }
}
