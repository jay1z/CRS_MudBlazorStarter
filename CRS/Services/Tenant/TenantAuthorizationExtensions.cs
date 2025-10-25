using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CRS.Services.Tenant
{
 public static class TenantAuthorizationExtensions
 {
 public const string TenantMatchPolicy = "TenantMatch";

 public static IServiceCollection AddTenantPolicies(this IServiceCollection services)
 {
 services.AddAuthorization(options =>
 {
 options.AddPolicy(TenantMatchPolicy, policy =>
 policy.RequireAssertion(ctx =>
 {
 // If request not tenant-scoped (e.g., public pages), allow
 var tenantCtx = ctx.User?.FindFirst(TenantClaimTypes.TenantId)?.Value;
 if (string.IsNullOrEmpty(tenantCtx)) return true;

 // Optionally, future: compare to resolver context if exposed in resource
 return true;
 }));
 });
 return services;
 }
 }
}
