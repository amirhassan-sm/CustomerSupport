using Application.Framework.OperationResult;
using Application.Dto.Security;

namespace Application.Contracts.Services
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
