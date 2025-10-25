namespace CRS.Models {
    public class Community : BaseModel, CRS.Services.Tenant.ITenantScoped {
        public Community() {
            Addresses = new List<Address>();
            IsActive = true;
        }
        public string? Name { get; set; }

        public DateTime? AnnualMeetingDate { get; set; }

        public List<Address>? Addresses { get; set; } = [];

        public bool IsActive { get; set; }

        // SaaS Refactor: Tenant scope
        public int TenantId { get; set; }
    }
}
