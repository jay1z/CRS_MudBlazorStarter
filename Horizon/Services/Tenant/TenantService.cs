using Horizon.Data;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services.MultiTenancy {
    // SaaS Refactor: Helper service for working with tenants
    public class TenantService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public TenantService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }
        public async Task<Horizon.Models.Tenant?> FindBySubdomainAsync(string subdomain, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.Set<Horizon.Models.Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
        }

        public async Task<Horizon.Models.Tenant?> FindByIdAsync(int tenantId, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.Set<Horizon.Models.Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        }

        public async Task<Horizon.Models.Tenant> CreateTenantAsync(string name, string subdomain, CancellationToken ct = default) {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("name required");
            if (string.IsNullOrWhiteSpace(subdomain)) throw new ArgumentException("subdomain required");
            var sub = subdomain.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(sub, "^[a-z0-9-]{1,63}$"))
                throw new InvalidOperationException("Invalid subdomain format.");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var exists = await db.Set<Horizon.Models.Tenant>().AnyAsync(t => t.Subdomain == sub, ct);
            if (exists) throw new InvalidOperationException("Subdomain already in use.");

            var tenant = new Horizon.Models.Tenant { Name = name.Trim(), Subdomain = sub, IsActive = true, CreatedAt = DateTime.UtcNow };
            db.Add(tenant);
            await db.SaveChangesAsync(ct);
            return tenant;
        }
    }
}