using CRS.Data;
using CRS.Models;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Customers {
    public class CustomerService : ICustomerService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenant;
        private readonly ILogger<CustomerService> _logger;

        public CustomerService(
            IDbContextFactory<ApplicationDbContext> dbFactory, 
            ITenantContext tenant,
            ILogger<CustomerService> logger) 
        { 
            _dbFactory = dbFactory; 
            _tenant = tenant;
            _logger = logger;
        }

        public async Task<int> GetActiveCustomerCountAsync(CancellationToken ct = default) {
            if (!_tenant.TenantId.HasValue) return 0;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.CustomerAccounts.CountAsync(c => c.TenantId == _tenant.TenantId && c.IsActive, ct);
        }

        public async Task<List<CustomerAccount>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var query = db.CustomerAccounts
                .Where(c => c.TenantId == _tenant.TenantId);

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<CustomerAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return null;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CustomerAccounts
                .Include(c => c.Communities)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenant.TenantId, ct);
        }

        public async Task<CustomerAccount> CreateAsync(CustomerAccount account, CancellationToken ct = default) 
        {
            if (!_tenant.TenantId.HasValue) 
                throw new InvalidOperationException("Tenant context required");

            account.TenantId = _tenant.TenantId.Value;
            account.CreatedAt = DateTime.UtcNow;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.CustomerAccounts.Add(account);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Created customer {CustomerId} ({Name}) for tenant {TenantId}", 
                account.Id, account.Name, account.TenantId);

            return account;
        }

        public async Task<CustomerAccount?> UpdateAsync(CustomerAccount account, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return null;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var existing = await db.CustomerAccounts
                .FirstOrDefaultAsync(c => c.Id == account.Id && c.TenantId == _tenant.TenantId, ct);

            if (existing == null) return null;

            existing.Name = account.Name;
            existing.Type = account.Type;
            existing.ContactName = account.ContactName;
            existing.Email = account.Email;
            existing.Phone = account.Phone;
            existing.AddressLine1 = account.AddressLine1;
            existing.AddressLine2 = account.AddressLine2;
            existing.City = account.City;
            existing.State = account.State;
            existing.PostalCode = account.PostalCode;
            existing.Notes = account.Notes;
            existing.Website = account.Website;
            existing.TaxId = account.TaxId;
            existing.PaymentTerms = account.PaymentTerms;
            existing.IsActive = account.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Updated customer {CustomerId} ({Name})", existing.Id, existing.Name);

            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var customer = await db.CustomerAccounts
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenant.TenantId, ct);

            if (customer == null) return false;

            // Soft delete
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Soft-deleted customer {CustomerId} ({Name})", customer.Id, customer.Name);

            return true;
        }

        public async Task<List<CustomerWithStats>> GetCustomersWithStatsAsync(CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var customers = await db.CustomerAccounts
                .Where(c => c.TenantId == _tenant.TenantId && c.IsActive)
                .Include(c => c.Communities)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            var result = new List<CustomerWithStats>();

            foreach (var customer in customers)
            {
                var communityIds = customer.Communities.Select(c => c.Id).ToList();

                var activeStudies = communityIds.Count > 0 
                    ? await db.ReserveStudies
                        .CountAsync(rs => rs.CommunityId.HasValue && communityIds.Contains(rs.CommunityId.Value) && rs.IsActive && !rs.IsComplete, ct)
                    : 0;

                // Calculate total revenue from paid invoices
                var revenue = communityIds.Count > 0
                    ? await db.Invoices
                        .Where(i => i.ReserveStudy != null && 
                                   i.ReserveStudy.CommunityId.HasValue && communityIds.Contains(i.ReserveStudy.CommunityId.Value) &&
                                   i.Status == InvoiceStatus.Paid)
                        .SumAsync(i => i.TotalAmount, ct)
                    : 0m;

                result.Add(new CustomerWithStats(
                    Customer: customer,
                    CommunityCount: customer.Communities.Count,
                    ActiveStudyCount: activeStudies,
                    TotalRevenue: revenue
                ));
            }

            return result;
        }

        public async Task<List<CustomerAccount>> SearchAsync(string query, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue || string.IsNullOrWhiteSpace(query)) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var searchTerm = query.ToLower();

            return await db.CustomerAccounts
                .Where(c => c.TenantId == _tenant.TenantId && c.IsActive)
                .Where(c => c.Name.ToLower().Contains(searchTerm) ||
                           (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                           (c.ContactName != null && c.ContactName.ToLower().Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .Take(20)
                .ToListAsync(ct);
        }
    }
}