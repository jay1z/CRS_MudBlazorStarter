using Microsoft.AspNetCore.Identity;

namespace CRS.Data {
    public class ApplicationUser : IdentityUser<Guid> {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public IList<string>? Roles { get; set; }
        public StatusEnum Status { get; set; } = StatusEnum.Active;

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

    public enum StatusEnum {
        Active,
        Inactive,
        LockedOut
    }
}
