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
    [Authorize(Policy = AppPolicies.AdminOnly)]
    public class RoleManagementController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RoleManagementController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _roleService.GetAllRolesAsync();
            return FromResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var result = await _roleService.GetRoleByIdAsync(id);
            return FromResult(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AddRoleDto model)
        {
            var result = await _roleService.AddRoleAsync(model);
            return FromResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] EditRoleDto model)
        {
            if (model is null)
                return BadRequest();

            model.RoleId = id;
            var result = await _roleService.EditRoleAsync(model);
            return FromResult(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _roleService.DeleteRoleAsync(id);
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
