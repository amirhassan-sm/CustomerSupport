using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Identity.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public int? CustomerId { get; set; }

        public string? RefreshToken { get; set; }

        public DateTime? RefreshTokenExpiration { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsDeleted { get; set; }
    }
}
