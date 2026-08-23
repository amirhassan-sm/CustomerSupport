using Applicatio.Freamwork.OperationResult;
using Application.Contrast.Authorization;
using Application.Contrast.Services;
using Application.Dto.Customer;
using Application.Dto.Security;
using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace Infrastructure.Security.Identity.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ICustomerServices customerServices;
        private readonly IGenerateToken generateToken;
        private readonly ITokenBlacklist tokenBlacklist;
        private readonly ILogger<AuthenticationService> logger;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ICustomerServices customerServices,
            IGenerateToken generateToken,
            ITokenBlacklist tokenBlacklist,
            ILogger<AuthenticationService> logger)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.customerServices = customerServices;
            this.generateToken = generateToken;
            this.tokenBlacklist = tokenBlacklist;
            this.logger = logger;
        }

        public async Task<GenericOperationResult<TokenResult>> LoginAsync(LoginDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.UserName) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return GenericOperationResult<TokenResult>.ToFail(
                    "Login failed.",
                    new List<string> { "Username and password are required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var userName = dto.UserName.Trim();
                var user = await userManager.FindByNameAsync(userName)
                    ?? await userManager.FindByEmailAsync(userName);

                if (user is null || user.IsDeleted || !await userManager.CheckPasswordAsync(user, dto.Password))
                {
                    return GenericOperationResult<TokenResult>.ToFail(
                        "Login failed.",
                        new List<string> { "Invalid username or password." },
                        "INVALID_CREDENTIALS",
                        HttpStatusCode.Unauthorized);
                }

                await TryLinkExistingCustomerAsync(user);

                return await IssueTokensAsync(user, "Login succeeded.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Login failed for {UserName}.", dto.UserName);
                return Unexpected<TokenResult>("Login failed.");
            }
        }

        public async Task<OperationResult> SignUpAsync(SignUpDto dto)
        {
            if (dto is null)
            {
                return OperationResult.ToFail(
                    "Sign up failed.",
                    new List<string> { "Sign up data is required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var customerResult = await customerServices.ResolveOrCreateAccountCustomerAsync(
                    new CustomerAccountLinkDto
                    {
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        Email = dto.Email,
                        PhoneNumber = dto.PhoneNumber,
                        CustomerId = dto.CustomerId
                    });

                if (!customerResult.Success)
                {
                    return OperationResult.ToFail(
                        customerResult.Message,
                        customerResult.Errors,
                        customerResult.ErrorCode ?? "SIGNUP_FAILED",
                        customerResult.statusCode ?? HttpStatusCode.BadRequest);
                }

                var user = new ApplicationUser
                {
                    UserName = dto.UserName.Trim(),
                    Email = dto.Email.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                    FirstName = dto.FirstName.Trim(),
                    LastName = dto.LastName.Trim(),
                    CustomerId = customerResult.Item,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                };

                var createResult = await userManager.CreateAsync(user, dto.Password);
                if (!createResult.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Sign up failed.",
                        IdentityErrors(createResult),
                        "SIGNUP_FAILED",
                        HttpStatusCode.BadRequest);
                }

                if (await roleManager.RoleExistsAsync(AppRoles.Customer))
                    await userManager.AddToRoleAsync(user, AppRoles.Customer);

                return OperationResult.ToSuccess("Sign up succeeded.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sign up failed for {UserName}.", dto.UserName);
                return UnexpectedOperation("Sign up failed.");
            }
        }

        public async Task<GenericOperationResult<TokenResult>> RefreshAsync(RefreshTokenDto dto)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return GenericOperationResult<TokenResult>.ToFail(
                    "Refresh failed.",
                    new List<string> { "Refresh token is required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var user = await userManager.Users
                    .FirstOrDefaultAsync(x => x.RefreshToken == dto.RefreshToken);

                if (user is null || user.IsDeleted)
                {
                    return GenericOperationResult<TokenResult>.ToFail(
                        "Refresh failed.",
                        new List<string> { "Invalid refresh token." },
                        "INVALID_REFRESH_TOKEN",
                        HttpStatusCode.Unauthorized);
                }

                if (user.RefreshTokenExpiration is null || user.RefreshTokenExpiration <= DateTime.UtcNow)
                {
                    return GenericOperationResult<TokenResult>.ToFail(
                        "Refresh failed.",
                        new List<string> { "Refresh token has expired." },
                        "REFRESH_TOKEN_EXPIRED",
                        HttpStatusCode.Unauthorized);
                }

                return await IssueTokensAsync(user, "Token refreshed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Refresh token failed.");
                return Unexpected<TokenResult>("Refresh failed.");
            }
        }

        public async Task<OperationResult> LogoutAsync(RefreshTokenDto dto, string? accessToken = null)
        {
            if (dto is null || string.IsNullOrWhiteSpace(dto.RefreshToken))
            {
                return OperationResult.ToFail(
                    "Logout failed.",
                    new List<string> { "Refresh token is required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                await RevokeAccessTokenAsync(accessToken);

                var user = await userManager.Users
                    .FirstOrDefaultAsync(x => x.RefreshToken == dto.RefreshToken);

                if (user is null)
                    return OperationResult.ToSuccess("Logout succeeded.");

                user.RefreshToken = null;
                user.RefreshTokenExpiration = null;
                await userManager.UpdateAsync(user);

                return OperationResult.ToSuccess("Logout succeeded.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Logout failed.");
                return UnexpectedOperation("Logout failed.");
            }
        }

        private async Task RevokeAccessTokenAsync(string? accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                return;

            try
            {
                var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
                var jti = jwt.Id;
                if (string.IsNullOrWhiteSpace(jti))
                    jti = jwt.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)?.Value;

                var timeToLive = jwt.ValidTo - DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(jti) && timeToLive > TimeSpan.Zero)
                    await tokenBlacklist.RevokeAsync(jti, timeToLive);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not revoke access token on logout.");
            }
        }

        private async Task TryLinkExistingCustomerAsync(ApplicationUser user)
        {
            if (user.CustomerId.HasValue || string.IsNullOrWhiteSpace(user.Email))
                return;

            var link = await customerServices.ResolveOrCreateAccountCustomerAsync(
                new CustomerAccountLinkDto
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber ?? string.Empty
                },
                createIfMissing: false);

            if (!link.Success)
                return;

            user.CustomerId = link.Item;
            var update = await userManager.UpdateAsync(user);
            if (!update.Succeeded)
                user.CustomerId = null;
        }

        private async Task<GenericOperationResult<TokenResult>> IssueTokensAsync(ApplicationUser user, string message)
        {
            var accessToken = await generateToken.GenerateAcsessToken(
                user.Id,
                user.UserName ?? string.Empty,
                user.FirstName,
                user.LastName);

            var refreshToken = generateToken.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiration = DateTime.UtcNow.Add(RefreshTokenLifetime);
            user.LastLoginAt = DateTime.UtcNow;

            var saveResult = await userManager.UpdateAsync(user);
            if (!saveResult.Succeeded)
            {
                return GenericOperationResult<TokenResult>.ToFail(
                    "Failed to save refresh token.",
                    IdentityErrors(saveResult),
                    "REFRESH_TOKEN_SAVE_FAILED",
                    HttpStatusCode.InternalServerError);
            }

            return GenericOperationResult<TokenResult>.ToSuccess(
                message,
                new TokenResult
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
        }

        private static List<string> IdentityErrors(IdentityResult result)
        {
            return result.Errors.Select(x => x.Description).ToList();
        }

        private static GenericOperationResult<T> Unexpected<T>(string message)
        {
            return GenericOperationResult<T>.ToFail(
                message,
                new List<string> { "An unexpected error occurred." },
                "EXCEPTION_OCCURRED",
                HttpStatusCode.InternalServerError);
        }

        private static OperationResult UnexpectedOperation(string message)
        {
            return OperationResult.ToFail(
                message,
                new List<string> { "An unexpected error occurred." },
                "EXCEPTION_OCCURRED",
                HttpStatusCode.InternalServerError);
        }
    }
}
