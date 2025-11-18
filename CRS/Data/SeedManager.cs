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
        /// Production-safe seeding: ensure a single default tenant exists. Do not create sample tenants or demo admins here.
        /// </summary>
        public static async Task SeedTenantsAndAdminsAsync(IServiceProvider serviceProvider) {
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;

            ApplicationDbContext db;
            try {
                var factory = sp.GetService<IDbContextFactory<ApplicationDbContext>>();
                db = factory != null ? await factory.CreateDbContextAsync() : sp.GetRequiredService<ApplicationDbContext>();
            } catch {
                db = sp.GetRequiredService<ApplicationDbContext>();
            }

            await using (db.ConfigureAwait(false)) {
                // Ensure roles exist
                await SeedRolesAsync(serviceProvider);

                // Ensure a default tenant (Id=1) exists with predictable subdomain
                var defaultSubdomain = "default";
                var defaultName = "Default Tenant";

                var existingDefault = await db.Set<Tenant>()
                    .AsTracking()
                    .FirstOrDefaultAsync(t => t.Id == 1 || t.Subdomain == defaultSubdomain);

                if (existingDefault == null) {
                    var t = new Tenant { Name = defaultName, Subdomain = defaultSubdomain, IsActive = true, CreatedAt = DateTime.UtcNow };
                    db.Add(t);
                    await db.SaveChangesAsync();
                } else {
                    // Normalize name/subdomain if needed
                    if (string.IsNullOrWhiteSpace(existingDefault.Subdomain)) existingDefault.Subdomain = defaultSubdomain;
                    if (string.IsNullOrWhiteSpace(existingDefault.Name)) existingDefault.Name = defaultName;
                    await db.SaveChangesAsync();
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
                TenantId = tenantId ?? 1
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
