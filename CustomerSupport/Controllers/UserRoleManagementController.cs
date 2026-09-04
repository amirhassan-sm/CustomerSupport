using Application.Framework.OperationResult;
using Application.Contracts.Authorization;
using Application.Contracts.Services;
using Application.Dto.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CustomerSupport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserRoleManagementController : ControllerBase
    {
        private readonly IUserRoleServices _userRoleServices;

        public UserRoleManagementController(IUserRoleServices userRoleServices)
        {
            _userRoleServices = userRoleServices;
        }

        [HttpGet("by-user/{userId}")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetRolesByUserId(string userId)
        {
            var result = await _userRoleServices.GetRolesByUserIdAsync(userId);
            return FromResult(result);
        }

        [HttpGet("by-role/{roleName}")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetUsersByRole(string roleName)
        {
            var result = await _userRoleServices.GetUsersByRoleAsync(roleName);
            return FromResult(result);
        }

        [HttpPost("assign")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Assign([FromBody] UserRoleDto model)
        {
            var result = await _userRoleServices.AssignRoleToUserAsync(model);
            return FromResult(result);
        }

        [HttpPost("remove")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Remove([FromBody] UserRoleDto model)
        {
            var result = await _userRoleServices.RemoveRoleFromUserAsync(model);
            return FromResult(result);
        }

        private IActionResult FromResult(OperationResult result)
        {
            if (result.Success)
                return Ok(result);

            var statusCode = (int)(result.statusCode ?? HttpStatusCode.BadRequest);
            return StatusCode(statusCode, result);
        }

        private IActionResult FromResult<T>(GenericOperationResult<T> result)
        {
            if (result.Success)
                return Ok(result);

            var statusCode = (int)(result.statusCode ?? HttpStatusCode.BadRequest);
            return StatusCode(statusCode, result);
        }
    }
}
