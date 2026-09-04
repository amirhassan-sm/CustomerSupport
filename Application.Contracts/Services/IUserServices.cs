using Application.Framework.OperationResult;
using Application.Framework.SearchBaseModel;
using Application.Dto.Security;

namespace Application.Contracts.Services
{
    public interface IUserServices
    {
        Task<GenericOperationResult<UserProfileDto>> GetProfileAsync(string id);

        Task<OperationResult> UpdateProfileAsync(UpdateUserDto profile);

        Task<GenericComplexResult<UserSearchModel, UserProfileDto>> Search(UserSearchModel sm);

        Task<OperationResult> RemoveUserAsync(string id);
    }
}
