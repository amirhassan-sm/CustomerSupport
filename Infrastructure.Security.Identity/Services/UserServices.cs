using Application.Framework.OperationResult;
using Application.Framework.SearchBaseModel;
using Application.Contracts.Services;
using Application.Dto.Security;
using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Infrastructure.Security.Identity.Services
{
    public class UserServices : IUserServices
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SecurityContext db;
        private readonly ILogger<UserServices> logger;

        public UserServices(
            UserManager<ApplicationUser> userManager,
            SecurityContext db,
            ILogger<UserServices> logger)
        {
            this.userManager = userManager;
            this.db = db;
            this.logger = logger;
        }

        public async Task<GenericOperationResult<UserProfileDto>> GetProfileAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return GenericOperationResult<UserProfileDto>.ToFail(
                    "Failed to get profile.",
                    new List<string> { "User id is required." },
                    "INVALID_ID",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null || user.IsDeleted)
                    return UserNotFound<UserProfileDto>();

                var roles = await userManager.GetRolesAsync(user);
                return GenericOperationResult<UserProfileDto>.ToSuccess(
                    "Profile retrieved successfully.",
                    Map(user, roles));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get profile {UserId}.", id);
                return Unexpected<UserProfileDto>("Failed to get profile.");
            }
        }

        public async Task<OperationResult> UpdateProfileAsync(UpdateUserDto profile)
        {
            if (profile is null || string.IsNullOrWhiteSpace(profile.UserId))
            {
                return OperationResult.ToFail(
                    "Failed to update profile.",
                    new List<string> { "User id is required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var user = await userManager.FindByIdAsync(profile.UserId);
                if (user is null || user.IsDeleted)
                {
                    return OperationResult.ToFail(
                        "User not found.",
                        new List<string> { "This user does not exist." },
                        "USER_NOT_FOUND",
                        HttpStatusCode.NotFound);
                }

                user.FirstName = profile.FirstName.Trim();
                user.LastName = profile.LastName.Trim();
                user.CustomerId = profile.CustomerId;

                if (!string.IsNullOrWhiteSpace(profile.UserName)
                    && !string.Equals(user.UserName, profile.UserName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var userNameResult = await userManager.SetUserNameAsync(user, profile.UserName.Trim());
                    if (!userNameResult.Succeeded)
                    {
                        return OperationResult.ToFail(
                            "Failed to update username.",
                            userNameResult.Errors.Select(x => x.Description).ToList(),
                            "UPDATE_USERNAME_FAILED",
                            HttpStatusCode.BadRequest);
                    }
                }

                if (!string.IsNullOrWhiteSpace(profile.Email)
                    && !string.Equals(user.Email, profile.Email.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    var emailResult = await userManager.SetEmailAsync(user, profile.Email.Trim());
                    if (!emailResult.Succeeded)
                    {
                        return OperationResult.ToFail(
                            "Failed to update email.",
                            emailResult.Errors.Select(x => x.Description).ToList(),
                            "UPDATE_EMAIL_FAILED",
                            HttpStatusCode.BadRequest);
                    }
                }

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to update profile.",
                        result.Errors.Select(x => x.Description).ToList(),
                        "UPDATE_PROFILE_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess("Profile updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update profile {UserId}.", profile.UserId);
                return OperationResult.ToFail(
                    "Failed to update profile.",
                    new List<string> { "An unexpected error occurred." },
                    "EXCEPTION_OCCURRED",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericComplexResult<UserSearchModel, UserProfileDto>> Search(UserSearchModel sm)
        {
            sm ??= new UserSearchModel();

            var query = db.Users.AsNoTracking().Where(x => !x.IsDeleted);

            if (!string.IsNullOrWhiteSpace(sm.Phrase))
            {
                var phrase = sm.Phrase.Trim();
                query = query.Where(x =>
                    x.UserName!.Contains(phrase) ||
                    x.Email!.Contains(phrase) ||
                    x.FirstName.Contains(phrase) ||
                    x.LastName.Contains(phrase));
            }

            if (!string.IsNullOrWhiteSpace(sm.RoleName))
            {
                var roleName = sm.RoleName.Trim();
                var userIdsInRole = from userRole in db.UserRoles
                                    join role in db.Roles on userRole.RoleId equals role.Id
                                    where role.Name == roleName
                                    select userRole.UserId;

                query = query.Where(x => userIdsInRole.Contains(x.Id));
            }

            sm.RecordCount = await query.CountAsync();

            var users = await query
                .OrderBy(x => x.UserName)
                .Skip((sm.pageIndex - 1) * sm.pageSize)
                .Take(sm.pageSize)
                .ToListAsync();

            var userIds = users.Select(x => x.Id).ToList();
            var roleRows = await (
                from userRole in db.UserRoles
                join role in db.Roles on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new { userRole.UserId, RoleName = role.Name ?? string.Empty }
            ).ToListAsync();

            var rolesByUser = roleRows
                .GroupBy(x => x.UserId)
                .ToDictionary(x => x.Key, x => (IList<string>)x.Select(r => r.RoleName).ToList());

            var items = users
                .Select(user => Map(
                    user,
                    rolesByUser.TryGetValue(user.Id, out var roles) ? roles : new List<string>()))
                .ToList();

            return new GenericComplexResult<UserSearchModel, UserProfileDto>
            {
                SearchModel = sm,
                ListIteams = items
            };
        }

        public async Task<OperationResult> RemoveUserAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return OperationResult.ToFail(
                    "Failed to remove user.",
                    new List<string> { "User id is required." },
                    "INVALID_ID",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var user = await userManager.FindByIdAsync(id);
                if (user is null)
                {
                    return OperationResult.ToFail(
                        "User not found.",
                        new List<string> { "This user does not exist." },
                        "USER_NOT_FOUND",
                        HttpStatusCode.NotFound);
                }

                if (user.IsDeleted)
                {
                    return OperationResult.ToFail(
                        "Failed to remove user.",
                        new List<string> { "This user is already deleted." },
                        "USER_ALREADY_DELETED",
                        HttpStatusCode.Conflict);
                }

                user.IsDeleted = true;
                user.RefreshToken = null;
                user.RefreshTokenExpiration = null;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to remove user.",
                        result.Errors.Select(x => x.Description).ToList(),
                        "REMOVE_USER_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess("User removed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove user {UserId}.", id);
                return OperationResult.ToFail(
                    "Failed to remove user.",
                    new List<string> { "An unexpected error occurred." },
                    "EXCEPTION_OCCURRED",
                    HttpStatusCode.InternalServerError);
            }
        }

        private static UserProfileDto Map(ApplicationUser user, IList<string> roles)
        {
            return new UserProfileDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                CustomerId = user.CustomerId,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = roles.ToList()
            };
        }

        private static GenericOperationResult<T> UserNotFound<T>()
        {
            return GenericOperationResult<T>.ToFail(
                "User not found.",
                new List<string> { "This user does not exist." },
                "USER_NOT_FOUND",
                HttpStatusCode.NotFound);
        }

        private static GenericOperationResult<T> Unexpected<T>(string message)
        {
            return GenericOperationResult<T>.ToFail(
                message,
                new List<string> { "An unexpected error occurred." },
                "EXCEPTION_OCCURRED",
                HttpStatusCode.InternalServerError);
        }
    }
}
