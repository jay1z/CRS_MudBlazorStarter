using CRS.Services.Tenant;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CRS.Middleware {
    // SaaS Refactor: Middleware to resolve tenant from host and populate context
    public class TenantResolverMiddleware {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        public TenantResolverMiddleware(RequestDelegate next, IConfiguration configuration) { _next = next; _configuration = configuration; }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) {
            var host = context.Request.Host.Host; // e.g., acme.example.com
            var rootDomain = _configuration["App:RootDomain"]; // optional; e.g., example.com
            var subdomain = ExtractSubdomain(host, rootDomain);
            var path = context.Request.Path;

            CRS.Models.Tenant? tenantFromHost = null;
            // Resolve host-based tenant (fallback)
            if (!string.IsNullOrEmpty(subdomain)) {
                await using var db = await dbFactory.CreateDbContextAsync();
                tenantFromHost = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain);

                // If a subdomain was specified but no tenant found, redirect to friendly page
                if (tenantFromHost == null &&
                    !path.StartsWithSegments("/tenant/not-found") &&
                    !path.StartsWithSegments("/tenant/signup") &&
                    !path.StartsWithSegments("/health") &&
                    !path.StartsWithSegments("/api") &&
                    !path.StartsWithSegments("/_framework") &&
                    !path.StartsWithSegments("/_content") &&
                    !path.StartsWithSegments("/kanbanhub") &&
                    !path.StartsWithSegments("/favicon.ico")) {
                    var q = Uri.EscapeDataString(host);
                    context.Response.Redirect($"/tenant/not-found?host={q}");
                    return;
                }
            }
            // If there is no subdomain, do not set tenant context. Homepage & public routes are allowed.

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

                    // Cross-tenant enforcement: if host is a tenant and user's tenant doesn't match, sign out
                    if (tenantFromHost != null && claimTenantId != 0 && claimTenantId != tenantFromHost.Id) {
                        await signInManager.SignOutAsync();
                        context.Response.Redirect("/Account/Login");
                        return;
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

        private static string? ExtractSubdomain(string host, string? rootDomain) {
            // Handle localhost and direct hostnames without subdomain
            if (string.IsNullOrWhiteSpace(host) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return null;
            // If host is an IP
            if (Regex.IsMatch(host, @"^\d+\.\d+\.\d+\.\d+$")) return null;

            // If a root domain is configured, use it to determine the subdomain reliably
            if (!string.IsNullOrWhiteSpace(rootDomain)) {
                var rd = rootDomain.Trim('.');
                if (host.Equals(rd, StringComparison.OrdinalIgnoreCase)) return null;
                if (host.EndsWith("." + rd, StringComparison.OrdinalIgnoreCase)) {
                    var prefix = host.Substring(0, host.Length - rd.Length);
                    if (prefix.EndsWith(".")) prefix = prefix[..^1]; // trim trailing dot
                    var first = prefix.Split('.').FirstOrDefault();
                    if (string.IsNullOrEmpty(first)) return null;
                    if (string.Equals(first, "www", StringComparison.OrdinalIgnoreCase)) return null;
                    return first.ToLowerInvariant();
                }
            }

            // Fallback heuristic: sub.domain.tld -> take first label
            var parts = host.Split('.');
            if (parts.Length < 3) return null; // expecting sub.domain.tld
            var candidate = parts[0];
            if (string.Equals(candidate, "www", StringComparison.OrdinalIgnoreCase)) return null;
            return candidate.ToLowerInvariant();
        }
    }
}