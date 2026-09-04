using System.Text.Json.Serialization;
using Customer.Bootstrap;
using CustomerSupport.ExceptionHandling;
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

builder.Services.WireUpCustomerSystem(customerConnectionString);
builder.Services.WireUpSecuritySystem(securityConnectionString, secretKey, issuer, audience);

var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "customersupport:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

await WaitForDatabasesAsync(app.Services);
await app.Services.SeedIdentityAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DefaultModelExpandDepth(2);
        options.DefaultModelsExpandDepth(2);
    });
}

if (!app.Configuration.GetValue("DisableHttpsRedirection", false))
    app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task WaitForDatabasesAsync(IServiceProvider services)
{
    const int maxAttempts = 20;

    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await services.MigrateCustomerDatabaseAsync();
            await services.MigrateSecurityDatabaseAsync();
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            Console.WriteLine($"Waiting for SQL Server ({attempt}/{maxAttempts}): {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}
