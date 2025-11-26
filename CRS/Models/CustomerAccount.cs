namespace CRS.Models {
    public class CustomerAccount : BaseModel, CRS.Services.Tenant.ITenantScoped {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int TenantId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}