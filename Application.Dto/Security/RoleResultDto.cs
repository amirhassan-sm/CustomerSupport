namespace Application.Dto.Security
{
    public class RoleResultDto
    {
        public string RoleId { get; set; } = string.Empty;

        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
