using Application.Dto.Ticket;
using Domain.Customer.Enums;
using FluentValidation;

namespace Application.Validation
{
    public class TicketCreateDtoValidator : AbstractValidator<TicketCreateDto>
    {
        public TicketCreateDtoValidator()
        {
            RuleFor(x => x.CustomerId)
                .GreaterThan(0)
                .WithMessage("Customer id must be greater than zero.");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(x => x.Priority)
                .Must(priority => priority == default || Enum.IsDefined(priority))
                .WithMessage("Ticket priority is not valid.");
        }
    }
}
