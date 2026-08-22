using Application.Contrast.Authorization;
using Application.Contrast.Services;
using Infrastructure.Security.Identity;
using Infrastructure.Security.Identity.Models;
using Infrastructure.Security.Identity.Seed;
using Infrastructure.Security.Identity.Services;
using Infrastructure.Security.Identity.Token;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace Security.Bootstrap
{
    public static class Bootstrap
    {
        public static void WireUpSecuritySystem(
            this IServiceCollection services,
            string securityConnectionString,
            string secretKey,
            string issuer,
            string audience)
        {
            services.AddDbContext<SecurityContext>(options =>
                options.UseSqlServer(securityConnectionString));

            services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<SecurityContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    ClockSkew = TimeSpan.FromMinutes(2),
                    NameClaimType = ClaimTypes.NameIdentifier,
                    RoleClaimType = ClaimTypes.Role
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[JWT] Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"[JWT] Challenge: {context.Error} - {context.ErrorDescription}");
                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(AppPolicies.AdminOnly, policy =>
                    policy.RequireRole(AppRoles.Admin));

                options.AddPolicy(AppPolicies.Staff, policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.Agent));

                options.AddPolicy(AppPolicies.TicketAccess, policy =>
                    policy.RequireRole(AppRoles.Admin, AppRoles.Agent, AppRoles.Customer));
            });
            services.AddScoped<IGenerateToken, GenerateToken>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IRoleService, RoleService>();
            services.AddScoped<IUserRoleServices, UserRoleServices>();
            services.AddScoped<IUserSevices, UserServices>();
        }

        public static async Task SeedIdentityAsync(this IServiceProvider services)
        {
            await IdentityDataSeeder.SeedAsync(services);
        }
    }
}
