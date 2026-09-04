using Application.Framework.OperationResult;
using Application.Framework.SearchBaseModel;
using Application.Contracts.QueryServices;
using Application.Contracts.Services;
using Application.Dto.Customer;
using Customer.DomainServiceContract.Services;
using Domain.Customer.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System.Net;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Application.Implementation
{
    public class CustomerServices : ICustomerServices
    {
        private readonly ILogger<CustomerServices> logger;
        private readonly ICustomerRepository repo;
        private readonly ICustomerQueryService queryService;
        private readonly IValidator<CustomerCreateDto> createValidator;
        private readonly IValidator<CustomerUpdateDto> updateValidator;

        public CustomerServices(
            ILogger<CustomerServices> logger,
            ICustomerRepository repo,
            ICustomerQueryService queryService,
            IValidator<CustomerCreateDto> createValidator,
            IValidator<CustomerUpdateDto> updateValidator)
        {
            this.logger = logger;
            this.repo = repo;
            this.queryService = queryService;
            this.createValidator = createValidator;
            this.updateValidator = updateValidator;
        }

        public async Task<GenericOperationResult<CustomerResultDto>> GetByIdAsync(int id)
        {
            if (id <= 0)
            {
                return Fail<CustomerResultDto>(
                    "Invalid customer id.",
                    "INVALID_ID",
                    HttpStatusCode.BadRequest,
                    "Customer id must be greater than zero.");
            }

            var customer = await repo.GetByIdAsync(id);
            if (customer is null)
                return NotFound<CustomerResultDto>(id);

            return GenericOperationResult<CustomerResultDto>.ToSuccess(
                customer.CustomerId,
                "Customer retrieved successfully.",
                Map(customer));
        }

        public async Task<GenericOperationResult<IReadOnlyList<CustomerResultDto>>> GetAllAsync()
        {
            var customers = await repo.GetAllAsync();
            var items = customers.Select(Map).ToList();

            return GenericOperationResult<IReadOnlyList<CustomerResultDto>>.ToSuccess(
                "Customers retrieved successfully.",
                items);
        }

        public async Task<GenericOperationResult<CustomerResultDto>> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Fail<CustomerResultDto>(
                    "Email is required.",
                    "INVALID_EMAIL",
                    HttpStatusCode.BadRequest,
                    "Email cannot be empty.");
            }

            var customer = await repo.GetByEmailAsync(NormalizeEmail(email));
            if (customer is null)
            {
                return GenericOperationResult<CustomerResultDto>.ToFail(
                    "Customer not found.",
                    new List<string> { $"No customer found with email '{email.Trim()}'." },
                    "NOT_FOUND",
                    HttpStatusCode.NotFound);
            }

            return GenericOperationResult<CustomerResultDto>.ToSuccess(
                customer.CustomerId,
                "Customer retrieved successfully.",
                Map(customer));
        }

        public async Task<GenericOperationResult<CustomerResultDto>> CreateAsync(CustomerCreateDto model)
        {
            if (model is null)
            {
                return Fail<CustomerResultDto>(
                    "Customer data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Customer model cannot be null.");
            }

            var validation = await createValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<CustomerResultDto>(validation);

            var email = NormalizeEmail(model.Email);
            if (await repo.ExistsByEmailAsync(email))
            {
                return GenericOperationResult<CustomerResultDto>.ToFail(
                    "Email already exists.",
                    new List<string> { $"A customer with email '{email}' already exists." },
                    "EMAIL_EXISTS",
                    HttpStatusCode.Conflict);
            }

            var customer = new CustomerEntity
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                CompanyName = NormalizeOptional(model.CompanyName),
                PhoneNumber = model.PhoneNumber.Trim(),
                Email = email,
                Type = NormalizeType(model.Type),
                Status = CustomerStatus.Active,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            await repo.AddAsync(customer);
            await repo.SaveChangesAsync();

            logger.LogInformation("Customer {CustomerId} created.", customer.CustomerId);

            return GenericOperationResult<CustomerResultDto>.ToSuccess(
                customer.CustomerId,
                "Customer created successfully.",
                Map(customer));
        }

        public async Task<GenericOperationResult<int>> ResolveOrCreateAccountCustomerAsync(
            CustomerAccountLinkDto model,
            bool createIfMissing = true)
        {
            if (model is null || string.IsNullOrWhiteSpace(model.Email))
            {
                return Fail<int>(
                    "Customer email is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Email cannot be empty.");
            }

            var email = NormalizeEmail(model.Email);

            if (model.CustomerId.HasValue)
            {
                if (model.CustomerId.Value <= 0)
                {
                    return Fail<int>(
                        "Invalid customer id.",
                        "INVALID_ID",
                        HttpStatusCode.BadRequest,
                        "Customer id must be greater than zero.");
                }

                var byId = await repo.GetByIdAsync(model.CustomerId.Value);
                if (byId is null)
                    return NotFound<int>(model.CustomerId.Value);

                if (!string.Equals(byId.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    return GenericOperationResult<int>.ToFail(
                        byId.CustomerId,
                        "Customer email does not match.",
                        new List<string> { "The supplied customer id does not belong to this email." },
                        "EMAIL_MISMATCH",
                        HttpStatusCode.BadRequest);
                }

                return GenericOperationResult<int>.ToSuccess(
                    byId.CustomerId,
                    "Customer linked successfully.",
                    byId.CustomerId);
            }

            var existing = await repo.GetByEmailAsync(email);
            if (existing is not null)
            {
                return GenericOperationResult<int>.ToSuccess(
                    existing.CustomerId,
                    "Customer linked successfully.",
                    existing.CustomerId);
            }

            if (!createIfMissing)
            {
                return GenericOperationResult<int>.ToFail(
                    "Customer not found.",
                    new List<string> { $"No customer found with email '{email}'." },
                    "NOT_FOUND",
                    HttpStatusCode.NotFound);
            }

            var created = await CreateAsync(new CustomerCreateDto
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = email,
                PhoneNumber = model.PhoneNumber,
                Type = CustomerType.Individual
            });

            if (!created.Success || created.Item is null)
            {
                return GenericOperationResult<int>.ToFail(
                    created.Message,
                    created.Errors,
                    created.ErrorCode,
                    created.statusCode ?? HttpStatusCode.BadRequest);
            }

            return GenericOperationResult<int>.ToSuccess(
                created.Item.CustomerId,
                "Customer created successfully.",
                created.Item.CustomerId);
        }

        public async Task<GenericOperationResult<CustomerResultDto>> UpdateAsync(int id, CustomerUpdateDto model)
        {
            if (id <= 0)
            {
                return Fail<CustomerResultDto>(
                    "Invalid customer id.",
                    "INVALID_ID",
                    HttpStatusCode.BadRequest,
                    "Customer id must be greater than zero.",
                    id);
            }

            if (model is null)
            {
                return Fail<CustomerResultDto>(
                    "Customer data is required.",
                    "INVALID_INPUT",
                    HttpStatusCode.BadRequest,
                    "Customer model cannot be null.",
                    id);
            }

            var validation = await updateValidator.ValidateAsync(model);
            if (!validation.IsValid)
                return ValidationFailed<CustomerResultDto>(validation, id);

            var customer = await repo.GetByIdAsync(id);
            if (customer is null)
                return NotFound<CustomerResultDto>(id);

            var email = NormalizeEmail(model.Email);
            if (await repo.ExistsByEmailAsync(email, id))
            {
                return GenericOperationResult<CustomerResultDto>.ToFail(
                    id,
                    "Email already exists.",
                    new List<string> { $"A customer with email '{email}' already exists." },
                    "EMAIL_EXISTS",
                    HttpStatusCode.Conflict);
            }

            customer.FirstName = model.FirstName.Trim();
            customer.LastName = model.LastName.Trim();
            customer.CompanyName = NormalizeOptional(model.CompanyName);
            customer.PhoneNumber = model.PhoneNumber.Trim();
            customer.Email = email;
            customer.Type = NormalizeType(model.Type);
            customer.Status = model.Status;
            customer.UpdatedAt = DateTime.UtcNow;

            repo.Update(customer);
            await repo.SaveChangesAsync();

            logger.LogInformation("Customer {CustomerId} updated.", customer.CustomerId);

            return GenericOperationResult<CustomerResultDto>.ToSuccess(
                customer.CustomerId,
                "Customer updated successfully.",
                Map(customer));
        }

        public async Task<GenericOperationResult<bool>> ChangeStatusAsync(int id, CustomerStatus status)
        {
            if (id <= 0)
            {
                return Fail<bool>(
                    "Invalid customer id.",
                    "INVALID_ID",
                    HttpStatusCode.BadRequest,
                    "Customer id must be greater than zero.",
                    id);
            }

            if (!Enum.IsDefined(status))
            {
                return Fail<bool>(
                    "Invalid customer status.",
                    "INVALID_STATUS",
                    HttpStatusCode.BadRequest,
                    $"Status '{status}' is not valid.",
                    id);
            }

            var customer = await repo.GetByIdAsync(id);
            if (customer is null)
                return NotFound<bool>(id);

            if (customer.Status == status)
            {
                return GenericOperationResult<bool>.ToSuccess(
                    id,
                    "Customer status is already set.",
                    true);
            }

            customer.Status = status;
            customer.UpdatedAt = DateTime.UtcNow;
            repo.Update(customer);
            await repo.SaveChangesAsync();

            logger.LogInformation("Customer {CustomerId} status changed to {Status}.", id, status);

            return GenericOperationResult<bool>.ToSuccess(
                id,
                "Customer status updated successfully.",
                true);
        }

        public async Task<GenericOperationResult<bool>> DeleteAsync(int id)
        {
            if (id <= 0)
            {
                return Fail<bool>(
                    "Invalid customer id.",
                    "INVALID_ID",
                    HttpStatusCode.BadRequest,
                    "Customer id must be greater than zero.",
                    id);
            }

            var customer = await repo.GetByIdAsync(id);
            if (customer is null)
                return NotFound<bool>(id);

            repo.Delete(customer);
            await repo.SaveChangesAsync();

            logger.LogInformation("Customer {CustomerId} deleted.", id);

            return GenericOperationResult<bool>.ToSuccess(
                id,
                "Customer deleted successfully.",
                true);
        }

        public Task<GenericComplexResult<CustomerSearchModel, CustomerResultDto>> Search(CustomerSearchModel sm)
        {
            return queryService.Search(sm);
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
                    "Customer validation failed.",
                    errors,
                    "VALIDATION_ERROR",
                    HttpStatusCode.BadRequest);
            }

            return GenericOperationResult<T>.ToFail(
                "Customer validation failed.",
                errors,
                "VALIDATION_ERROR",
                HttpStatusCode.BadRequest);
        }

        private static CustomerType NormalizeType(CustomerType type)
        {
            return type == default ? CustomerType.Individual : type;
        }

        private static string NormalizeEmail(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
                "Customer not found.",
                new List<string> { $"No customer found with id '{id}'." },
                "NOT_FOUND",
                HttpStatusCode.NotFound);
        }

        private static CustomerResultDto Map(CustomerEntity customer)
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
    }
}
