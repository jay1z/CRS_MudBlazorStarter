using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class ServiceContact : BaseModel {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        public string? CompanyName { get; set; }

        [DataType(DataType.EmailAddress)] public string? Email { get; set; }

        [DataType(DataType.PhoneNumber)] public string? Phone { get; set; }
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
