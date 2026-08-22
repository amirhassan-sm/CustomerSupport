using Application.Dto.Ticket;
using FluentValidation;

namespace Application.Validation
{
    public class AssignTicketDtoValidator : AbstractValidator<AssignTicketDto>
    {
        public AssignTicketDtoValidator()
        {
            RuleFor(x => x.AssignedAgentId)
                .NotEmpty().WithMessage("Assigned agent id is required.")
                .MaximumLength(450).WithMessage("Assigned agent id cannot exceed 450 characters.");
        }
    }
}
