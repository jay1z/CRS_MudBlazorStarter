using CRS.Models.Security;

using Microsoft.EntityFrameworkCore;

namespace CRS.Data;

public static class RoleSeedExtensions
{
    public static async Task EnsureScopedRolesAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Seed roles table if empty
        if (!await db.Set<Role>().AnyAsync())
        {
            var roles = new List<Role>
            {
                new() { Name = "PlatformAdmin", Scope = RoleScope.Platform },
                new() { Name = "PlatformSupport", Scope = RoleScope.Platform },
                new() { Name = "TenantOwner", Scope = RoleScope.Tenant },
                new() { Name = "TenantSpecialist", Scope = RoleScope.Tenant },
                new() { Name = "TenantViewer", Scope = RoleScope.Tenant },
                new() { Name = "HOAUser", Scope = RoleScope.External },
                new() { Name = "HOAAuditor", Scope = RoleScope.External }
            };
            db.AddRange(roles);
            await db.SaveChangesAsync();
        }

        // Leave existing ASP.NET Identity role seeding as-is for compatibility
    }
}
