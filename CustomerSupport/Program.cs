using System.Text.Json.Serialization;
using Customer.Bootstrap;
using CustomerSupport.Swagger;
using Microsoft.OpenApi.Models;
using Security.Bootstrap;

var builder = WebApplication.CreateBuilder(args);

var customerConnectionString = builder.Configuration.GetConnectionString("CustomerSupport")
    ?? throw new InvalidOperationException("Connection string 'CustomerSupport' is missing.");
var securityConnectionString = builder.Configuration.GetConnectionString("Security")
    ?? throw new InvalidOperationException("Connection string 'Security' is missing.");
var secretKey = builder.Configuration["jwt:SecretKey"]
    ?? throw new InvalidOperationException("Configuration value 'jwt:SecretKey' is missing.");
var issuer = builder.Configuration["jwt:Issuer"]
    ?? throw new InvalidOperationException("Configuration value 'jwt:Issuer' is missing.");
var audience = builder.Configuration["jwt:Audience"]
    ?? throw new InvalidOperationException("Configuration value 'jwt:Audience' is missing.");

builder.Services.WierUpCustomerSystem(customerConnectionString);
builder.Services.WireUpSecuritySystem(securityConnectionString, secretKey, issuer, audience);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: true));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.UseInlineDefinitionsForEnums();
    options.SchemaFilter<EnumSchemaFilter>();
    options.SchemaFilter<CustomerExampleSchemaFilter>();
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

await app.Services.SeedIdentityAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DefaultModelExpandDepth(2);
        options.DefaultModelsExpandDepth(2);
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
