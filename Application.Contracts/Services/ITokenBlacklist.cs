namespace Application.Contracts.Services
{
    public interface ITokenBlacklist
    {
        Task RevokeAsync(string jti, TimeSpan timeToLive);

        Task<bool> IsRevokedAsync(string jti);
    }
}
