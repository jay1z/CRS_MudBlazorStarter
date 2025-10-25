namespace CRS.Services.Tenant {
    // SaaS Refactor: Provides access to the current request's tenant
    public interface ITenantContext {
        int? TenantId { get; set; }
        string? TenantName { get; set; }
        string? Subdomain { get; set; }
        bool IsActive { get; set; }
        // Branding payload for per-tenant theming
        string? BrandingJson { get; set; }
    }
}