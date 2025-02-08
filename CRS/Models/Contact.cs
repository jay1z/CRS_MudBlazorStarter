using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class Contact : BaseModel, IContact {
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Display(Name = "Email")]
        [DataType(DataType.EmailAddress)] public string? Email { get; set; }

        [Display(Name = "Phone")]
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
    }
}
