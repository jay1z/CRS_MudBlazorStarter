using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class ServiceContact : BaseModel {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string CompanyName { get; set; }
        [DataType(DataType.EmailAddress)] public required string Email { get; set; }
        [DataType(DataType.PhoneNumber)] public required string Phone { get; set; }
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
