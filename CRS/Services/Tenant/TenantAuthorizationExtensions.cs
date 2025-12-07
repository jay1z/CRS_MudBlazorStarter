using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using System.Security.Claims;

namespace CRS.Services.Tenant {
    public static class TenantAuthorizationExtensions {
        public const string TenantMatchPolicy = "TenantMatch";

        public static IServiceCollection AddTenantPolicies(this IServiceCollection services) {
            services.AddAuthorization(options => {
                options.AddPolicy(TenantMatchPolicy, policy =>
                policy.RequireAssertion(ctx => {
                    // If request not tenant-scoped (e.g., public pages), allow
                    var tenantCtx = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
                    if (string.IsNullOrEmpty(tenantCtx)) return true;

                    // Optionally, future: compare to resolver context if exposed in resource
                    return true;
                }));

            // Platform admin - accept either standard role claim or custom alx_role claim (seeded via UserRoleAssignments)
            options.AddPolicy("RequirePlatformAdmin", policy => policy.RequireAssertion(ctx =>
            {
                var roles = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                var alx = ctx.User?.FindAll("alx_role")?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                return roles.Contains("PlatformAdmin") || alx.Contains("PlatformAdmin");
            }));

                // Tenant Owner
                options.AddPolicy("RequireTenantOwner", policy => policy.RequireAssertion(ctx => {
                    var roleClaims = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
                    var tenantClaim = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
                    return roleClaims.Contains("TenantOwner") || roleClaims.Contains("PlatformAdmin");
                }));

                // Tenant Staff (Owner or Specialist)
                options.AddPolicy("RequireTenantStaff", policy => policy.RequireAssertion(ctx => {
                    var roles = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                    var alxRoles = ctx.User?.FindAll("alx_role")?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                    var tenantClaim = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
                    // PlatformAdmin can access all tenant staff pages
                    if (roles.Contains("PlatformAdmin") || alxRoles.Contains("PlatformAdmin")) return true;
                    return (roles.Contains("TenantOwner") || roles.Contains("TenantSpecialist")) && !string.IsNullOrEmpty(tenantClaim);
                }));

                // Tenant Viewer (Owner, Specialist, or Viewer)
                options.AddPolicy("RequireTenantViewer", policy => policy.RequireAssertion(ctx => {
                    var roles = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                    var alxRoles = ctx.User?.FindAll("alx_role")?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                    var tenantClaim = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
                    // PlatformAdmin can access all tenant viewer pages
                    if (roles.Contains("PlatformAdmin") || alxRoles.Contains("PlatformAdmin")) return true;
                    return (roles.Contains("TenantOwner") || roles.Contains("TenantSpecialist") || roles.Contains("TenantViewer")) && !string.IsNullOrEmpty(tenantClaim);
                }));

                // HOA User/Auditor
                options.AddPolicy("RequireHOAUser", policy => policy.RequireAssertion(ctx => {
                    var roles = ctx.User?.FindAll(ClaimTypes.Role)?.Select(c => c.Value).ToHashSet() ?? new HashSet<string>();
                    var tenantClaim = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
                    return (roles.Contains("HOAUser") || roles.Contains("HOAAuditor")) && !string.IsNullOrEmpty(tenantClaim);
                }));
            });
            return services;
        }
    }
}
