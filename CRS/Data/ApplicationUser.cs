using Microsoft.AspNetCore.Identity;

namespace CRS.Data {
    // Add profile data for application users by adding properties to the ApplicationUser class
    public class ApplicationUser : IdentityUser {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public IList<string>? Roles { get; set; }
        public StatusEnum Status { get; set; } = StatusEnum.Active;
    }

    public enum StatusEnum {
        Active,
        Inactive,
        LockedOut
    }
}
