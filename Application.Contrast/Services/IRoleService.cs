using Applicatio.Freamwork.OperationResult;
using Application.Dto.Security;

namespace Application.Contrast.Services
{
    public interface IRoleService
    {
        Task<OperationResult> AddRoleAsync(AddRoleDto model);

        Task<OperationResult> EditRoleAsync(EditRoleDto model);

        Task<OperationResult> DeleteRoleAsync(string id);

        Task<GenericOperationResult<RoleResultDto>> GetRoleByIdAsync(string id);

        Task<GenericOperationResult<IReadOnlyList<RoleResultDto>>> GetAllRolesAsync();
    }
}
