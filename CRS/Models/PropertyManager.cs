using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Data;

namespace Horizon.Models {
    public class PropertyManager : BaseModel, IContact, Horizon.Services.Tenant.ITenantScoped {
        [ForeignKey(nameof(ApplicationUser))] public Guid? ApplicationUserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [EmailAddress]
        [DataType(DataType.EmailAddress)] public string? Email { get; set; }

        [Phone]
        [DataType(DataType.PhoneNumber)] public string? Phone { get; set; }

        public string? Extension { get; set; }

        public string FullName => $"{FirstName} {LastName}";
        public string FullNameInverted => $"{LastName}, {FirstName}";

        // SaaS Refactor: Tenant scope
        public int TenantId { get; set; }
    }
}
