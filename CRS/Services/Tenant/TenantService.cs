using CRS.Data;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services.MultiTenancy {
    // SaaS Refactor: Helper service for working with tenants
    public class TenantService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public TenantService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }
        public async Task<CRS.Models.Tenant?> FindBySubdomainAsync(string subdomain, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.Set<CRS.Models.Tenant>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
        }
    }
}