using Application.Framework.OperationResult;
using Application.Contracts.Services;
using Application.Dto.Security;
using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Infrastructure.Security.Identity.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly ILogger<RoleService> logger;

        public RoleService(RoleManager<ApplicationRole> roleManager, ILogger<RoleService> logger)
        {
            this.roleManager = roleManager;
            this.logger = logger;
        }

        public async Task<OperationResult> AddRoleAsync(AddRoleDto model)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.RoleName))
            {
                return OperationResult.ToFail(
                    "Failed to add role.",
                    new List<string> { "Role name is required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var roleName = model.RoleName.Trim();
                if (await roleManager.RoleExistsAsync(roleName))
                {
                    return OperationResult.ToFail(
                        "Failed to add role.",
                        new List<string> { $"Role '{roleName}' already exists." },
                        "ROLE_EXISTS",
                        HttpStatusCode.Conflict);
                }

                var result = await roleManager.CreateAsync(new ApplicationRole
                {
                    Name = roleName,
                    Description = NormalizeOptional(model.Description)
                });

                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to add role.",
                        IdentityErrors(result),
                        "ADD_ROLE_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess("Role added successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to add role {RoleName}.", model.RoleName);
                return Unexpected("Failed to add role.");
            }
        }

        public async Task<OperationResult> EditRoleAsync(EditRoleDto model)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.RoleId) || string.IsNullOrWhiteSpace(model.RoleName))
            {
                return OperationResult.ToFail(
                    "Failed to update role.",
                    new List<string> { "Role id and name are required." },
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var role = await roleManager.FindByIdAsync(model.RoleId);
                if (role is null)
                    return RoleNotFound();

                role.Name = model.RoleName.Trim();
                role.Description = NormalizeOptional(model.Description);

                var result = await roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to update role.",
                        IdentityErrors(result),
                        "UPDATE_ROLE_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess("Role updated successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update role {RoleId}.", model.RoleId);
                return Unexpected("Failed to update role.");
            }
        }

        public async Task<OperationResult> DeleteRoleAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return OperationResult.ToFail(
                    "Failed to delete role.",
                    new List<string> { "Role id is required." },
                    "INVALID_ID",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                    return RoleNotFound();

                var result = await roleManager.DeleteAsync(role);
                if (!result.Succeeded)
                {
                    return OperationResult.ToFail(
                        "Failed to delete role.",
                        IdentityErrors(result),
                        "DELETE_ROLE_FAILED",
                        HttpStatusCode.BadRequest);
                }

                return OperationResult.ToSuccess("Role deleted successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete role {RoleId}.", id);
                return Unexpected("Failed to delete role.");
            }
        }

        public async Task<GenericOperationResult<RoleResultDto>> GetRoleByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return GenericOperationResult<RoleResultDto>.ToFail(
                    "Failed to get role.",
                    new List<string> { "Role id is required." },
                    "INVALID_ID",
                    HttpStatusCode.BadRequest);
            }

            try
            {
                var role = await roleManager.FindByIdAsync(id);
                if (role is null)
                {
                    return GenericOperationResult<RoleResultDto>.ToFail(
                        "Role not found.",
                        new List<string> { "This role does not exist." },
                        "ROLE_NOT_FOUND",
                        HttpStatusCode.NotFound);
                }

                return GenericOperationResult<RoleResultDto>.ToSuccess("Role retrieved successfully.", Map(role));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get role {RoleId}.", id);
                return GenericOperationResult<RoleResultDto>.ToFail(
                    "Failed to get role.",
                    new List<string> { "An unexpected error occurred." },
                    "EXCEPTION_OCCURRED",
                    HttpStatusCode.InternalServerError);
            }
        }

        public async Task<GenericOperationResult<IReadOnlyList<RoleResultDto>>> GetAllRolesAsync()
        {
            try
            {
                var roles = await roleManager.Roles
                    .AsNoTracking()
                    .OrderBy(x => x.Name)
                    .Select(x => new RoleResultDto
                    {
                        RoleId = x.Id,
                        RoleName = x.Name ?? string.Empty,
                        Description = x.Description
                    })
                    .ToListAsync();

                return GenericOperationResult<IReadOnlyList<RoleResultDto>>.ToSuccess(
                    "Roles retrieved successfully.",
                    roles);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get roles.");
                return GenericOperationResult<IReadOnlyList<RoleResultDto>>.ToFail(
                    "Failed to get roles.",
                    new List<string> { "An unexpected error occurred." },
                    "EXCEPTION_OCCURRED",
                    HttpStatusCode.InternalServerError);
            }
        }

        private static RoleResultDto Map(ApplicationRole role)
        {
            return new RoleResultDto
            {
                RoleId = role.Id,
                RoleName = role.Name ?? string.Empty,
                Description = role.Description
            };
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static List<string> IdentityErrors(IdentityResult result)
        {
            return result.Errors.Select(x => x.Description).ToList();
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
