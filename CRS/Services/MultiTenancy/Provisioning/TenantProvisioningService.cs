using CRS.Data;
using CRS.Models;
using CRS.MultiTenancy.Database;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.MultiTenancy.Provisioning {
    public interface ITenantProvisioningQueue {
        Task EnqueueAsync(int tenantId, CancellationToken ct = default);
        Task<int?> DequeueAsync(CancellationToken ct = default); // returns tenantId or null
    }

    public class InMemoryTenantProvisioningQueue : ITenantProvisioningQueue {
        private readonly Queue<int> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);
        public Task EnqueueAsync(int tenantId, CancellationToken ct = default) {
            lock (_queue) { _queue.Enqueue(tenantId); }
            _signal.Release();
            return Task.CompletedTask;
        }
        public async Task<int?> DequeueAsync(CancellationToken ct = default) {
            await _signal.WaitAsync(ct);
            lock (_queue) { return _queue.Count > 0 ? _queue.Dequeue() : null; }
        }
    }

    public interface ITenantProvisioningService {
        Task ProvisionAsync(int tenantId, CancellationToken ct = default);
    }

    public class TenantProvisioningService : ITenantProvisioningService {
        private readonly ITenantMigrationService _migrationService;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantDatabaseResolver _resolver; // add
        private readonly ILogger<TenantProvisioningService> _logger; // add
        public TenantProvisioningService(ITenantMigrationService migrationService, IDbContextFactory<ApplicationDbContext> dbFactory, ITenantDatabaseResolver resolver, ILogger<TenantProvisioningService> logger) {
            _migrationService = migrationService; _dbFactory = dbFactory; _resolver = resolver; _logger = logger; }

        public async Task ProvisionAsync(int tenantId, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null) return;
            tenant.ProvisioningStatus = TenantProvisioningStatus.Provisioning;
            await db.SaveChangesAsync(ct);
            try {
                var conn = _resolver.GetConnectionString(tenantId);
                _logger.LogInformation("Creating Azure SQL database for tenant {TenantId} using conn template {ConnectionString}", tenantId, conn);
                // Placeholder: actual Azure SQL database creation would occur here using management SDK.
                // For now we assume DB exists and proceed with migrations.
                await _migrationService.EnsureDatabaseAsync(tenantId, ct);
                await _migrationService.MigrateAsync(tenantId, ct);
                await _migrationService.SeedAsync(tenantId, ct);
                tenant.ProvisioningStatus = TenantProvisioningStatus.Active;
                tenant.ProvisionedAt = DateTime.UtcNow;
                tenant.ProvisioningError = null;
            } catch (Exception ex) {
                tenant.ProvisioningStatus = TenantProvisioningStatus.Failed;
                tenant.ProvisioningError = ex.Message;
                _logger.LogError(ex, "Provisioning failed for tenant {TenantId}", tenantId);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
