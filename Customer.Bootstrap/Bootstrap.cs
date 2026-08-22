using Application.Contrast.QueryServices;
using Application.Contrast.Services;
using Application.Implementation;
using Application.Validation;
using Customer.DomainServiceContract.Services;
using FluentValidation;
using Infrastructure.Customer.Persistance;
using Infrastructure.Customer.Persistance.Query;
using Infrastructure.Customer.Persistance.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Customer.Bootstrap
{
    public static class Bootstrap
    {
        public static void WierUpCustomerSystem(this IServiceCollection services, string CustomerConnectionString)
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
    }
}
