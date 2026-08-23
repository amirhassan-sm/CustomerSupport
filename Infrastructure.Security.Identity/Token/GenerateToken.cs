using Application.Contrast.Authorization;
using Application.Contrast.Services;
using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security.Identity.Token
{
    public class GenerateToken : IGenerateToken
    {
        private readonly IConfiguration config;
        private readonly UserManager<ApplicationUser> userManager;

        public GenerateToken(IConfiguration config, UserManager<ApplicationUser> userManager)
        {
            this.config = config;
            this.userManager = userManager;
        }

        public async Task<string> GenerateAcsessToken(
            string userId,
            string userName,
            string firstName,
            string lastName)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
                throw new InvalidOperationException("User not found.");

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, userName),
                new("firstName", firstName),
                new("lastName", lastName)
            };

            if (user.CustomerId.HasValue)
                claims.Add(new Claim(AppClaims.CustomerId, user.CustomerId.Value.ToString()));

            var roles = await userManager.GetRolesAsync(user);
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var secretKey = config["jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt secret key is missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var durationInMinutes = Convert.ToDouble(config["jwt:DurationInMinutes"] ?? "60");

            var token = new JwtSecurityToken(
                claims: claims,
                issuer: config["jwt:Issuer"],
                audience: config["jwt:Audience"],
                expires: DateTime.UtcNow.AddMinutes(durationInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
