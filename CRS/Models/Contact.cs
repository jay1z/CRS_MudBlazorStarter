using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

namespace CRS.Models {
    public class Contact : BaseModel, IContact, CRS.Services.Tenant.ITenantScoped {
        [ForeignKey(nameof(ApplicationUser))] public Guid? ApplicationUserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        [DataType(DataType.EmailAddress)] public string? Email { get; set; }

        [Display(Name = "Phone")]
        [Phone]
        [DataType(DataType.PhoneNumber)] public string? Phone { get; set; }

        [Display(Name = "Ext.")]
        public string? Extension { get; set; }

        public string FullName {
            get {
                return $"{FirstName} {LastName}";
            }
        }
        public string FullNameInverted {
            get {
                return $"{LastName}, {FirstName}";
            }
        }

        // SaaS Refactor: Tenant scope
        public int TenantId { get; set; }
    }
}
