using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

namespace CRS.Models {
    public class Profile : BaseModel {
        public ApplicationUser User { get; set; }
        [ForeignKey(nameof(ApplicationUser))] public Guid ApplicationUserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        [DataType(DataType.EmailAddress)] public string? Email { get; set; }
        [DataType(DataType.PhoneNumber)] public string? Phone { get; set; }
        public string? Extension { get; set; }
        [DataType(DataType.Text)] public string? Notes { get; set; }

        public string? Street { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        [DataType(DataType.PostalCode)] public string? Zip { get; set; }

        public string? FullAddress {
            get {
                return $"{Street}, {City}, {State} {Zip}";
            }
        }
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
