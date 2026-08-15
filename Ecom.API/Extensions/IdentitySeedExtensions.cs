using Ecom.Core.Entities;
using Microsoft.AspNetCore.Identity;

namespace Ecom.API.Extensions
{
    public static class IdentitySeedExtensions
    {
        private static readonly string[] Roles = { "Admin", "Customer" };

        public static async Task SeedRolesAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public static async Task SeedAdminUserAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            var adminEmail = configuration["AdminSeed:Email"];
            if (string.IsNullOrEmpty(adminEmail)) return;

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser is null) return; 

            if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}