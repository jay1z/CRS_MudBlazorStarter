using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using System.Linq;

namespace CRS.Data {
    public class SeedManager {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider) {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

            string[] roleNames = { "User", "Admin", "Specialist" };

            foreach (var roleName in roleNames) {
                if (!await roleManager.RoleExistsAsync(roleName)) {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                }
            }
        }

        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider) {
            // Use our helper to create the admin user if it doesn't exist.
            await SeedUserIfNotExists(serviceProvider, email: "admin@company.com", password: "admin", firstName: "Admin", lastName: "", roles: new List<string>() { "Admin" });
            await SeedUserIfNotExists(serviceProvider, email: "jason@admin.com", password: "Letmein1_", firstName: "Jason", lastName: "Admin", roles: new List<string>() { "Admin" });
        }

        public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider) {
            // Specialist test user
            await SeedUserIfNotExists(serviceProvider, email: "peter@specialist.com", password: "Letmein1_", firstName: "Peter", lastName: "Specialist", roles: new List<string>() { "Specialist" });

            // Specialist test user
            await SeedUserIfNotExists(serviceProvider, email: "jeff@specialist.com", password: "Letmein1_", firstName: "Jeff", lastName: "Specialist", roles: new List<string>() { "Specialist" });

            // Regular test user
            await SeedUserIfNotExists(serviceProvider, email: "jeff@user.com", password: "Letmein1_", firstName: "Jeff", lastName: "User", roles: new List<string>() { "User" });

            // Regular test user
            await SeedUserIfNotExists(serviceProvider, email: "jason@user.com", password: "Letmein1_", firstName: "Jason", lastName: "User", roles: new List<string>() { "User" });
        }

        /// <summary>
        /// Creates a new user with the specified details and role if a user with the given email does not already exist.
        /// </summary>
        private static async Task SeedUserIfNotExists(IServiceProvider serviceProvider, string email, string password, string firstName, string lastName, List<string> roles) {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<SeedManager>>();

            if (await userManager.FindByEmailAsync(email) != null) {
                logger.LogInformation($"User with email {email} already exists.");
                return;
            }

            var user = new ApplicationUser {
                FirstName = firstName,
                LastName = lastName,
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded) {
                foreach (var role in roles) {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation($"Seeded {role} user: {email}");
                }
            }
            else {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError($"Error seeding user {email}: {errors}");
            }
        }
    }
}
