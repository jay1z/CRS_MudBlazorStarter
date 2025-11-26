using System.Security.Claims;

using CRS.Data;
using CRS.Models.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CRS.Services.Tenant {
    // SaaS Refactor: Adds tenant & role claims to authenticated principals
    public class TenantClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<System.Guid>> {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenantContext;

        public TenantClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<System.Guid>> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
        : base(userManager, roleManager, optionsAccessor) {
            _dbFactory = dbFactory;
            _tenantContext = tenantContext;
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user) {
            var identity = await base.GenerateClaimsAsync(user);

            // Always include current tenant id if available
            identity.AddClaim(new Claim(TenantClaimTypes.TenantId, (_tenantContext.TenantId ?? user.TenantId).ToString()));
            if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
                identity.AddClaim(new Claim(TenantClaimTypes.TenantName, $"{user.FirstName} {user.LastName}".Trim()));

            // Load scoped role assignments for current tenant or platform
            try {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var currentTenantId = _tenantContext.TenantId ?? user.TenantId;

                var assignments = await db.UserRoleAssignments
                    .Include(a => a.Role!)
                    .Where(a => a.UserId == user.Id && (a.TenantId == null || a.TenantId == currentTenantId))
                    .ToListAsync();


                foreach (var a in assignments) {
                    if (a.Role == null) continue;
                    identity.AddClaim(new Claim(ClaimTypes.Role, a.Role.Name));
                    identity.AddClaim(new Claim("alx_role", a.Role.Name));
                    if (a.TenantId.HasValue) identity.AddClaim(new Claim("alx_role_tenant", a.TenantId.ToString()));
                }
            } catch {
                // Best-effort; do not block sign-in on role load failures
            }

            return identity;
        }
    }
}
