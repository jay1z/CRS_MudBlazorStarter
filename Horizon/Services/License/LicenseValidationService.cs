using Horizon.Data;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services.License {
    // SaaS Refactor: periodically validate tenant status
    public interface ILicenseValidationService {
        Task<bool> IsTenantActiveAsync(int tenantId, CancellationToken ct = default);
    }

    public class LicenseValidationService : ILicenseValidationService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public LicenseValidationService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }
        public async Task<bool> IsTenantActiveAsync(int tenantId, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            return tenant?.IsActive ?? false;
        }
    }

    // Optional: background service skeleton (can be enabled later)
    public class LicenseValidationBackgroundService : BackgroundService {
        private readonly ILogger<LicenseValidationBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        public LicenseValidationBackgroundService(ILogger<LicenseValidationBackgroundService> logger, IServiceProvider sp) {
            _logger = logger; _serviceProvider = sp;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                } catch (TaskCanceledException) { }
            }
        }
    }
}
