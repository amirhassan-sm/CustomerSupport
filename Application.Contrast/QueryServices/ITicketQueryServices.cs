using Applicatio.Freamwork.SearchBaseModel;
using Application.Dto.Ticket;

namespace Application.Contrast.QueryServices
{
    public interface ITicketQueryServices
    {
        Task<GenericComplexresult<TicketSearchModel, TicketResultDto>> Search(TicketSearchModel sm);
    }
}
