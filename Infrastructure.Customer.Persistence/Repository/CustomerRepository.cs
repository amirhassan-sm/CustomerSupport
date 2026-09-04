using Customer.DomainServiceContract.Services;
using Microsoft.EntityFrameworkCore;
using CustomerEntity = Domain.Customer.Entities.Customer;

namespace Infrastructure.Customer.Persistence.Repository
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly CustomerContext _db;

        public CustomerRepository(CustomerContext db)
        {
            _db = db;
        }

        private IQueryable<CustomerEntity> ActiveCustomers =>
            _db.Customers.Where(c => !c.IsDeleted);

        public async Task<CustomerEntity?> GetByIdAsync(int id)
        {
            return await ActiveCustomers.FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<CustomerEntity?> GetByEmailAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await ActiveCustomers.FirstOrDefaultAsync(c => c.Email == normalizedEmail);
        }

        public async Task<IReadOnlyList<CustomerEntity>> GetAllAsync()
        {
            return await ActiveCustomers
                .OrderByDescending(c => c.CreatedAt)
                .ThenBy(c => c.CustomerId)
                .ToListAsync();
        }

        public async Task<bool> ExistsByEmailAsync(string email, int? excludeCustomerId = null)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var query = ActiveCustomers.Where(c => c.Email == normalizedEmail);

            if (excludeCustomerId.HasValue)
                query = query.Where(c => c.CustomerId != excludeCustomerId.Value);

            return await query.AnyAsync();
        }

        public async Task AddAsync(CustomerEntity model)
        {
            await _db.Customers.AddAsync(model);
        }

        public void Update(CustomerEntity model)
        {
            _db.Customers.Update(model);
        }

        public void Delete(CustomerEntity model)
        {
            model.IsDeleted = true;
            model.UpdatedAt = DateTime.UtcNow;
            _db.Customers.Update(model);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
