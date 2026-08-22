using Application.Contrast.Authorization;
using Infrastructure.Security.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Security.Identity.Seed
{
    public static class IdentityDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await EnsureRoleAsync(roleManager, AppRoles.Admin, "Full access to users, roles, and tickets.");
            await EnsureRoleAsync(roleManager, AppRoles.Agent, "Works the support ticket queue.");
            await EnsureRoleAsync(roleManager, AppRoles.Customer, "Opens and follows up on support tickets.");

            await EnsureAdminAsync(userManager, config);
        }

        private static async Task EnsureRoleAsync(
            RoleManager<ApplicationRole> roleManager,
            string roleName,
            string description)
        {
            if (await roleManager.RoleExistsAsync(roleName))
                return;

            await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName,
                Description = description
            });
        }

        private static async Task EnsureAdminAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration config)
        {
            var userName = config["Seed:Admin:UserName"] ?? "admin";
            var email = config["Seed:Admin:Email"] ?? "admin@customersupport.local";
            var password = config["Seed:Admin:Password"] ?? "Admin1234";
            var firstName = config["Seed:Admin:FirstName"] ?? "System";
            var lastName = config["Seed:Admin:LastName"] ?? "Admin";

            var admin = await userManager.FindByNameAsync(userName)
                ?? await userManager.FindByEmailAsync(email);

            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = userName,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    CreatedAt = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(admin, password);
                if (!createResult.Succeeded)
                    return;
            }

            if (!await userManager.IsInRoleAsync(admin, AppRoles.Admin))
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
    }
}
