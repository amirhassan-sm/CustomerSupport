using Applicatio.Freamwork.SearchBaseModel;
using Application.Dto.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contrast.QueryServices
{
    public interface ICustomerQueryService
    {
        Task<GenericComplexresult<CustomerSearchModel, CustomerResultDto>> Search(CustomerSearchModel sm);
    }
}
