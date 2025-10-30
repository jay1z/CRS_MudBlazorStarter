using CRS.Services.Tenant;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace CRS.Middleware {
    // SaaS Refactor: Middleware to resolve tenant from host and populate context
    public class TenantResolverMiddleware {
        private readonly RequestDelegate _next;
        public TenantResolverMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<ApplicationUser> userManager) {
            var host = context.Request.Host.Host; // e.g., acme.reserveapp.com
            var subdomain = ExtractSubdomain(host);

            CRS.Models.Tenant? tenantFromHost = null;
            // Resolve host-based tenant (fallback)
            if (!string.IsNullOrEmpty(subdomain)) {
                await using var db = await dbFactory.CreateDbContextAsync();
                tenantFromHost = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain);
            }

            // Prefer authenticated user's tenant when available (user-driven selection)
            try {
                if (context.User?.Identity?.IsAuthenticated == true) {
                    // Try to read tenant id from claims first
                    var claimTenant = context.User.FindFirst(CRS.Services.Tenant.TenantClaimTypes.TenantId)?.Value;
                    int.TryParse(claimTenant, out var claimTenantId);

                    // If no claim, try to lookup user and use their TenantId
                    if (claimTenantId == 0) {
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var guid)) {
                            var user = await userManager.FindByIdAsync(userId);
                            if (user != null) claimTenantId = user.TenantId;
                        }
                    }

                    if (claimTenantId != 0) {
                        // Load tenant by id and set TenantContext from user tenant
                        await using var db = await dbFactory.CreateDbContextAsync();
                        var userTenant = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Id == claimTenantId);
                        if (userTenant != null) {
                            tenantContext.TenantId = userTenant.Id;
                            tenantContext.TenantName = userTenant.Name;
                            tenantContext.Subdomain = userTenant.Subdomain;
                            tenantContext.IsActive = userTenant.IsActive;
                            tenantContext.BrandingJson = userTenant.BrandingJson;
                            tenantContext.IsResolvedByLogin = true;
                        }
                    }
                }
            } catch {
                // ignore errors; tenant context is best-effort
            }

            // If no tenant set from authenticated user, fall back to host-resolved tenant
            if (tenantContext.TenantId == null && tenantFromHost != null) {
                tenantContext.TenantId = tenantFromHost.Id;
                tenantContext.TenantName = tenantFromHost.Name;
                tenantContext.Subdomain = tenantFromHost.Subdomain;
                tenantContext.IsActive = tenantFromHost.IsActive;
                tenantContext.BrandingJson = tenantFromHost.BrandingJson;
            }

            await _next(context);
        }

        private static string? ExtractSubdomain(string host) {
            // Handle localhost and direct hostnames without subdomain
            if (string.IsNullOrWhiteSpace(host) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return null;
            // If host is an IP
            if (Regex.IsMatch(host, @"^\d+\.\d+\.\d+\.\d+$")) return null;
            var parts = host.Split('.');
            if (parts.Length < 3) return null; // expecting sub.domain.tld
            return parts[0];
        }
    }
}