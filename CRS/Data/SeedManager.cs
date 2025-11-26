using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using CRS.Models;
using CRS.Models.Security;

namespace CRS.Data {
    public class SeedManager {
        // Core scoped roles seeding (custom SaaS roles, not Identity roles)
        public static async Task SeedScopedRolesAsync(IServiceProvider serviceProvider) {
            using var scope = serviceProvider.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<ApplicationDbContext>();
            var logger = sp.GetRequiredService<ILogger<SeedManager>>();

            // Use migrations only; do NOT call EnsureCreated (conflicts with Migrate)
            // Always assume schema exists or will be migrated earlier in pipeline.

            var required = new (string Name, RoleScope Scope)[] {
                ("PlatformAdmin", RoleScope.Platform),
                ("PlatformSupport", RoleScope.Platform),
                ("TenantOwner", RoleScope.Tenant),
                ("TenantSpecialist", RoleScope.Tenant),
                ("TenantViewer", RoleScope.Tenant),
                ("HOAUser", RoleScope.External),
                ("HOAAuditor", RoleScope.External)
            };

            var roleSet = db.Set<Role>();
            var existing = await roleSet.AsNoTracking().ToDictionaryAsync(r => r.Name, r => r.Id);
            int added = 0;
            foreach (var r in required) {
                if (!existing.ContainsKey(r.Name)) {
                    roleSet.Add(new Role { Name = r.Name, Scope = r.Scope });
                    added++;
                }
            }
            if (added > 0) {
                await db.SaveChangesAsync();
                logger.LogInformation("SeedScopedRolesAsync: Added {Count} missing roles.", added);
            } else {
                logger.LogInformation("SeedScopedRolesAsync: All required roles already present.");
            }
        }

        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider) {
            await SeedScopedRolesAsync(serviceProvider);
            var platformTenant = await EnsureTenantAsync(serviceProvider, subdomain: "platform", name: "Platform", isActive: true);
            var user = await SeedUserIfNotExists(serviceProvider,
                email: "admin@platform.com",
                password: "Letmeinnow1_",
                firstName: "Platform",
                lastName: "Admin",
                tenantId: platformTenant.Id);
            await AssignScopedRoleAsync(serviceProvider, user, "PlatformAdmin", tenantId: 0);
        }

        public static async Task SeedTestUsersAsync(IServiceProvider serviceProvider) {
            await SeedScopedRolesAsync(serviceProvider);
            var peter = await SeedUserIfNotExists(serviceProvider, email: "peter@specialist.com", password: "Letmeinnow1_", firstName: "Peter", lastName: "Specialist");
            var jeffS = await SeedUserIfNotExists(serviceProvider, email: "jeff@specialist.com", password: "Letmeinnow1_", firstName: "Jeff", lastName: "Specialist");
            var jeffU = await SeedUserIfNotExists(serviceProvider, email: "jeff@user.com", password: "Letmeinnow1_", firstName: "Jeff", lastName: "User");
            var jason = await SeedUserIfNotExists(serviceProvider, email: "jason@user.com", password: "Letmeinnow1_", firstName: "Jason", lastName: "User");
            if (peter != null) await AssignScopedRoleAsync(serviceProvider, peter, "TenantSpecialist", peter.TenantId);
            if (jeffS != null) await AssignScopedRoleAsync(serviceProvider, jeffS, "TenantSpecialist", jeffS.TenantId);
            if (jeffU != null) await AssignScopedRoleAsync(serviceProvider, jeffU, "TenantViewer", jeffU.TenantId);
            if (jason != null) await AssignScopedRoleAsync(serviceProvider, jason, "TenantViewer", jason.TenantId);
        }

