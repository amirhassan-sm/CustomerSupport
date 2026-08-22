using Applicatio.Freamwork.OperationResult;
using Application.Contrast.Services;
using Application.Dto.Security;
using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Infrastructure.Security.Identity.Services
{
    public class UserRoleServices : IUserRoleServices
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ILogger<UserRoleServices> logger;

        public UserRoleServices(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ILogger<UserRoleServices> logger)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.logger = logger;
        }

        public async Task<OperationResult> AssignRoleToUserAsync(UserRoleDto model)
        {
            var check = Validate(model);
            if (check is not null)
                return check;

            try
            {
                var user = await FindActiveUserAsync(model.UserId);
                if (user is null)
                    return UserNotFound();

                var roleName = model.RoleName.Trim();
                if (!await roleManager.RoleExistsAsync(roleName))
                    return RoleNotFound();

                if (await userManager.IsInRoleAsync(user, roleName))
                {
                    return OperationResult.ToFail(
                        "Failed to assign role.",
                        new List<string> { "User already has this role." },
                        "ROLE_ALREADY_ASSIGNED",
                        HttpStatusCode.Conflict);
                }

                var result = await userManager.AddToRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to assign role.",
                        result.Errors.Select(x => x.Description).ToList(),
                        "ASSIGN_ROLE_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess($"Role '{roleName}' assigned successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to assign role {RoleName} to user {UserId}.", model.RoleName, model.UserId);
                return Unexpected("Failed to assign role.");
            }
        }

        public async Task<OperationResult> RemoveRoleFromUserAsync(UserRoleDto model)
        {
            var check = Validate(model);
            if (check is not null)
                return check;

            try
            {
                var user = await FindActiveUserAsync(model.UserId);
                if (user is null)
                    return UserNotFound();

                var roleName = model.RoleName.Trim();
                if (!await roleManager.RoleExistsAsync(roleName))
                    return RoleNotFound();

                if (!await userManager.IsInRoleAsync(user, roleName))
                {
                    return OperationResult.ToFail(
                        "Failed to remove role.",
                        new List<string> { "User does not have this role." },
                        "ROLE_NOT_ASSIGNED",
                        HttpStatusCode.BadRequest);
                }

                var result = await userManager.RemoveFromRoleAsync(user, roleName);
                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to remove role.",
                        result.Errors.Select(x => x.Description).ToList(),
                        "REMOVE_ROLE_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess($"Role '{roleName}' removed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to remove role {RoleName} from user {UserId}.", model.RoleName, model.UserId);
                return Unexpected("Failed to remove role.");
            }
        }

        public async Task<GenericOperationResult<IReadOnlyList<string>>> GetRolesByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return GenericOperationResult<IReadOnlyList<string>>.ToFail(
                    "Failed to get roles.",
                    new List<string> { "User id is required." },
                    "INVALID_ID",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var user = await FindActiveUserAsync(userId);
                if (user is null)
                {
                    return GenericOperationResult<IReadOnlyList<string>>.ToFail(
                        "User not found.",
                        new List<string> { "This user does not exist." },
                        "USER_NOT_FOUND",
                        HttpStatusCode.NotFound);
                }

                var roles = await userManager.GetRolesAsync(user);
                return GenericOperationResult<IReadOnlyList<string>>.ToSuccess(
                    "Roles retrieved successfully.",
                    roles.ToList());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get roles for user {UserId}.", userId);
                return GenericOperationResult<IReadOnlyList<string>>.ToFail(
                    "Failed to get roles.",
                    new List<string> { "An unexpected error occurred." },
                    "EXCEPTION_OCCURRED",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericOperationResult<IReadOnlyList<UserProfileDto>>> GetUsersByRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return GenericOperationResult<IReadOnlyList<UserProfileDto>>.ToFail(
                    "Failed to get users.",
                    new List<string> { "Role name is required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var name = roleName.Trim();
                if (!await roleManager.RoleExistsAsync(name))
                {
                    return GenericOperationResult<IReadOnlyList<UserProfileDto>>.ToFail(
                        "Role not found.",
                        new List<string> { "This role does not exist." },
                        "ROLE_NOT_FOUND",
                        HttpStatusCode.NotFound);
                }

                var users = await userManager.GetUsersInRoleAsync(name);
                var items = new List<UserProfileDto>();

                foreach (var user in users.Where(x => !x.IsDeleted).OrderBy(x => x.UserName))
                {
                    var roles = await userManager.GetRolesAsync(user);
                    items.Add(Map(user, roles));
                }

                return GenericOperationResult<IReadOnlyList<UserProfileDto>>.ToSuccess(
                    "Users retrieved successfully.",
                    items);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get users for role {RoleName}.", roleName);
                return GenericOperationResult<IReadOnlyList<UserProfileDto>>.ToFail(
                    "Failed to get users.",
                    new List<string> { "An unexpected error occurred." },
                    "EXCEPTION_OCCURRED",
                    HttpStatusCode.InternalServerError);
            }
        }

        private async Task<ApplicationUser?> FindActiveUserAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null || user.IsDeleted)
                return null;

            return user;
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

        private static OperationResult? Validate(UserRoleDto model)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.UserId) || string.IsNullOrWhiteSpace(model.RoleName))
            {
                return OperationResult.ToFail(
                    "Invalid role assignment.",
                    new List<string> { "User id and role name are required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            return null;
        }

        private static OperationResult UserNotFound()
        {
            return OperationResult.ToFail(
                "User not found.",
                new List<string> { "This user does not exist." },
                "USER_NOT_FOUND",
                HttpStatusCode.NotFound);
        }

        private static OperationResult RoleNotFound()
        {
            return OperationResult.ToFail(
                "Role not found.",
                new List<string> { "This role does not exist." },
                "ROLE_NOT_FOUND",
                HttpStatusCode.NotFound);
        }

        private static OperationResult Unexpected(string message)
        {
            return OperationResult.ToFail(
                message,
                new List<string> { "An unexpected error occurred." },
                "EXCEPTION_OCCURRED",
                HttpStatusCode.InternalServerError);
        }
    }
}
