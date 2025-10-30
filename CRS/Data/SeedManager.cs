using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

using System.Linq;
using System.Collections.Generic;

using CRS.Models;

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
            // Legacy admin users (no explicit tenant)
            await SeedUserIfNotExists(serviceProvider, email: "admin@company.com", password: "Letmeinnow1_", firstName: "Admin", lastName: "", roles: new List<string>() { "Admin" });
            await SeedUserIfNotExists(serviceProvider, email: "jason@admin.com", password: "Letmeinnow1_", firstName: "Jason", lastName: "Admin", roles: new List<string>() { "Admin" });
        }

        public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider) {
            // Specialist test user
            await SeedUserIfNotExists(serviceProvider, email: "peter@specialist.com", password: "Letmeinnow1_", firstName: "Peter", lastName: "Specialist", roles: new List<string>() { "Specialist" });

            // Specialist test user
            await SeedUserIfNotExists(serviceProvider, email: "jeff@specialist.com", password: "Letmeinnow1_", firstName: "Jeff", lastName: "Specialist", roles: new List<string>() { "Specialist" });

            // Regular test user
            await SeedUserIfNotExists(serviceProvider, email: "jeff@user.com", password: "Letmeinnow1_", firstName: "Jeff", lastName: "User", roles: new List<string>() { "User" });

            // Regular test user
            await SeedUserIfNotExists(serviceProvider, email: "jason@user.com", password: "Letmeinnow1_", firstName: "Jason", lastName: "User", roles: new List<string>() { "User" });
        }

        /// <summary>
        /// Seed three example tenants and create an admin user for each tenant.
        /// </summary>
        public static async Task SeedTenantsAndAdminsAsync(IServiceProvider serviceProvider) {
            // Create a scope to resolve DbContext
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            ApplicationDbContext? db = null;
            try {
                var factory = sp.GetService<IDbContextFactory<ApplicationDbContext>>();
                if (factory != null) db = await factory.CreateDbContextAsync();
                else db = sp.GetRequiredService<ApplicationDbContext>();
            } catch {
                db = sp.GetRequiredService<ApplicationDbContext>();
            }
            await using (db.ConfigureAwait(false)) {
                var tenantsToEnsure = new[] {
                    new Tenant { Name = "Tenant Alpha", Subdomain = "alpha", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Tenant { Name = "Tenant Beta", Subdomain = "beta", IsActive = true, CreatedAt = DateTime.UtcNow },
                    new Tenant { Name = "Tenant Gamma", Subdomain = "gamma", IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                var createdTenants = new List<Tenant>();
                foreach (var t in tenantsToEnsure) {
                    var existing = db.Set<Tenant>().FirstOrDefault(x => x.Subdomain == t.Subdomain);
                    if (existing == null) {
                        db.Set<Tenant>().Add(t);
                        await db.SaveChangesAsync();
                        createdTenants.Add(t);
                    } else {
                        createdTenants.Add(existing);
                    }
                }

                // Ensure roles exist
                await SeedRolesAsync(serviceProvider);

                // Create an admin user for each tenant using the same password as existing seeds
                var password = "Letmeinnow1_";

                // Define admin accounts for tenants (you can change names/emails as desired)
                var admins = new[] {
                    new { Email = "admin@alpha.com", First = "Alice", Last = "Alpha", Tenant = createdTenants[0] },
                    new { Email = "admin@beta.com", First = "Ben", Last = "Beta", Tenant = createdTenants[1] },
                    new { Email = "admin@gamma.com", First = "Gina", Last = "Gamma", Tenant = createdTenants[2] }
                };

                foreach (var a in admins) {
                    await SeedUserIfNotExists(serviceProvider, email: a.Email, password: password, firstName: a.First, lastName: a.Last, roles: new List<string> { "Admin" }, tenantId: a.Tenant.Id);
                }
            }
        }

        /// <summary>
        /// Creates a new user with the specified details and role if a user with the given email does not already exist.
        /// Accepts an optional tenantId to assign the user to a tenant.
        /// </summary>
        private static async Task SeedUserIfNotExists(IServiceProvider serviceProvider, string email, string password, string firstName, string lastName, List<string> roles, int? tenantId = null) {
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
                EmailConfirmed = true,
                TenantId = tenantId ??1
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
