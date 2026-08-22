using Applicatio.Freamwork.OperationResult;
using Application.Contrast.Authorization;
using Application.Contrast.Services;
using Application.Dto.Common;
using Application.Dto.Customer;
using Domain.Customer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CustomerSupport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerServices _customerServices;

        public CustomerController(ICustomerServices customerServices)
        {
            _customerServices = customerServices;
        }

        [HttpGet("types")]
        [Authorize(Policy = AppPolicies.Staff)]
        public IActionResult GetTypes()
        {
            return Ok(GetEnumOptions<CustomerType>());
        }

        [HttpGet("statuses")]
        [Authorize(Policy = AppPolicies.Staff)]
        public IActionResult GetStatuses()
        {
            return Ok(GetEnumOptions<CustomerStatus>());
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _customerServices.GetByIdAsync(id);
            return FromResult(result);
        }

        [HttpGet]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetAll()
        {
            var result = await _customerServices.GetAllAsync();
            return FromResult(result);
        }

        [HttpGet("by-email")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var result = await _customerServices.GetByEmailAsync(email);
            return FromResult(result);
        }

        [HttpPost("search")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> Search([FromBody] CustomerSearchModel searchModel)
        {
            var result = await _customerServices.Search(searchModel);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Create([FromBody] CustomerCreateDto model)
        {
            var result = await _customerServices.CreateAsync(model);
            if (!result.Success)
                return FromResult(result);

            return CreatedAtAction(nameof(GetById), new { id = result.RecordId }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerUpdateDto model)
        {
            var result = await _customerServices.UpdateAsync(id, model);
            return FromResult(result);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeCustomerStatusDto model)
        {
            var result = await _customerServices.ChangeStatusAsync(id, model.Status);
            return FromResult(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _customerServices.DeleteAsync(id);
            return FromResult(result);
        }

        private IActionResult FromResult<T>(GenericOperationResult<T> result)
        {
            if (result.Success)
                return Ok(result);

            var statusCode = (int)(result.statusCode ?? HttpStatusCode.BadRequest);
            return StatusCode(statusCode, result);
        }

        private static IReadOnlyList<EnumOptionDto> GetEnumOptions<TEnum>()
            where TEnum : struct, Enum
        {
            return Enum.GetValues<TEnum>()
                .Select(value => new EnumOptionDto
                {
                    Name = value.ToString(),
                    Value = Convert.ToInt32(value)
                })
                .ToList();
        }
    }
}
