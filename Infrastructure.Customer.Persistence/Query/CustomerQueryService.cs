using Application.Framework.SearchBaseModel;
using Application.Contracts.QueryServices;
using Application.Dto.Customer;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Customer.Persistence.Query
{
    public class CustomerQueryService : ICustomerQueryService
    {
        private readonly CustomerContext _db;

        public CustomerQueryService(CustomerContext db)
        {
            _db = db;
        }

        public async Task<GenericComplexResult<CustomerSearchModel, CustomerResultDto>> Search(CustomerSearchModel sm)
        {
            sm ??= new CustomerSearchModel();

            var query = _db.Customers
                .AsNoTracking()
                .Where(c => !c.IsDeleted);

            if (!string.IsNullOrWhiteSpace(sm.Phrase))
            {
                var phrase = sm.Phrase.Trim();

                query = query.Where(c =>
                    c.FirstName.Contains(phrase) ||
                    c.LastName.Contains(phrase) ||
                    c.Email.Contains(phrase) ||
                    c.PhoneNumber.Contains(phrase) ||
                    (c.CompanyName != null && c.CompanyName.Contains(phrase)));
            }

            sm.RecordCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .ThenBy(c => c.CustomerId)
                .Skip((sm.pageIndex - 1) * sm.pageSize)
                .Take(sm.pageSize)
                .Select(c => new CustomerResultDto
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    CompanyName = c.CompanyName,
                    PhoneNumber = c.PhoneNumber,
                    Email = c.Email,
                    Type = c.Type,
                    Status = c.Status
                })
                .ToListAsync();

            return new GenericComplexResult<CustomerSearchModel, CustomerResultDto>
            {
                SearchModel = sm,
                ListIteams = items
            };
        }
    }
}
