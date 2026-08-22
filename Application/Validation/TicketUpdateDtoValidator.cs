using Application.Dto.Ticket;
using Domain.Customer.Enums;
using FluentValidation;

namespace Application.Validation
{
    public class TicketUpdateDtoValidator : AbstractValidator<TicketUpdateDto>
    {
        public TicketUpdateDtoValidator()
        {
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
