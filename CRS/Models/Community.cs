namespace CRS.Models {
    /// <summary>
    /// Represents a community (HOA, condo association, etc.)
    /// 
    /// ADDRESS MANAGEMENT:
    /// -------------------
    /// Physical Address (required): The primary location of the community.
    /// Mailing Address (optional): Separate mailing/correspondence address (PO Box, management office, etc.)
    /// 
    /// Both addresses are stored in the Addresses table and referenced via FK pointers.
    /// If MailingAddressId is null, the physical address is used for mailing.
    /// </summary>
    public class Community : BaseModel, CRS.Services.Tenant.ITenantScoped {
        public string? Name { get; set; }

        public DateTime? AnnualMeetingDate { get; set; }

        // Physical address (required) - primary location of the community
        public Guid PhysicalAddressId { get; set; }
        public Address? PhysicalAddress { get; set; }

        // Mailing address (optional) - if null, use physical address for mailing
        public Guid? MailingAddressId { get; set; }
        public Address? MailingAddress { get; set; }

        public bool IsActive { get; set; } = true;

        // SaaS Refactor: Tenant scope
        public int TenantId { get; set; }
        
        // Additional community details
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public int? NumberOfUnits { get; set; }
        public int? YearBuilt { get; set; }
        public string? Description { get; set; }
        
        // Demo Mode
        public bool IsDemo { get; set; } = false;

        /// <summary>
        /// Gets the effective mailing address (MailingAddress if set, otherwise PhysicalAddress)
        /// </summary>
        public Address? EffectiveMailingAddress => MailingAddress ?? PhysicalAddress;
    }
}

