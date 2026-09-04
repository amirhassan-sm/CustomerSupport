using System.Net;
using Application.Framework.SearchBaseModel;
using Application.Contracts.QueryServices;
using Application.Dto.Customer;
using Application.Implementation;
using Customer.DomainServiceContract.Services;
using Domain.Customer.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Application.Tests;

public class CustomerServicesTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly Mock<ICustomerQueryService> _queryService = new();
    private readonly Mock<IValidator<CustomerCreateDto>> _createValidator = new();
    private readonly Mock<IValidator<CustomerUpdateDto>> _updateValidator = new();

    public CustomerServicesTests()
    {
        SetupValid(_createValidator);
        SetupValid(_updateValidator);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_InvalidId_ReturnsBadRequest(int id)
    {
        var result = await CreateSut().GetByIdAsync(id);

        AssertFailed(result, "INVALID_ID", HttpStatusCode.BadRequest, "Invalid customer id.");
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_MissingCustomer_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().GetByIdAsync(7);

        AssertNotFound(result, 7);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCustomer_ReturnsMappedDto()
    {
        var customer = ExistingCustomer();
        _repo.Setup(r => r.GetByIdAsync(customer.CustomerId)).ReturnsAsync(customer);

        var result = await CreateSut().GetByIdAsync(customer.CustomerId);

        Assert.True(result.Success);
        Assert.Equal(customer.CustomerId, result.RecordId);
        Assert.Equal("Customer retrieved successfully.", result.Message);
        AssertMapped(customer, result.Item);
    }

    [Fact]
    public async Task GetAllAsync_NoCustomers_ReturnsEmptyList()
    {
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<CustomerEntity>());

        var result = await CreateSut().GetAllAsync();

        Assert.True(result.Success);
        Assert.Equal("Customers retrieved successfully.", result.Message);
        Assert.NotNull(result.Item);
        Assert.Empty(result.Item);
    }

    [Fact]
    public async Task GetAllAsync_ExistingCustomers_ReturnsMappedDtos()
    {
        var customers = new[]
        {
            ExistingCustomer(1, "Ada", "Lovelace", "ada@example.com"),
            ExistingCustomer(2, "Grace", "Hopper", "grace@example.com")
        };
        _repo.Setup(r => r.GetAllAsync()).ReturnsAsync(customers);

        var result = await CreateSut().GetAllAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.Item!.Count);
        AssertMapped(customers[0], result.Item[0]);
        AssertMapped(customers[1], result.Item[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByEmailAsync_MissingEmail_ReturnsBadRequest(string? email)
    {
        var result = await CreateSut().GetByEmailAsync(email!);

        AssertFailed(result, "INVALID_EMAIL", HttpStatusCode.BadRequest, "Email is required.");
        _repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetByEmailAsync_MissingCustomer_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().GetByEmailAsync("  ADA@example.com  ");

        Assert.False(result.Success);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal(HttpStatusCode.NotFound, result.statusCode);
        Assert.Contains("No customer found with email 'ADA@example.com'.", result.Errors);
        _repo.Verify(r => r.GetByEmailAsync("ada@example.com"), Times.Once);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingCustomer_NormalizesEmailAndReturnsMappedDto()
    {
        var customer = ExistingCustomer();
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync(customer);

        var result = await CreateSut().GetByEmailAsync("  ADA@EXAMPLE.COM  ");

        Assert.True(result.Success);
        Assert.Equal(customer.CustomerId, result.RecordId);
        AssertMapped(customer, result.Item);
    }

    [Fact]
    public async Task CreateAsync_NullModel_ReturnsBadRequest()
    {
        var result = await CreateSut().CreateAsync(null!);

        AssertFailed(result, "INVALID_INPUT", HttpStatusCode.BadRequest, "Customer data is required.");
        _repo.Verify(r => r.AddAsync(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidModel_ReturnsValidationError()
    {
        SetupInvalid(_createValidator, "First name is required.");

        var result = await CreateSut().CreateAsync(ValidCreateDto());

        AssertFailed(result, "VALIDATION_ERROR", HttpStatusCode.BadRequest, "Customer validation failed.");
        Assert.Contains("First name is required.", result.Errors);
        _repo.Verify(r => r.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DuplicateEmail_ReturnsConflict()
    {
        _repo.Setup(r => r.ExistsByEmailAsync("ada@example.com", null)).ReturnsAsync(true);

        var result = await CreateSut().CreateAsync(ValidCreateDto(email: "  ADA@EXAMPLE.COM  "));

        AssertFailed(result, "EMAIL_EXISTS", HttpStatusCode.Conflict, "Email already exists.");
        Assert.Contains("A customer with email 'ada@example.com' already exists.", result.Errors);
        _repo.Verify(r => r.AddAsync(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ValidModel_NormalizesFieldsAndPersists()
    {
        CustomerEntity? saved = null;
        _repo.Setup(r => r.ExistsByEmailAsync("ada@example.com", null)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<CustomerEntity>()))
            .Callback<CustomerEntity>(customer =>
            {
                customer.CustomerId = 42;
                saved = customer;
            })
            .Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await CreateSut().CreateAsync(new CustomerCreateDto
        {
            FirstName = "  Ada  ",
            LastName = "  Lovelace  ",
            CompanyName = "   ",
            PhoneNumber = "  555-0100  ",
            Email = "  ADA@EXAMPLE.COM  ",
            Type = default
        });
        var after = DateTime.UtcNow;

        Assert.True(result.Success);
        Assert.Equal(42, result.RecordId);
        Assert.Equal("Customer created successfully.", result.Message);
        Assert.NotNull(saved);
        Assert.Equal("Ada", saved!.FirstName);
        Assert.Equal("Lovelace", saved.LastName);
        Assert.Null(saved.CompanyName);
        Assert.Equal("555-0100", saved.PhoneNumber);
        Assert.Equal("ada@example.com", saved.Email);
        Assert.Equal(CustomerType.Individual, saved.Type);
        Assert.Equal(CustomerStatus.Active, saved.Status);
        Assert.False(saved.IsDeleted);
        Assert.InRange(saved.CreatedAt, before, after);
        Assert.NotNull(result.Item);
        Assert.Equal(42, result.Item.CustomerId);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_MissingEmail_ReturnsBadRequest()
    {
        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "  " });

        AssertFailed(result, "INVALID_INPUT", HttpStatusCode.BadRequest, "Customer email is required.");
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_InvalidCustomerId_ReturnsBadRequest()
    {
        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "ada@example.com", CustomerId = 0 });

        AssertFailed(result, "INVALID_ID", HttpStatusCode.BadRequest, "Invalid customer id.");
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_CustomerIdNotFound_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(9)).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "ada@example.com", CustomerId = 9 });

        AssertNotFound(result, 9);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_CustomerIdEmailMismatch_ReturnsBadRequest()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ExistingCustomer(email: "other@example.com"));

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "ada@example.com", CustomerId = 1 });

        Assert.False(result.Success);
        Assert.Equal(1, result.RecordId);
        Assert.Equal("EMAIL_MISMATCH", result.ErrorCode);
        Assert.Equal(HttpStatusCode.BadRequest, result.statusCode);
        Assert.Equal("Customer email does not match.", result.Message);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_MatchingCustomerId_LinksExistingCustomer()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ExistingCustomer(email: "ADA@example.com"));

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "ada@example.com", CustomerId = 1 });

        Assert.True(result.Success);
        Assert.Equal(1, result.RecordId);
        Assert.Equal(1, result.Item);
        Assert.Equal("Customer linked successfully.", result.Message);
        _repo.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_ExistingEmail_LinksWithoutCreating()
    {
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync(ExistingCustomer());

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "  ADA@EXAMPLE.COM  " });

        Assert.True(result.Success);
        Assert.Equal(1, result.Item);
        Assert.Equal("Customer linked successfully.", result.Message);
        _repo.Verify(r => r.AddAsync(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_MissingCustomerAndCreateDisabled_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto { Email = "ada@example.com" },
            createIfMissing: false);

        Assert.False(result.Success);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal(HttpStatusCode.NotFound, result.statusCode);
        Assert.Contains("No customer found with email 'ada@example.com'.", result.Errors);
        _repo.Verify(r => r.AddAsync(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_CreateFails_ForwardsFailure()
    {
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync((CustomerEntity?)null);
        SetupInvalid(_createValidator, "Phone number is required.");

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.com"
            });

        AssertFailed(result, "VALIDATION_ERROR", HttpStatusCode.BadRequest, "Customer validation failed.");
        Assert.Contains("Phone number is required.", result.Errors);
    }

    [Fact]
    public async Task ResolveOrCreateAccountCustomerAsync_MissingCustomer_CreatesIndividualCustomer()
    {
        _repo.Setup(r => r.GetByEmailAsync("ada@example.com")).ReturnsAsync((CustomerEntity?)null);
        _repo.Setup(r => r.ExistsByEmailAsync("ada@example.com", null)).ReturnsAsync(false);
        _repo.Setup(r => r.AddAsync(It.IsAny<CustomerEntity>()))
            .Callback<CustomerEntity>(customer => customer.CustomerId = 55)
            .Returns(Task.CompletedTask);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await CreateSut().ResolveOrCreateAccountCustomerAsync(
            new CustomerAccountLinkDto
            {
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.com",
                PhoneNumber = "555-0100"
            });

        Assert.True(result.Success);
        Assert.Equal(55, result.RecordId);
        Assert.Equal(55, result.Item);
        Assert.Equal("Customer created successfully.", result.Message);
        _repo.Verify(r => r.AddAsync(It.Is<CustomerEntity>(c =>
            c.Type == CustomerType.Individual &&
            c.Email == "ada@example.com" &&
            c.FirstName == "Ada")), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public async Task UpdateAsync_InvalidId_ReturnsBadRequest(int id)
    {
        var result = await CreateSut().UpdateAsync(id, ValidUpdateDto());

        AssertFailed(result, "INVALID_ID", HttpStatusCode.BadRequest, "Invalid customer id.");
        Assert.Equal(id, result.RecordId);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NullModel_ReturnsBadRequest()
    {
        var result = await CreateSut().UpdateAsync(1, null!);

        AssertFailed(result, "INVALID_INPUT", HttpStatusCode.BadRequest, "Customer data is required.");
        Assert.Equal(1, result.RecordId);
    }

    [Fact]
    public async Task UpdateAsync_InvalidModel_ReturnsValidationError()
    {
        SetupInvalid(_updateValidator, "Email is not valid.");

        var result = await CreateSut().UpdateAsync(1, ValidUpdateDto());

        AssertFailed(result, "VALIDATION_ERROR", HttpStatusCode.BadRequest, "Customer validation failed.");
        Assert.Equal(1, result.RecordId);
        Assert.Contains("Email is not valid.", result.Errors);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MissingCustomer_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().UpdateAsync(8, ValidUpdateDto());

        AssertNotFound(result, 8);
        _repo.Verify(r => r.Update(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateEmail_ReturnsConflict()
    {
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(ExistingCustomer());
        _repo.Setup(r => r.ExistsByEmailAsync("new@example.com", 1)).ReturnsAsync(true);

        var result = await CreateSut().UpdateAsync(1, ValidUpdateDto(email: "  NEW@EXAMPLE.COM  "));

        AssertFailed(result, "EMAIL_EXISTS", HttpStatusCode.Conflict, "Email already exists.");
        Assert.Equal(1, result.RecordId);
        _repo.Verify(r => r.Update(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidModel_NormalizesFieldsAndPersists()
    {
        var customer = ExistingCustomer();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _repo.Setup(r => r.ExistsByEmailAsync("ada@example.com", 1)).ReturnsAsync(false);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await CreateSut().UpdateAsync(1, new CustomerUpdateDto
        {
            FirstName = "  Ada  ",
            LastName = "  Lovelace  ",
            CompanyName = "  Analytical Engine  ",
            PhoneNumber = "  555-0199  ",
            Email = "  ADA@EXAMPLE.COM  ",
            Type = default,
            Status = CustomerStatus.Inactive
        });
        var after = DateTime.UtcNow;

        Assert.True(result.Success);
        Assert.Equal("Customer updated successfully.", result.Message);
        Assert.Equal("Ada", customer.FirstName);
        Assert.Equal("Lovelace", customer.LastName);
        Assert.Equal("Analytical Engine", customer.CompanyName);
        Assert.Equal("555-0199", customer.PhoneNumber);
        Assert.Equal("ada@example.com", customer.Email);
        Assert.Equal(CustomerType.Individual, customer.Type);
        Assert.Equal(CustomerStatus.Inactive, customer.Status);
        Assert.NotNull(customer.UpdatedAt);
        Assert.InRange(customer.UpdatedAt.Value, before, after);
        AssertMapped(customer, result.Item);
        _repo.Verify(r => r.Update(customer), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task ChangeStatusAsync_InvalidId_ReturnsBadRequest(int id)
    {
        var result = await CreateSut().ChangeStatusAsync(id, CustomerStatus.Inactive);

        AssertFailed(result, "INVALID_ID", HttpStatusCode.BadRequest, "Invalid customer id.");
        Assert.Equal(id, result.RecordId);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_UndefinedStatus_ReturnsBadRequest()
    {
        var result = await CreateSut().ChangeStatusAsync(1, (CustomerStatus)99);

        AssertFailed(result, "INVALID_STATUS", HttpStatusCode.BadRequest, "Invalid customer status.");
        Assert.Equal(1, result.RecordId);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_MissingCustomer_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().ChangeStatusAsync(3, CustomerStatus.Inactive);

        AssertNotFound(result, 3);
    }

    [Fact]
    public async Task ChangeStatusAsync_AlreadySet_DoesNotPersist()
    {
        var customer = ExistingCustomer();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);

        var result = await CreateSut().ChangeStatusAsync(1, CustomerStatus.Active);

        Assert.True(result.Success);
        Assert.True(result.Item);
        Assert.Equal("Customer status is already set.", result.Message);
        _repo.Verify(r => r.Update(It.IsAny<CustomerEntity>()), Times.Never);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task ChangeStatusAsync_NewStatus_PersistsChange()
    {
        var customer = ExistingCustomer();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var before = DateTime.UtcNow;
        var result = await CreateSut().ChangeStatusAsync(1, CustomerStatus.Inactive);
        var after = DateTime.UtcNow;

        Assert.True(result.Success);
        Assert.True(result.Item);
        Assert.Equal("Customer status updated successfully.", result.Message);
        Assert.Equal(CustomerStatus.Inactive, customer.Status);
        Assert.NotNull(customer.UpdatedAt);
        Assert.InRange(customer.UpdatedAt.Value, before, after);
        _repo.Verify(r => r.Update(customer), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-9)]
    public async Task DeleteAsync_InvalidId_ReturnsBadRequest(int id)
    {
        var result = await CreateSut().DeleteAsync(id);

        AssertFailed(result, "INVALID_ID", HttpStatusCode.BadRequest, "Invalid customer id.");
        Assert.Equal(id, result.RecordId);
        _repo.Verify(r => r.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_MissingCustomer_ReturnsNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(4)).ReturnsAsync((CustomerEntity?)null);

        var result = await CreateSut().DeleteAsync(4);

        AssertNotFound(result, 4);
        _repo.Verify(r => r.Delete(It.IsAny<CustomerEntity>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ExistingCustomer_DeletesAndSaves()
    {
        var customer = ExistingCustomer();
        _repo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(customer);
        _repo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await CreateSut().DeleteAsync(1);

        Assert.True(result.Success);
        Assert.True(result.Item);
        Assert.Equal(1, result.RecordId);
        Assert.Equal("Customer deleted successfully.", result.Message);
        _repo.Verify(r => r.Delete(customer), Times.Once);
        _repo.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Search_DelegatesToQueryService()
    {
        var searchModel = new CustomerSearchModel { Phrase = "ada" };
        var expected = new GenericComplexResult<CustomerSearchModel, CustomerResultDto>
        {
            SearchModel = searchModel,
            ListIteams = { new CustomerResultDto { CustomerId = 1, FirstName = "Ada" } }
        };
        _queryService.Setup(q => q.Search(searchModel)).ReturnsAsync(expected);

        var result = await CreateSut().Search(searchModel);

        Assert.Same(expected, result);
        _queryService.Verify(q => q.Search(searchModel), Times.Once);
    }

    private CustomerServices CreateSut()
    {
        return new CustomerServices(
            NullLogger<CustomerServices>.Instance,
            _repo.Object,
            _queryService.Object,
            _createValidator.Object,
            _updateValidator.Object);
    }

    private static void SetupValid<T>(Mock<IValidator<T>> validator)
    {
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());
    }

    private static void SetupInvalid<T>(Mock<IValidator<T>> validator, string error)
    {
        validator
            .Setup(v => v.ValidateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Property", error) }));
    }

    private static CustomerCreateDto ValidCreateDto(string email = "ada@example.com")
    {
        return new CustomerCreateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            PhoneNumber = "555-0100",
            Email = email,
            Type = CustomerType.Individual
        };
    }

    private static CustomerUpdateDto ValidUpdateDto(string email = "ada@example.com")
    {
        return new CustomerUpdateDto
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            PhoneNumber = "555-0100",
            Email = email,
            Type = CustomerType.Individual,
            Status = CustomerStatus.Active
        };
    }

    private static CustomerEntity ExistingCustomer(
        int id = 1,
        string firstName = "Ada",
        string lastName = "Lovelace",
        string email = "ada@example.com")
    {
        return new CustomerEntity
        {
            CustomerId = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PhoneNumber = "555-0100",
            Type = CustomerType.Individual,
            Status = CustomerStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }

    private static void AssertMapped(CustomerEntity customer, CustomerResultDto? dto)
    {
        Assert.NotNull(dto);
        Assert.Equal(customer.CustomerId, dto.CustomerId);
        Assert.Equal(customer.FirstName, dto.FirstName);
        Assert.Equal(customer.LastName, dto.LastName);
        Assert.Equal(customer.CompanyName, dto.CompanyName);
        Assert.Equal(customer.Email, dto.Email);
        Assert.Equal(customer.PhoneNumber, dto.PhoneNumber);
        Assert.Equal(customer.Type, dto.Type);
        Assert.Equal(customer.Status, dto.Status);
    }

    private static void AssertFailed<T>(
        Application.Framework.OperationResult.GenericOperationResult<T> result,
        string errorCode,
        HttpStatusCode statusCode,
        string message)
    {
        Assert.False(result.Success);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(statusCode, result.statusCode);
        Assert.Equal(message, result.Message);
        Assert.NotEmpty(result.Errors);
    }

    private static void AssertNotFound<T>(
        Application.Framework.OperationResult.GenericOperationResult<T> result,
        int id)
    {
        Assert.False(result.Success);
        Assert.Equal(id, result.RecordId);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
        Assert.Equal(HttpStatusCode.NotFound, result.statusCode);
        Assert.Equal("Customer not found.", result.Message);
        Assert.Contains($"No customer found with id '{id}'.", result.Errors);
    }
}
