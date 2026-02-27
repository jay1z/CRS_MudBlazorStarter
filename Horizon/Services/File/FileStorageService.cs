using System.Text;

using Horizon.Services.Tenant;

namespace Horizon.Services.File {
    // SaaS Refactor: stores files in per-tenant folder structure
    public interface IFileStorageService {
        string GetTenantRootPath(int tenantId);
        string GetUploadPath(int tenantId);
        string GetReportPath(int tenantId);
        string GetTenantRelativePath(string path);
    }

    public class FileStorageService : IFileStorageService {
        private readonly IWebHostEnvironment _env;
        private readonly ITenantContext _tenantContext;
        public FileStorageService(IWebHostEnvironment env, ITenantContext tenantContext) {
            _env = env;
            _tenantContext = tenantContext;
        }

        public string GetTenantRootPath(int tenantId) => Path.Combine(_env.WebRootPath ?? "wwwroot", "tenant-assets", $"tenant-{tenantId}");
        public string GetUploadPath(int tenantId) => Path.Combine(GetTenantRootPath(tenantId), "uploads");
        public string GetReportPath(int tenantId) => Path.Combine(GetTenantRootPath(tenantId), "reports");

        public string GetTenantRelativePath(string absolutePath) {
            var wwwroot = _env.WebRootPath ?? "wwwroot";
            var relative = absolutePath.Replace(wwwroot, "").Replace("\\", "/");
            if (!relative.StartsWith("/")) relative = "/" + relative;
            return relative;
        }

        // Convenience helpers with current tenant
        private int CurrentTenantId => _tenantContext.TenantId ?? 1;
        public string CurrentUploadPath() => EnsureDir(GetUploadPath(CurrentTenantId));
        public string CurrentReportPath() => EnsureDir(GetReportPath(CurrentTenantId));

        private static string EnsureDir(string path) {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
