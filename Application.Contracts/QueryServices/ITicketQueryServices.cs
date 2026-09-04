using Application.Framework.SearchBaseModel;
using Application.Dto.Ticket;

namespace Application.Contracts.QueryServices
{
    public interface ITicketQueryServices
    {
        Task<GenericComplexResult<TicketSearchModel, TicketResultDto>> Search(TicketSearchModel sm);
    }
}
