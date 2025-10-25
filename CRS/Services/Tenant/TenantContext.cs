namespace CRS.Services.Tenant {
    // SaaS Refactor: Scoped context holding current tenant information
    public class TenantContext : ITenantContext {
        public int? TenantId { get; set; }
        public string? TenantName { get; set; }
        public string? Subdomain { get; set; }
        public bool IsActive { get; set; } = true;
        public string? BrandingJson { get; set; }
    }
}