        public static async Task SeedTenantsAndAdminsAsync(IServiceProvider serviceProvider) {
            await SeedScopedRolesAsync(serviceProvider);
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
                await EnsureTenantAsync(serviceProvider, subdomain: "platform", name: "Platform", isActive: true);
            }
        }

        private static async Task<Tenant> EnsureTenantAsync(IServiceProvider services, string subdomain, string name, bool isActive) {
            var factory = services.GetService<IDbContextFactory<ApplicationDbContext>>();
            var db = factory != null ? await factory.CreateDbContextAsync() : services.GetRequiredService<ApplicationDbContext>();
            await using (db.ConfigureAwait(false)) {
                var existing = await db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain);
                if (existing != null) {
                    bool changed = false;
                    if (string.IsNullOrWhiteSpace(existing.Name)) { existing.Name = name; changed = true; }
                    if (existing.IsActive != isActive) { existing.IsActive = isActive; changed = true; }
                    if (changed) await db.SaveChangesAsync();
                    return existing;
                }
                var t = new Tenant { Name = name, Subdomain = subdomain, IsActive = isActive, CreatedAt = DateTime.UtcNow };
                db.Add(t);
                await db.SaveChangesAsync();
                return t;
            }
        }

        private static async Task<ApplicationUser> SeedUserIfNotExists(IServiceProvider serviceProvider, string email, string password, string firstName, string lastName, int? tenantId = null) {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<SeedManager>>();
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) { logger.LogInformation("User {Email} already exists.", email); return existing; }

            var assignedTenantId = tenantId;
            if (!assignedTenantId.HasValue) {
                var factory = serviceProvider.GetService<IDbContextFactory<ApplicationDbContext>>();
                var db = factory != null ? await factory.CreateDbContextAsync() : serviceProvider.GetRequiredService<ApplicationDbContext>();
                await using (db.ConfigureAwait(false)) {
                    var platformTenant = await db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == "platform") ?? await db.Tenants.FirstOrDefaultAsync();
                    assignedTenantId = platformTenant?.Id ?? 1;
                }
            }

            var user = new ApplicationUser {
                FirstName = firstName,
                LastName = lastName,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                TenantId = assignedTenantId!.Value
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded) {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                logger.LogError("Error seeding user {Email}: {Errors}", email, errors);
            }

            try {
                var factory = serviceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                await using var db = await factory.CreateDbContextAsync();
                var tenantUserCount = await db.Users.CountAsync(u => u.TenantId == user.TenantId && u.Id != user.Id);
                if (tenantUserCount == 0) {
                    await AssignScopedRoleAsync(serviceProvider, user, user.TenantId == 0 ? "PlatformAdmin" : "TenantOwner", user.TenantId == 0 ? 0 : user.TenantId);
                }
            } catch { }

            return user;
        }

        private static async Task AssignScopedRoleAsync(IServiceProvider sp, ApplicationUser user, string roleName, int tenantId) {
            // Use DbContextFactory to get a fresh context for assignments
            var factory = sp.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
            await using var db = await factory.CreateDbContextAsync();

            // Ensure the tenant exists when tenantId != 0
            if (tenantId != 0) {
                var tenantExists = await db.Tenants.AnyAsync(t => t.Id == tenantId);
                if (!tenantExists) {
                    var logger = sp.GetRequiredService<ILogger<SeedManager>>();
                    logger.LogWarning("AssignScopedRoleAsync: Tenant {TenantId} not found. Skipping assignment of role {RoleName} for user {UserId}.", tenantId, roleName, user.Id);
                    return;
                }
            }

            // Look up role in custom Roles2 table
            var role = await db.Roles2.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role == null) {
                // As a fallback, create the custom role if missing
                role = new Role { Name = roleName, Scope = tenantId == 0 ? RoleScope.Platform : RoleScope.Tenant };
                db.Roles2.Add(role);
                await db.SaveChangesAsync();
            }

            // Ensure user is attached to this DbContext so FK uses same identity
            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            if (existingUser == null) {
                // Attempt to attach a minimal user stub to satisfy FK constraints
                db.Users.Attach(new ApplicationUser { Id = user.Id, UserName = user.UserName, Email = user.Email, TenantId = user.TenantId });
            }

            bool exists = await db.UserRoleAssignments.AnyAsync(a => a.UserId == user.Id && a.RoleId == role.Id && a.TenantId == (tenantId == 0 ? null : tenantId));
            if (!exists) {
                db.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = role.Id, TenantId = tenantId == 0 ? null : tenantId });
                await db.SaveChangesAsync();
            }
        }
    }
}
