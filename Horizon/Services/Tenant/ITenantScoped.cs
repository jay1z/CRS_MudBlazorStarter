namespace Horizon.Services.Tenant {
    // SaaS Refactor: Marks an entity as tenant-scoped
    public interface ITenantScoped {
        int TenantId { get; set; }
    }
}