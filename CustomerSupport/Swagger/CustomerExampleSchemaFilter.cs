using Application.Dto.Customer;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CustomerSupport.Swagger
{
    public sealed class CustomerExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(CustomerCreateDto))
            {
                schema.Example = new OpenApiObject
                {
                    ["firstName"] = new OpenApiString("John"),
                    ["lastName"] = new OpenApiString("Doe"),
                    ["companyName"] = new OpenApiNull(),
                    ["phoneNumber"] = new OpenApiString("09120000000"),
                    ["email"] = new OpenApiString("john@example.com"),
                    ["type"] = new OpenApiString("Individual")
                };
            }
            else if (context.Type == typeof(CustomerUpdateDto))
            {
                schema.Example = new OpenApiObject
                {
                    ["firstName"] = new OpenApiString("John"),
                    ["lastName"] = new OpenApiString("Doe"),
                    ["companyName"] = new OpenApiNull(),
                    ["phoneNumber"] = new OpenApiString("09120000000"),
                    ["email"] = new OpenApiString("john@example.com"),
                    ["type"] = new OpenApiString("Individual"),
                    ["status"] = new OpenApiString("Active")
                };
            }
            else if (context.Type == typeof(ChangeCustomerStatusDto))
            {
                schema.Example = new OpenApiObject
                {
                    ["status"] = new OpenApiString("Inactive")
                };
            }
        }
    }
}
