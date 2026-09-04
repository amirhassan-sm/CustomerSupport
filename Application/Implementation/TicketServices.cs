using Application.Framework.OperationResult;
using Application.Framework.SearchBaseModel;
using Application.Contracts.QueryServices;
using Application.Contracts.Services;
using Application.Dto.Customer;
using Application.Dto.Ticket;
using Customer.DomainServiceContract.Services;
using Domain.Customer.Entities;
using Domain.Customer.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System.Net;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Application.Implementation
{
    public class TicketServices : ITicketServices
    {
        private readonly ILogger<TicketServices> logger;
        private readonly ITicketRepository repo;
        private readonly ICustomerRepository customerRepo;
        private readonly ITicketQueryServices queryService;
        private readonly IValidator<TicketCreateDto> createValidator;
        private readonly IValidator<TicketUpdateDto> updateValidator;
        private readonly IValidator<AssignTicketDto> assignValidator;
        private readonly IValidator<ChangeTicketStatusDto> changeStatusValidator;
        private readonly IValidator<AddTicketMessageDto> addMessageValidator;

        public TicketServices(
            ILogger<TicketServices> logger,
            ITicketRepository repo,
            ICustomerRepository customerRepo,
            ITicketQueryServices queryService,
            IValidator<TicketCreateDto> createValidator,
            IValidator<TicketUpdateDto> updateValidator,
            IValidator<AssignTicketDto> assignValidator,
            IValidator<ChangeTicketStatusDto> changeStatusValidator,
            IValidator<AddTicketMessageDto> addMessageValidator)
        {
            this.logger = logger;
            this.repo = repo;
            this.customerRepo = customerRepo;
            this.queryService = queryService;
            this.createValidator = createValidator;
            this.updateValidator = updateValidator;
            this.assignValidator = assignValidator;
            this.changeStatusValidator = changeStatusValidator;
            this.addMessageValidator = addMessageValidator;
        }

        public async Task<GenericOperationResult<TicketResultDto>> GetByIdAsync(int id)
        {
            if (id <= 0)
                return InvalidId<TicketResultDto>();

            var ticket = await repo.GetByIdAsync(id);
            if (ticket is null)
                return NotFound<TicketResultDto>(id);

            return GenericOperationResult<TicketResultDto>.ToSuccess(
                ticket.TicketId,
                "Ticket retrieved successfully.",
                Map(ticket));
        }

        public async Task<GenericOperationResult<TicketDetailsDto>> GetByIdWithDetailsAsync(int ticketId)
        {
            if (ticketId <= 0)
                return InvalidId<TicketDetailsDto>();

            var ticket = await repo.GetByIdWithDetailsAsync(ticketId);
            if (ticket is null)
                return NotFound<TicketDetailsDto>(ticketId);

            return GenericOperationResult<TicketDetailsDto>.ToSuccess(
                ticket.TicketId,
                "Ticket retrieved successfully.",
                MapDetails(ticket));
        }

        public async Task<GenericOperationResult<IReadOnlyList<TicketResultDto>>> GetByCustomerIdAsync(int customerId)
        {
            if (customerId <= 0)
            {
                return Fail<IReadOnlyList<TicketResultDto>>(
                    "Invalid customer id.",
                    "INVALID_ID",
                    HttpStatusCode.BadRequest,
                    "Customer id must be greater than zero.");
            }

            var tickets = await repo.GetByCustomerIdAsync(customerId);
            var items = tickets.Select(Map).ToList();

            return GenericOperationResult<IReadOnlyList<TicketResultDto>>.ToSuccess(
                "Tickets retrieved successfully.",
                items);
        }

        public async Task<GenericOperationResult<IReadOnlyList<TicketResultDto>>> GetByAssignedAgentIdAsync(
            string assignedAgentId)
        {
            if (string.IsNullOrWhiteSpace(assignedAgentId))
            {
                return Fail<IReadOnlyList<TicketResultDto>>(
                    "Assigned agent id is required.",
                    "INVALID_AGENT_ID",
                    HttpStatusCode.BadRequest,
                    "Assigned agent id cannot be empty.");
            }

            var tickets = await repo.GetByAssignedAgentIdAsync(assignedAgentId.Trim());
            var items = tickets.Select(Map).ToList();

            return GenericOperationResult<IReadOnlyList<TicketResultDto>>.ToSuccess(
                "Tickets retrieved successfully.",
                items);
        }

        public async Task<GenericOperationResult<IReadOnlyList<TicketResultDto>>> GetByStatusAsync(TicketStatus status)
        {
            if (!Enum.IsDefined(status))
            {
                return Fail<IReadOnlyList<TicketResultDto>>(
                    "Invalid ticket status.",
                    "INVALID_STATUS",
                    HttpStatusCode.BadRequest,
                    $"Status '{status}' is not valid.");
            }

            var tickets = await repo.GetByStatusAsync(status);
            var items = tickets.Select(Map).ToList();

            return GenericOperationResult<IReadOnlyList<TicketResultDto>>.ToSuccess(
                "Tickets retrieved successfully.",
                items);
        }

        public async Task<GenericOperationResult<TicketResultDto>> CreateAsync(TicketCreateDto model)
        {
            if (model is null)
            {
                return Fail<TicketResultDto>(
                    "Ticket data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Ticket model cannot be null.");
            }

            var validation = await createValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<TicketResultDto>(validation);

            var customer = await customerRepo.GetByIdAsync(model.CustomerId);
            if (customer is null)
            {
                return GenericOperationResult<TicketResultDto>.ToFail(
                    "Customer not found.",
                    new List<string> { $"No customer found with id '{model.CustomerId}'." },
                    "NOT_FOUND",
                    HttpStatusCode.NotFound);
            }

            var now = DateTime.UtcNow;
            var ticket = new Ticket
            {
                CustomerId = model.CustomerId,
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Status = TicketStatus.Open,
                Priority = NormalizePriority(model.Priority),
                CreatedAt = now
            };

            ticket.StatusHistory.Add(new TicketStatusHistory
            {
                FromStatus = null,
                ToStatus = TicketStatus.Open,
                ChangedById = model.CustomerId.ToString(),
                ChangedAt = now,
                Description = "Ticket created."
            });

            await repo.AddAsync(ticket);
            await repo.SaveChangesAsync();

            logger.LogInformation("Ticket {TicketId} created for customer {CustomerId}.", ticket.TicketId, customer.CustomerId);

            return GenericOperationResult<TicketResultDto>.ToSuccess(
                ticket.TicketId,
                "Ticket created successfully.",
                Map(ticket));
        }

        public async Task<GenericOperationResult<TicketResultDto>> UpdateAsync(int id, TicketUpdateDto model)
        {
            if (id <= 0)
                return InvalidId<TicketResultDto>(id);

            if (model is null)
            {
                return Fail<TicketResultDto>(
                    "Ticket data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Ticket model cannot be null.",
                    id);
            }

            var validation = await updateValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<TicketResultDto>(validation, id);

            var ticket = await repo.GetByIdAsync(id);
            if (ticket is null)
                return NotFound<TicketResultDto>(id);

            if (IsClosed(ticket))
                return Closed<TicketResultDto>(id);

            ticket.Title = model.Title.Trim();
            ticket.Description = model.Description.Trim();
            ticket.Priority = NormalizePriority(model.Priority);
            ticket.UpdatedAt = DateTime.UtcNow;

            repo.Update(ticket);
            await repo.SaveChangesAsync();

            logger.LogInformation("Ticket {TicketId} updated.", ticket.TicketId);

            return GenericOperationResult<TicketResultDto>.ToSuccess(
                ticket.TicketId,
                "Ticket updated successfully.",
                Map(ticket));
        }

        public async Task<GenericOperationResult<TicketResultDto>> AssignAsync(
            int id,
            AssignTicketDto model,
            string changedById)
        {
            if (id <= 0)
                return InvalidId<TicketResultDto>(id);

            if (model is null)
            {
                return Fail<TicketResultDto>(
                    "Assignment data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Assignment model cannot be null.",
                    id);
            }

            var actorError = RequireActor<TicketResultDto>(changedById, id);
            if (actorError is not null)
                return actorError;

            var validation = await assignValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<TicketResultDto>(validation, id);

            var ticket = await repo.GetByIdAsync(id);
            if (ticket is null)
                return NotFound<TicketResultDto>(id);

            if (IsClosed(ticket))
                return Closed<TicketResultDto>(id);

            var agentId = model.AssignedAgentId.Trim();
            if (string.Equals(ticket.AssignedAgentId, agentId, StringComparison.Ordinal)
                && ticket.Status != TicketStatus.Open)
            {
                return GenericOperationResult<TicketResultDto>.ToSuccess(
                    id,
                    "Ticket is already assigned to this agent.",
                    Map(ticket));
            }

            var now = DateTime.UtcNow;
            ticket.AssignedAgentId = agentId;
            ticket.UpdatedAt = now;

            if (ticket.Status == TicketStatus.Open)
            {
                await AddHistoryAsync(ticket, TicketStatus.Assigned, changedById.Trim(), now, "Ticket assigned.");
                ticket.Status = TicketStatus.Assigned;
            }

            repo.Update(ticket);
            await repo.SaveChangesAsync();

            logger.LogInformation("Ticket {TicketId} assigned to {AgentId}.", ticket.TicketId, agentId);

            return GenericOperationResult<TicketResultDto>.ToSuccess(
                ticket.TicketId,
                "Ticket assigned successfully.",
                Map(ticket));
        }

        public async Task<GenericOperationResult<TicketResultDto>> ChangeStatusAsync(
            int id,
            ChangeTicketStatusDto model,
            string changedById)
        {
            if (id <= 0)
                return InvalidId<TicketResultDto>(id);

            if (model is null)
            {
                return Fail<TicketResultDto>(
                    "Status data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Status model cannot be null.",
                    id);
            }

            var actorError = RequireActor<TicketResultDto>(changedById, id);
            if (actorError is not null)
                return actorError;

            var validation = await changeStatusValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<TicketResultDto>(validation, id);

            var ticket = await repo.GetByIdAsync(id);
            if (ticket is null)
                return NotFound<TicketResultDto>(id);

            if (ticket.Status == model.Status)
            {
                return GenericOperationResult<TicketResultDto>.ToSuccess(
                    id,
                    "Ticket status is already set.",
                    Map(ticket));
            }

            if (model.Status == TicketStatus.Assigned
                && string.IsNullOrWhiteSpace(ticket.AssignedAgentId))
            {
                return Fail<TicketResultDto>(
                    "Ticket is not assigned.",
                    "TICKET_NOT_ASSIGNED",
                    HttpStatusCode.Conflict,
                    "Assign an agent before setting status to Assigned.",
                    id);
            }

            var now = DateTime.UtcNow;
            await AddHistoryAsync(
                ticket,
                model.Status,
                changedById.Trim(),
                now,
                NormalizeOptional(model.Description));

            ticket.Status = model.Status;
            ticket.UpdatedAt = now;
            ticket.ClosedAt = model.Status == TicketStatus.Closed ? now : null;

            repo.Update(ticket);
            await repo.SaveChangesAsync();

            logger.LogInformation("Ticket {TicketId} status changed to {Status}.", id, model.Status);

            return GenericOperationResult<TicketResultDto>.ToSuccess(
                ticket.TicketId,
                "Ticket status updated successfully.",
                Map(ticket));
        }

        public async Task<GenericOperationResult<TicketMessageDto>> AddMessageAsync(
            int ticketId,
            AddTicketMessageDto model,
            string senderId,
            MessageSenderType senderType)
        {
            if (ticketId <= 0)
                return InvalidId<TicketMessageDto>();

            if (model is null)
            {
                return Fail<TicketMessageDto>(
                    "Message data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Message model cannot be null.",
                    ticketId);
            }

            var actorError = RequireActor<TicketMessageDto>(senderId, ticketId);
            if (actorError is not null)
                return actorError;

            if (!Enum.IsDefined(senderType))
            {
                return Fail<TicketMessageDto>(
                    "Invalid sender type.",
                    "INVALID_SENDER_TYPE",
                    HttpStatusCode.BadRequest,
                    $"Sender type '{senderType}' is not valid.",
                    ticketId);
            }

            var validation = await addMessageValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<TicketMessageDto>(validation, ticketId);

            var ticket = await repo.GetByIdAsync(ticketId);
            if (ticket is null)
                return NotFound<TicketMessageDto>(ticketId);

            if (IsClosed(ticket))
                return Closed<TicketMessageDto>(ticketId);

            var now = DateTime.UtcNow;
            var message = new TicketMessage
            {
                TicketId = ticket.TicketId,
                SenderType = senderType,
                SenderId = senderId.Trim(),
                Message = model.Message.Trim(),
                CreatedAt = now
            };

            ticket.UpdatedAt = now;
            repo.Update(ticket);
            await repo.AddMessageAsync(message);
            await repo.SaveChangesAsync();

            logger.LogInformation("Message {MessageId} added to ticket {TicketId}.", message.TicketMessageId, ticketId);

            return GenericOperationResult<TicketMessageDto>.ToSuccess(
                message.TicketMessageId,
                "Message added successfully.",
                MapMessage(message));
        }

        public async Task<GenericOperationResult<bool>> DeleteAsync(int id)
        {
            if (id <= 0)
                return InvalidId<bool>(id);

            var ticket = await repo.GetByIdAsync(id);
            if (ticket is null)
                return NotFound<bool>(id);

            repo.Delete(ticket);
            await repo.SaveChangesAsync();

            logger.LogInformation("Ticket {TicketId} deleted.", id);

            return GenericOperationResult<bool>.ToSuccess(
                id,
                "Ticket deleted successfully.",
                true);
        }

        public Task<GenericComplexResult<TicketSearchModel, TicketResultDto>> Search(TicketSearchModel sm)
        {
            return queryService.Search(sm);
        }

        private async Task AddHistoryAsync(
            Ticket ticket,
            TicketStatus toStatus,
            string changedById,
            DateTime changedAt,
            string? description)
        {
            await repo.AddStatusHistoryAsync(new TicketStatusHistory
            {
                TicketId = ticket.TicketId,
                FromStatus = ticket.Status,
                ToStatus = toStatus,
                ChangedById = changedById,
                ChangedAt = changedAt,
                Description = description
            });
        }

        private static bool IsClosed(Ticket ticket)
        {
            return ticket.Status == TicketStatus.Closed;
        }

        private static TicketPriority NormalizePriority(TicketPriority priority)
        {
            return priority == default ? TicketPriority.Medium : priority;
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static GenericOperationResult<T>? RequireActor<T>(string? actorId, int recordId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return Fail<T>(
                    "Actor id is required.",
                    "INVALID_ACTOR",
                    HttpStatusCode.BadRequest,
                    "The current user id is required.",
                    recordId);
            }

            if (actorId.Trim().Length > 450)
            {
                return Fail<T>(
                    "Actor id is invalid.",
                    "INVALID_ACTOR",
                    HttpStatusCode.BadRequest,
                    "Actor id cannot exceed 450 characters.",
                    recordId);
            }

            return null;
        }

        private static GenericOperationResult<T> ValidationFailed<T>(
            ValidationResult result,
            int? recordId = null)
        {
            var errors = result.Errors
                .Select(error => error.ErrorMessage)
                .Distinct()
                .ToList();

            if (recordId.HasValue)
            {
                return GenericOperationResult<T>.ToFail(
                    recordId.Value,
                    "Ticket validation failed.",
                    errors,
                    "VALIDATION_ERROR",
                    HttpStatusCode.BadRequest);
            }

            return GenericOperationResult<T>.ToFail(
                "Ticket validation failed.",
                errors,
                "VALIDATION_ERROR",
                HttpStatusCode.BadRequest);
        }

        private static GenericOperationResult<T> InvalidId<T>(int? recordId = null)
        {
            return Fail<T>(
                "Invalid ticket id.",
                "INVALID_ID",
                HttpStatusCode.BadRequest,
                "Ticket id must be greater than zero.",
                recordId);
        }

        private static GenericOperationResult<T> Closed<T>(int id)
        {
            return GenericOperationResult<T>.ToFail(
                id,
                "Ticket is closed.",
                new List<string> { "Closed tickets cannot be modified. Reopen the ticket first." },
                "TICKET_CLOSED",
                HttpStatusCode.Conflict);
        }

        private static GenericOperationResult<T> Fail<T>(
            string message,
            string errorCode,
            HttpStatusCode statusCode,
            string error,
            int? recordId = null)
        {
            if (recordId.HasValue)
            {
                return GenericOperationResult<T>.ToFail(
                    recordId.Value,
                    message,
                    new List<string> { error },
                    errorCode,
                    statusCode);
            }

            return GenericOperationResult<T>.ToFail(
                message,
                new List<string> { error },
                errorCode,
                statusCode);
        }

        private static GenericOperationResult<T> NotFound<T>(int id)
        {
            return GenericOperationResult<T>.ToFail(
                id,
                "Ticket not found.",
                new List<string> { $"No ticket found with id '{id}'." },
                "NOT_FOUND",
                HttpStatusCode.NotFound);
        }

        private static TicketResultDto Map(Ticket ticket)
        {
            return new TicketResultDto
            {
                TicketId = ticket.TicketId,
                CustomerId = ticket.CustomerId,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                AssignedAgentId = ticket.AssignedAgentId,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ClosedAt = ticket.ClosedAt
            };
        }

        private static TicketDetailsDto MapDetails(Ticket ticket)
        {
            return new TicketDetailsDto
            {
                TicketId = ticket.TicketId,
                CustomerId = ticket.CustomerId,
                Title = ticket.Title,
                Description = ticket.Description,
                Status = ticket.Status,
                Priority = ticket.Priority,
                AssignedAgentId = ticket.AssignedAgentId,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ClosedAt = ticket.ClosedAt,
                Customer = MapCustomer(ticket.Customer),
                Messages = ticket.Messages.Select(MapMessage).ToList(),
                StatusHistory = ticket.StatusHistory.Select(MapHistory).ToList()
            };
        }

        private static CustomerResultDto MapCustomer(CustomerEntity customer)
        {
            return new CustomerResultDto
            {
                CustomerId = customer.CustomerId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                CompanyName = customer.CompanyName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                Type = customer.Type,
                Status = customer.Status
            };
        }

        private static TicketMessageDto MapMessage(TicketMessage message)
        {
            return new TicketMessageDto
            {
                TicketMessageId = message.TicketMessageId,
                SenderType = message.SenderType,
                SenderId = message.SenderId,
                Message = message.Message,
                CreatedAt = message.CreatedAt
            };
        }

        private static TicketStatusHistoryDto MapHistory(TicketStatusHistory history)
        {
            return new TicketStatusHistoryDto
            {
                TicketStatusHistoryId = history.TicketStatusHistoryId,
                FromStatus = history.FromStatus,
                ToStatus = history.ToStatus,
                ChangedById = history.ChangedById,
                ChangedAt = history.ChangedAt,
                Description = history.Description
            };
        }
    }
}
