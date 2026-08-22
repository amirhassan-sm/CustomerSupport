using Applicatio.Freamwork.OperationResult;
using Applicatio.Freamwork.SearchBaseModel;
using Application.Dto.Security;

namespace Application.Contrast.Services
{
    public interface IUserSevices
    {
        Task<GenericOperationResult<UserProfileDto>> GetProfileAsync(string id);

        Task<OperationResult> UpdateProfileAsync(UpdateUserDto profile);

        Task<GenericComplexresult<UserSearchModel, UserProfileDto>> Search(UserSearchModel sm);

        Task<OperationResult> RemoveUserAsync(string id);
    }
}
