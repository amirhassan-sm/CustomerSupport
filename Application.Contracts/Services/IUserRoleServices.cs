using Application.Framework.OperationResult;
using Application.Dto.Security;

namespace Application.Contracts.Services
{
    public interface IUserRoleServices
    {
        Task<OperationResult> AssignRoleToUserAsync(UserRoleDto model);

        Task<OperationResult> RemoveRoleFromUserAsync(UserRoleDto model);

        Task<GenericOperationResult<IReadOnlyList<string>>> GetRolesByUserIdAsync(string userId);

        Task<GenericOperationResult<IReadOnlyList<UserProfileDto>>> GetUsersByRoleAsync(string roleName);
    }
}
