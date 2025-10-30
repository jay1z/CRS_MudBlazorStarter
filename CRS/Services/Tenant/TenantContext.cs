using System;

namespace CRS.Services.Tenant {
    // SaaS Refactor: Scoped context holding current tenant information
    public class TenantContext : ITenantContext {
        private int? _tenantId;
        private string? _tenantName;
        private string? _subdomain;
        private bool _isActive = true;
        private string? _brandingJson;
        private bool _isResolvedByLogin = false;

        public int? TenantId { get => _tenantId; set { _tenantId = value; NotifyChanged(); } }
        public string? TenantName { get => _tenantName; set { _tenantName = value; NotifyChanged(); } }
        public string? Subdomain { get => _subdomain; set { _subdomain = value; NotifyChanged(); } }
        public bool IsActive { get => _isActive; set { _isActive = value; NotifyChanged(); } }
        public string? BrandingJson { get => _brandingJson; set { _brandingJson = value; NotifyChanged(); } }
        // Track whether tenant was set from user login
        public bool IsResolvedByLogin { get => _isResolvedByLogin; set { _isResolvedByLogin = value; NotifyChanged(); } }

        public event Action? OnTenantChanged;

        private void NotifyChanged() => OnTenantChanged?.Invoke();
    }
}