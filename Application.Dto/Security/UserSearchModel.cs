using Application.Framework.SearchBaseModel;

namespace Application.Dto.Security
{
    public class UserSearchModel : PageModel
    {
        public string? Phrase { get; set; }

        public string? RoleName { get; set; }
    }
}
