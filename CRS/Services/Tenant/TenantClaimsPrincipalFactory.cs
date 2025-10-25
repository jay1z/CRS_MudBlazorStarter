using System.Security.Claims;

using CRS.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CRS.Services.Tenant {
    // SaaS Refactor: Adds tenant claims to authenticated principals
    public class TenantClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<System.Guid>> {
        public TenantClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<System.Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user) {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim(TenantClaimTypes.TenantId, user.TenantId.ToString()));
            // Optional: tenant-friendly name can be user display or tenant label
            if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
                identity.AddClaim(new Claim(TenantClaimTypes.TenantName, $"{user.FirstName} {user.LastName}".Trim()));
            return identity;
        }
    }
}
