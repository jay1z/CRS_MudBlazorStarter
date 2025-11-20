using CRS.Services.Tenant;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CRS.Middleware {
    public class TenantResolverMiddleware {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        public TenantResolverMiddleware(RequestDelegate next, IConfiguration configuration) { _next = next; _configuration = configuration; }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) {
            var host = context.Request.Host.Host;
            var rootDomain = _configuration["App:RootDomain"];
            var subdomain = ExtractSubdomain(host, rootDomain);
            var path = context.Request.Path;

            CRS.Models.Tenant? tenantFromHost = null;
            if (!string.IsNullOrEmpty(subdomain)) {
                await using var db = await dbFactory.CreateDbContextAsync();
                tenantFromHost = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain);

                if (tenantFromHost == null && ShouldAllowRedirect(path)) {
                    var q = Uri.EscapeDataString(host);
                    context.Response.Redirect($"/tenant/not-found?host={q}");
                    return;
                }
            }

            try {
                if (context.User?.Identity?.IsAuthenticated == true) {
                    var claimTenant = context.User.FindFirst(CRS.Services.Tenant.TenantClaimTypes.TenantId)?.Value;
                    int.TryParse(claimTenant, out var claimTenantId);
                    if (claimTenantId == 0) {
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var guid)) {
                            var user = await userManager.FindByIdAsync(userId);
                            if (user != null) claimTenantId = user.TenantId;
                        }
                    }
                    if (tenantFromHost != null && claimTenantId != 0 && claimTenantId != tenantFromHost.Id) {
                        await signInManager.SignOutAsync();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                    if (claimTenantId != 0) {
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
            } catch { }

            if (tenantContext.TenantId == null && tenantFromHost != null) {
                tenantContext.TenantId = tenantFromHost.Id;
                tenantContext.TenantName = tenantFromHost.Name;
                tenantContext.Subdomain = tenantFromHost.Subdomain;
                tenantContext.IsActive = tenantFromHost.IsActive;
                tenantContext.BrandingJson = tenantFromHost.BrandingJson;
            }

            await _next(context);
        }

        private static bool ShouldAllowRedirect(PathString path) =>
            !path.StartsWithSegments("/tenant/not-found") &&
            !path.StartsWithSegments("/tenant/signup") &&
            !path.StartsWithSegments("/health") &&
            !path.StartsWithSegments("/api") &&
            !path.StartsWithSegments("/_framework") &&
            !path.StartsWithSegments("/_content") &&
            !path.StartsWithSegments("/kanbanhub") &&
            !path.StartsWithSegments("/favicon.ico");

        private static string? ExtractSubdomain(string host, string? rootDomain) {
            if (string.IsNullOrWhiteSpace(host) || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return null;
            if (Regex.IsMatch(host, @"^\d+\.\d+\.\d+\.\d+$")) return null;
            if (!string.IsNullOrWhiteSpace(rootDomain)) {
                var rd = rootDomain.Trim('.');
                if (host.Equals(rd, StringComparison.OrdinalIgnoreCase)) return null;
                if (host.EndsWith("." + rd, StringComparison.OrdinalIgnoreCase)) {
                    var prefix = host.Substring(0, host.Length - rd.Length);
                    if (prefix.EndsWith(".")) prefix = prefix[..^1];
                    var first = prefix.Split('.').FirstOrDefault();
                    if (string.IsNullOrEmpty(first)) return null;
                    if (string.Equals(first, "www", StringComparison.OrdinalIgnoreCase)) return null;
                    return first.ToLowerInvariant();
                }
            }
            var parts = host.Split('.');
            if (parts.Length < 3) return null;
            var candidate = parts[0];
            if (string.Equals(candidate, "www", StringComparison.OrdinalIgnoreCase)) return null;
            return candidate.ToLowerInvariant();
        }
    }
}