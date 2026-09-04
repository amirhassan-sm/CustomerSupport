using Application.Framework.SearchBaseModel;
using Application.Dto.Customer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.QueryServices
{
    public interface ICustomerQueryService
    {
        Task<GenericComplexResult<CustomerSearchModel, CustomerResultDto>> Search(CustomerSearchModel sm);
    }
}
