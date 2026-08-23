using Applicatio.Freamwork.OperationResult;
using Application.Dto.Security;

namespace Application.Contrast.Services
{
    public interface IAuthenticationService
    {
        Task<GenericOperationResult<TokenResult>> LoginAsync(LoginDto dto);

        Task<OperationResult> SignUpAsync(SignUpDto dto);

        Task<GenericOperationResult<TokenResult>> RefreshAsync(RefreshTokenDto dto);

        Task<OperationResult> LogoutAsync(RefreshTokenDto dto, string? accessToken = null);
    }
}
