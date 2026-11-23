using CurioBid.Models;
using Microsoft.AspNetCore.Identity;

namespace CurioBid.Data
{
    public class DBSeeder
    {
        public static readonly string[] Roles = { "User", "Reviewer", "Admin" };
        private static readonly string[] LegacyRoles = { "Buyer", "Seller" };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var config = services.GetRequiredService<IConfiguration>();
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            foreach (var legacy in LegacyRoles)
            {
                if (!await roleManager.RoleExistsAsync(legacy)) continue;

                var users = await userManager.GetUsersInRoleAsync(legacy);
                foreach (var u in users)
                {
                    if (!await userManager.IsInRoleAsync(u, "User"))
                    {
                        await userManager.AddToRoleAsync(u, "User");
                    }
                    await userManager.RemoveFromRoleAsync(u, legacy);
                }

                var oldRole = await roleManager.FindByNameAsync(legacy);
                if (oldRole != null) await roleManager.DeleteAsync(oldRole);
            }
            await CreateUserAsync(userManager, config, "SeedUsers:Admin", "Admin");
            await CreateUserAsync(userManager, config, "SeedUsers:Reviewer", "Reviewer");

        }
    private static async Task CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            IConfiguration config,
            string section,
            string role)
        {
            var email = config[$"{section}:Email"];
            var password = config[$"{section}:Password"];
            var fullName = config[$"{section}:FullName"] ?? role;

            // No config means no seeded user (the roles above still exist)
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;

            if (await userManager.FindByEmailAsync(email) != null) return;

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, role);
            }
        }
    }
}
