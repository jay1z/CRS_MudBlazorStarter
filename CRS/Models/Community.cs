namespace CRS.Models {
    /// <summary>
    /// Represents a community (HOA, condo association, etc.)
    /// 
    /// ADDRESS MANAGEMENT RULES:
    /// ------------------------
    /// 1. A Community can have MULTIPLE addresses:
    ///    - Physical addresses (AddressType.Physical) - homes/buildings in the community
    ///    - Mailing address (AddressType.Mailing) - main office/correspondence address
    /// 
    /// 2. Physical addresses (homes/buildings):
    ///    - Can have many (e.g., 100 Oak St, 102 Oak St, etc.)
    ///    - IsMailingAddress = false
    ///    - AddressType = Physical
    /// 
    /// 3. Mailing address (main office):
    ///    - ONLY ONE per community (enforced by unique index)
    ///    - IsMailingAddress = true
    ///    - AddressType = Mailing
    ///    - Used for official correspondence, billing, etc.
    /// 
    /// 4. The mailing address is typically:
    ///    - NOT one of the physical addresses
    ///    - A PO Box, management office, or separate location
    ///    - Can be set to match a physical address if needed
    /// 
    /// Query patterns:
    /// - Physical addresses: Addresses.Where(a => a.AddressType == AddressType.Physical)
    /// - Mailing address: Addresses.FirstOrDefault(a => a.IsMailingAddress)
    /// </summary>
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
        
        // Phase 1: Removed duplicate address fields (Address1, City, State, ZipCode)
        // Use Addresses collection instead for normalized data
        
        // Additional community details
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int? NumberOfUnits { get; set; }
        public int? YearBuilt { get; set; }
        public string? Description { get; set; }
        
        // Demo Mode
        public bool IsDemo { get; set; } = false;
    }
}

