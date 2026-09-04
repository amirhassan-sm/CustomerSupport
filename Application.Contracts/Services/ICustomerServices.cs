using Application.Framework.OperationResult;
using Application.Framework.SearchBaseModel;
using Application.Dto.Customer;
using Domain.Customer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Contracts.Services
{
    public interface ICustomerServices
    {
        Task<GenericOperationResult<CustomerResultDto>> GetByIdAsync(int id);

        Task<GenericOperationResult<IReadOnlyList<CustomerResultDto>>> GetAllAsync();

        Task<GenericOperationResult<CustomerResultDto>> GetByEmailAsync(string email);

        Task<GenericOperationResult<CustomerResultDto>> CreateAsync(CustomerCreateDto model);

        Task<GenericOperationResult<int>> ResolveOrCreateAccountCustomerAsync(
            CustomerAccountLinkDto model,
            bool createIfMissing = true);

        Task<GenericOperationResult<CustomerResultDto>> UpdateAsync(
            int id,
            CustomerUpdateDto model);

        Task<GenericOperationResult<bool>> ChangeStatusAsync(int id, CustomerStatus status);

        Task<GenericOperationResult<bool>> DeleteAsync(int id);
        Task<GenericComplexResult<CustomerSearchModel, CustomerResultDto>> Search(CustomerSearchModel sm);


    }
}
