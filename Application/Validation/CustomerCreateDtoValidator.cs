using Application.Dto.Customer;
using Domain.Customer.Enums;
using FluentValidation;

namespace Application.Validation
{
    public class CustomerCreateDtoValidator : AbstractValidator<CustomerCreateDto>
    {
        public CustomerCreateDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("First name is required.")
                .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage("Last name is required.")
                .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .MaximumLength(256).WithMessage("Email cannot exceed 256 characters.")
                .EmailAddress().WithMessage("Email is not valid.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Phone number is required.")
                .MaximumLength(30).WithMessage("Phone number cannot exceed 30 characters.");

            RuleFor(x => x.Type)
                .Must(type => type == default || Enum.IsDefined(type))
                .WithMessage("Customer type is not valid.");

            RuleFor(x => x.CompanyName)
                .MaximumLength(200).WithMessage("Company name cannot exceed 200 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.CompanyName));

            When(x => ResolveType(x.Type) == CustomerType.Company, () =>
            {
                RuleFor(x => x.CompanyName)
                    .NotEmpty().WithMessage("Company name is required for company customers.");
            });
        }

        private static CustomerType ResolveType(CustomerType type)
        {
            return type == default ? CustomerType.Individual : type;
        }
    }
}
