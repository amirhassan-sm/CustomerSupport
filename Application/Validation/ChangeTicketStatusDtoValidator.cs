using Application.Dto.Ticket;
using FluentValidation;

namespace Application.Validation
{
    public class ChangeTicketStatusDtoValidator : AbstractValidator<ChangeTicketStatusDto>
    {
        public ChangeTicketStatusDtoValidator()
        {
            RuleFor(x => x.Status)
                .IsInEnum()
                .WithMessage("Ticket status is not valid.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Status description cannot exceed 500 characters.")
                .When(x => !string.IsNullOrWhiteSpace(x.Description));
        }
    }
}
