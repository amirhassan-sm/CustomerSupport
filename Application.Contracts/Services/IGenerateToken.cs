namespace Application.Contracts.Services
{
    public interface IGenerateToken
    {
        Task<string> GenerateAccessToken(string userId, string userName, string firstName, string lastName);

        string GenerateRefreshToken();
    }
}
