using Application.Framework.OperationResult;
using Application.Framework.SearchBaseModel;
using Application.Dto.Ticket;
using Domain.Customer.Enums;

namespace Application.Contracts.Services
{
    public interface ITicketServices
    {
        Task<GenericOperationResult<TicketResultDto>> GetByIdAsync(int id);

        Task<GenericOperationResult<TicketDetailsDto>> GetByIdWithDetailsAsync(int ticketId);

        Task<GenericOperationResult<IReadOnlyList<TicketResultDto>>> GetByCustomerIdAsync(int customerId);

        Task<GenericOperationResult<IReadOnlyList<TicketResultDto>>> GetByAssignedAgentIdAsync(string assignedAgentId);

        Task<GenericOperationResult<IReadOnlyList<TicketResultDto>>> GetByStatusAsync(TicketStatus status);

        Task<GenericOperationResult<TicketResultDto>> CreateAsync(TicketCreateDto model);

        Task<GenericOperationResult<TicketResultDto>> UpdateAsync(int id, TicketUpdateDto model);

        Task<GenericOperationResult<TicketResultDto>> AssignAsync(
            int id,
            AssignTicketDto model,
            string changedById);

        Task<GenericOperationResult<TicketResultDto>> ChangeStatusAsync(
            int id,
            ChangeTicketStatusDto model,
            string changedById);

        Task<GenericOperationResult<TicketMessageDto>> AddMessageAsync(
            int ticketId,
            AddTicketMessageDto model,
            string senderId,
            MessageSenderType senderType);

        Task<GenericOperationResult<bool>> DeleteAsync(int id);

        Task<GenericComplexResult<TicketSearchModel, TicketResultDto>> Search(TicketSearchModel sm);
    }
}
