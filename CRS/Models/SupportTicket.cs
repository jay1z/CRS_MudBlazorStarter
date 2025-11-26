namespace CRS.Models {
    public class SupportTicket : BaseModel, CRS.Services.Tenant.ITenantScoped {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int TenantId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open";
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
    }
}