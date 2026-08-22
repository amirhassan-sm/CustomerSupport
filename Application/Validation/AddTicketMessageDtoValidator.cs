using Application.Dto.Ticket;
using FluentValidation;

namespace Application.Validation
{
    public class AddTicketMessageDtoValidator : AbstractValidator<AddTicketMessageDto>
    {
        public AddTicketMessageDtoValidator()
        {
            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required.");
        }
    }
}
