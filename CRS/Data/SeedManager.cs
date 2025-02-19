using Microsoft.AspNetCore.Identity;

namespace CRS.Data {
    public class SeedManager {
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider) {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = { "User", "Admin", "Specialist" };

            foreach (var roleName in roleNames) {
                var roleExists = await roleManager.RoleExistsAsync(roleName);
                if (!roleExists) {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider) {
            //TODO: Change default admin user
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminEmail = "emailme@jasonzurowski.com";
            var adminPassword = "Letmein1_";

            if (await userManager.FindByEmailAsync(adminEmail) == null) {
                var adminUser = new ApplicationUser {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded) {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                // Handle errors if necessary
            }
        }
        public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider) {
            //TODO: Change default admin user
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var adminEmail = "jason.zurowski@gmail.com";
            var adminPassword = "Letmein1_";

            if (await userManager.FindByEmailAsync(adminEmail) == null) {
                var adminUser = new ApplicationUser {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded) {
                    await userManager.AddToRoleAsync(adminUser, "Specialist");
                }
                // Handle errors if necessary
            }

            adminEmail = "jason@email.com";
            adminPassword = "Letmein1_";

            if (await userManager.FindByEmailAsync(adminEmail) == null) {
                var adminUser = new ApplicationUser {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded) {
                    await userManager.AddToRoleAsync(adminUser, "User");
                }
                // Handle errors if necessary
            }
        }
    }
}
