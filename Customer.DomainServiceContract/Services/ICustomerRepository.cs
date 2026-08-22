using Customer.DomainServiceContract.BaseInterface;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Customer.DomainServiceContract.Services
{
    public interface ICustomerRepository : ICrudBaseInterface<CustomerEntity, int>
    {
        Task<CustomerEntity?> GetByEmailAsync(string email);

        Task<IReadOnlyList<CustomerEntity>> GetAllAsync();

        Task<bool> ExistsByEmailAsync(string email, int? excludeCustomerId = null);

        Task SaveChangesAsync();
    }
}
