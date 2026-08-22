using Applicatio.Freamwork.OperationResult;
using Application.Contrast.Authorization;
using Application.Contrast.Services;
using Application.Dto.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace CustomerSupport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserSevices _userServices;

        public UserManagementController(IUserSevices userServices)
        {
            _userServices = userServices;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            var result = await _userServices.GetProfileAsync(userId);
            return FromResult(result);
        }

        [HttpGet("{id}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _userServices.GetProfileAsync(id);
            return FromResult(result);
        }

        [HttpPost("search")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Search([FromBody] UserSearchModel searchModel)
        {
            var result = await _userServices.Search(searchModel);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto model)
        {
            if (model is null)
                return BadRequest();

            model.UserId = id;
            var result = await _userServices.UpdateProfileAsync(model);
            return FromResult(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _userServices.RemoveUserAsync(id);
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
