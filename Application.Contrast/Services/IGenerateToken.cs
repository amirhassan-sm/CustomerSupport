namespace Application.Contrast.Services
{
    public interface IGenerateToken
    {
        Task<string> GenerateAcsessToken(string userId, string userName, string firstName, string lastName);

        string GenerateRefreshToken();
    }
}
