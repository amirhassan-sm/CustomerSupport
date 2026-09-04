using Application.Contracts.QueryServices;
using Application.Contracts.Services;
using Application.Implementation;
using Application.Validation;
using Customer.DomainServiceContract.Services;
using FluentValidation;
using Infrastructure.Customer.Persistence;
using Infrastructure.Customer.Persistence.Query;
using Infrastructure.Customer.Persistence.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Customer.Bootstrap
{
    public static class Bootstrap
    {
        public static void WireUpCustomerSystem(this IServiceCollection services, string CustomerConnectionString)
        {
            services.AddDbContext<CustomerContext>(optionsAction => optionsAction.UseSqlServer(CustomerConnectionString));
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<ITicketRepository, TicketRepository>();
            services.AddScoped<ICustomerQueryService, CustomerQueryService>();
            services.AddScoped<ITicketQueryServices, TicketQueryService>();
            services.AddValidatorsFromAssemblyContaining<CustomerCreateDtoValidator>();
            services.AddScoped<ICustomerServices, CustomerServices>();
            services.AddScoped<ITicketServices, TicketServices>();
        }

        public static async Task MigrateCustomerDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CustomerContext>();
            await db.Database.MigrateAsync();
        }
    }
}
