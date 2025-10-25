using Microsoft.AspNetCore.Identity;

namespace CRS.Data {
    public class ApplicationUser : IdentityUser<Guid> {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Title { get; set; }
        public IList<string>? Roles { get; set; }
        public StatusEnum Status { get; set; } = StatusEnum.Active;

        // SaaS Refactor: Tenant association for identity
        public int TenantId { get; set; } = 1; // default backfill

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

        public string Initials { get { return GetUserInitials(); } }

        private string GetUserInitials() {
            var firstInitial = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : string.Empty;
            var lastInitial = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpper() : string.Empty;

            return $"{firstInitial}{lastInitial}";
        }

    }

    public enum StatusEnum {
        Active,
        Inactive,
        LockedOut
    }
}
