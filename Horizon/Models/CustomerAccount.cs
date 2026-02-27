using System.ComponentModel.DataAnnotations;
using Horizon.Data;

namespace Horizon.Models {
    /// <summary>
    /// Represents a customer/client account (HOA board, management company, etc.)
    /// A customer can have multiple communities/properties.
    /// </summary>
    public class CustomerAccount : BaseModel, Horizon.Services.Tenant.ITenantScoped {
        public Guid Id { get; set; } = Guid.CreateVersion7();

        /// <summary>
        /// Tenant this customer belongs to
        /// </summary>
        public int TenantId { get; set; }

        /// <summary>
        /// Customer/company name (e.g., "ABC Management Company" or "Sunset HOA Board")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of customer
        /// </summary>
        public CustomerType Type { get; set; } = CustomerType.HOABoard;

        /// <summary>
        /// Primary contact person name
        /// </summary>
        [MaxLength(100)]
        public string? ContactName { get; set; }

        /// <summary>
        /// Primary contact email
        /// </summary>
        [MaxLength(255)]
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>
        /// Primary contact phone
        /// </summary>
        [MaxLength(20)]
        public string? Phone { get; set; }

        /// <summary>
        /// Billing/mailing address line 1
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine1 { get; set; }

        /// <summary>
        /// Billing/mailing address line 2
        /// </summary>
        [MaxLength(200)]
        public string? AddressLine2 { get; set; }

        /// <summary>
        /// City
        /// </summary>
        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// State/Province
        /// </summary>
        [MaxLength(50)]
        public string? State { get; set; }

        /// <summary>
        /// ZIP/Postal code
        /// </summary>
        [MaxLength(20)]
        public string? PostalCode { get; set; }

        /// <summary>
        /// Internal notes about this customer
        /// </summary>
        [MaxLength(2000)]
        public string? Notes { get; set; }

        /// <summary>
        /// Website URL
        /// </summary>
        [MaxLength(500)]
        public string? Website { get; set; }

        /// <summary>
        /// Tax ID / EIN for invoicing
        /// </summary>
        [MaxLength(50)]
        public string? TaxId { get; set; }

        /// <summary>
        /// Payment terms (e.g., "Net 30")
        /// </summary>
        [MaxLength(50)]
        public string? PaymentTerms { get; set; }

        /// <summary>
        /// Is this customer active?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// When the customer was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the customer was last updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// [DEPRECATED] Single user account - kept for backwards compatibility.
        /// Use CustomerAccountUsers collection for multi-user support.
        /// </summary>
        public Guid? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Users linked to this customer account (supports multiple users per account)
        /// </summary>
        public ICollection<CustomerAccountUser> CustomerAccountUsers { get; set; } = new List<CustomerAccountUser>();

        /// <summary>
        /// Pending invitations to join this customer account
        /// </summary>
        public ICollection<CustomerAccountInvitation> Invitations { get; set; } = new List<CustomerAccountInvitation>();

        /// <summary>
        /// Communities/properties owned or managed by this customer
        /// </summary>
        public ICollection<Community> Communities { get; set; } = new List<Community>();
    }

    /// <summary>
    /// Type of customer account
    /// </summary>
    public enum CustomerType {
        /// <summary>
        /// HOA Board directly contracting for services
        /// </summary>
        HOABoard = 0,

        /// <summary>
        /// Property management company
        /// </summary>
        ManagementCompany = 1,

        /// <summary>
        /// Individual property owner
        /// </summary>
        PropertyOwner = 2,

        /// <summary>
        /// Real estate developer
        /// </summary>
        Developer = 3,

        /// <summary>
        /// Other type of customer
        /// </summary>
        Other = 99
    }
}