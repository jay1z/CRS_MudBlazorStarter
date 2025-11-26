using CRS.Data;
using CRS.Models;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Customers {
    public class CustomerService : ICustomerService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenant;
        public CustomerService(IDbContextFactory<ApplicationDbContext> dbFactory, ITenantContext tenant) { _dbFactory = dbFactory; _tenant = tenant; }

        public async Task<int> GetActiveCustomerCountAsync(CancellationToken ct = default) {
            if (!_tenant.TenantId.HasValue) return 0;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.CustomerAccounts.CountAsync(c => c.TenantId == _tenant.TenantId && c.IsActive, ct);
        }

        public async Task<CustomerAccount> CreateCustomerAsync(CustomerAccount account, CancellationToken ct = default) {
            if (!_tenant.TenantId.HasValue) throw new InvalidOperationException("Tenant context required");
            account.TenantId = _tenant.TenantId.Value;
            account.CreatedAt = DateTime.UtcNow;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.CustomerAccounts.Add(account);
            await db.SaveChangesAsync(ct);
            return account;
        }
    }
}