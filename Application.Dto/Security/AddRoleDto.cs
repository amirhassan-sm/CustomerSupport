namespace Application.Dto.Security
{
    public class AddRoleDto
    {
        public string RoleName { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
