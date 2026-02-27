using Microsoft.AspNetCore.Components.Forms;

namespace Horizon.Services.Storage {
    // Null implementation for development/testing without Azure Storage
    public class NullLogoStorageService : ILogoStorageService {
        private readonly ILogger<NullLogoStorageService> _logger;

        public NullLogoStorageService(ILogger<NullLogoStorageService> logger) {
            _logger = logger;
        }

        public Task<(bool Valid, string? Error)> ValidateLogoAsync(IBrowserFile file, CancellationToken ct = default) {
            _logger.LogWarning("NullLogoStorageService: Azure Storage not configured. Logo validation skipped.");
            return Task.FromResult((true, (string?)null));
        }

        public Task<string> UploadLogoAsync(int tenantId, IBrowserFile file, CancellationToken ct = default) {
            _logger.LogWarning("NullLogoStorageService: Azure Storage not configured. Logo upload skipped for tenant {TenantId}", tenantId);
            return Task.FromResult($"/placeholder-logo-{tenantId}.png");
        }

        public Task<bool> DeleteLogoAsync(int tenantId, CancellationToken ct = default) {
            _logger.LogWarning("NullLogoStorageService: Azure Storage not configured. Logo deletion skipped for tenant {TenantId}", tenantId);
            return Task.FromResult(false);
        }

        public Task<string?> GetLogoUrlAsync(int tenantId, CancellationToken ct = default) {
            _logger.LogWarning("NullLogoStorageService: Azure Storage not configured. Logo retrieval skipped for tenant {TenantId}", tenantId);
            return Task.FromResult((string?)null);
        }
    }
}
