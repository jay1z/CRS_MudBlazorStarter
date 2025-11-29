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
        
        // Additional property details for demo data
        public string? Address1 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int? NumberOfUnits { get; set; }
        public int? YearBuilt { get; set; }
        public string? Description { get; set; }
        
        // Demo Mode
        public bool IsDemo { get; set; } = false;
    }
}
