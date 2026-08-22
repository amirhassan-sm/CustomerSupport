using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Security.Identity.Models
{
    public class ApplicationRole : IdentityRole
    {
        public string? Description { get; set; }
    }
}
