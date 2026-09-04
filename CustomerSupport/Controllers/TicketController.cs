using Application.Framework.OperationResult;
using Application.Contracts.Authorization;
using Application.Contracts.Services;
using Application.Dto.Common;
using Application.Dto.Ticket;
using Domain.Customer.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace CustomerSupport.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TicketController : ControllerBase
    {
        private readonly ITicketServices _ticketServices;

        public TicketController(ITicketServices ticketServices)
        {
            _ticketServices = ticketServices;
        }

        [HttpGet("statuses")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public IActionResult GetStatuses()
        {
            return Ok(GetEnumOptions<TicketStatus>());
        }

        [HttpGet("priorities")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public IActionResult GetPriorities()
        {
            return Ok(GetEnumOptions<TicketPriority>());
        }

        [HttpGet("sender-types")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public IActionResult GetSenderTypes()
        {
            return Ok(GetEnumOptions<MessageSenderType>());
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _ticketServices.GetByIdAsync(id);
            return FromCustomerScopedResult(result);
        }

        [HttpGet("{id:int}/details")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public async Task<IActionResult> GetByIdWithDetails(int id)
        {
            var result = await _ticketServices.GetByIdWithDetailsAsync(id);
            return FromCustomerScopedResult(result);
        }

        [HttpGet("by-customer/{customerId:int}")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public async Task<IActionResult> GetByCustomerId(int customerId)
        {
            if (IsCustomerOnly())
            {
                var linkedCustomerId = GetLinkedCustomerId();
                if (linkedCustomerId is null)
                {
                    return BadRequest(new
                    {
                        message = "This account is not linked to a customer record."
                    });
                }

                if (customerId != linkedCustomerId.Value)
                    return NotFound();
            }

            var result = await _ticketServices.GetByCustomerIdAsync(customerId);
            return FromResult(result);
        }

        [HttpGet("by-agent")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetByAssignedAgentId([FromQuery] string? assignedAgentId)
        {
            var agentId = string.IsNullOrWhiteSpace(assignedAgentId)
                ? RequireUserId()
                : assignedAgentId;

            if (agentId is null)
                return Unauthorized();

            var result = await _ticketServices.GetByAssignedAgentIdAsync(agentId);
            return FromResult(result);
        }

        [HttpGet("by-status/{status}")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> GetByStatus(TicketStatus status)
        {
            var result = await _ticketServices.GetByStatusAsync(status);
            return FromResult(result);
        }

        [HttpPost("search")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> Search([FromBody] TicketSearchModel searchModel)
        {
            var result = await _ticketServices.Search(searchModel);
            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public async Task<IActionResult> Create([FromBody] TicketCreateDto model)
        {
            if (model is null)
                return BadRequest();

            if (IsCustomerOnly())
            {
                var customerId = GetLinkedCustomerId();
                if (customerId is null)
                {
                    return BadRequest(new
                    {
                        message = "This account is not linked to a customer record."
                    });
                }

                model.CustomerId = customerId.Value;
            }

            var result = await _ticketServices.CreateAsync(model);
            if (!result.Success)
                return FromResult(result);

            return CreatedAtAction(nameof(GetById), new { id = result.RecordId }, result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> Update(int id, [FromBody] TicketUpdateDto model)
        {
            var result = await _ticketServices.UpdateAsync(id, model);
            return FromResult(result);
        }

        [HttpPatch("{id:int}/assign")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> Assign(int id, [FromBody] AssignTicketDto model)
        {
            var userId = RequireUserId();
            if (userId is null)
                return Unauthorized();

            var result = await _ticketServices.AssignAsync(id, model, userId);
            return FromResult(result);
        }

        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = AppPolicies.Staff)]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeTicketStatusDto model)
        {
            var userId = RequireUserId();
            if (userId is null)
                return Unauthorized();

            var result = await _ticketServices.ChangeStatusAsync(id, model, userId);
            return FromResult(result);
        }

        [HttpPost("{id:int}/messages")]
        [Authorize(Policy = AppPolicies.TicketAccess)]
        public async Task<IActionResult> AddMessage(int id, [FromBody] AddTicketMessageDto model)
        {
            var userId = RequireUserId();
            if (userId is null)
                return Unauthorized();

            if (IsCustomerOnly())
            {
                var ticket = await _ticketServices.GetByIdAsync(id);
                var scoped = FromCustomerScopedResult(ticket);
                if (scoped is not OkObjectResult)
                    return scoped;
            }

            var result = await _ticketServices.AddMessageAsync(
                id,
                model,
                userId,
                ResolveSenderType());
            return FromResult(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = AppPolicies.AdminOnly)]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _ticketServices.DeleteAsync(id);
            return FromResult(result);
        }

        private string? RequireUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        private bool IsCustomerOnly()
        {
            return User.IsInRole(AppRoles.Customer)
                && !User.IsInRole(AppRoles.Agent)
                && !User.IsInRole(AppRoles.Admin);
        }

        private int? GetLinkedCustomerId()
        {
            var value = User.FindFirstValue(AppClaims.CustomerId);
            return int.TryParse(value, out var customerId) ? customerId : null;
        }

        private bool CanAccessCustomerTicket(int ticketCustomerId)
        {
            if (!IsCustomerOnly())
                return true;

            var customerId = GetLinkedCustomerId();
            return customerId.HasValue && customerId.Value == ticketCustomerId;
        }

        private IActionResult FromCustomerScopedResult<T>(GenericOperationResult<T> result)
            where T : TicketResultDto
        {
            if (result.Success && result.Item is not null && !CanAccessCustomerTicket(result.Item.CustomerId))
                return NotFound();

            return FromResult(result);
        }

        private MessageSenderType ResolveSenderType()
        {
            if (User.IsInRole(AppRoles.Agent) || User.IsInRole(AppRoles.Admin))
                return MessageSenderType.Agent;

            return MessageSenderType.Customer;
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
