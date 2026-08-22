namespace Application.Dto.Security
{
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;

        public string RefreshToken { get; set; } = string.Empty;
    }
}
