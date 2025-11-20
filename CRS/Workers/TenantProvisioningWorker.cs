using CRS.Services.MultiTenancy.Provisioning;

namespace CRS.Workers {
    public class TenantProvisioningWorker : BackgroundService {
        private readonly ITenantProvisioningQueue _queue;
        private readonly ITenantProvisioningService _service;
        private readonly ILogger<TenantProvisioningWorker> _logger;
        public TenantProvisioningWorker(ITenantProvisioningQueue queue, ITenantProvisioningService service, ILogger<TenantProvisioningWorker> logger) { _queue = queue; _service = service; _logger = logger; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            _logger.LogInformation("TenantProvisioningWorker started.");
            while (!stoppingToken.IsCancellationRequested) {
                int? tenantId = null;
                try { tenantId = await _queue.DequeueAsync(stoppingToken); } catch (OperationCanceledException) { break; }
                if (tenantId == null) continue;
                try {
                    _logger.LogInformation("Provisioning tenant {TenantId}", tenantId);
                    await _service.ProvisionAsync(tenantId.Value, stoppingToken);
                } catch (Exception ex) {
                    _logger.LogError(ex, "Unexpected error provisioning tenant {TenantId}", tenantId);
                }
            }
            _logger.LogInformation("TenantProvisioningWorker stopped.");
        }
    }
}
