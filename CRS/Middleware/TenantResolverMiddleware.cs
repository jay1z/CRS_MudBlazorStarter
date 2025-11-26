using CRS.Services.Tenant;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CRS.Middleware {
    public class TenantResolverMiddleware {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantResolverMiddleware> _logger;
        public TenantResolverMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<TenantResolverMiddleware> logger) { _next = next; _configuration = configuration; _logger = logger; }

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ApplicationDbContext> dbFactory, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) {
            var host = context.Request.Host.Host;
            var rootDomain = _configuration["App:RootDomain"];
            var subdomain = ExtractSubdomain(host, rootDomain);
            var path = context.Request.Path;

            CRS.Models.Tenant? tenantFromHost = null;
            if (!string.IsNullOrEmpty(subdomain)) {
                try {
                    await using var db = await dbFactory.CreateDbContextAsync();
                    tenantFromHost = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain);
                } catch (Exception ex) {
                    _logger.LogError(ex, "TenantResolver: DB error resolving tenant for subdomain {Subdomain}", subdomain);
                }

                if (tenantFromHost == null && ShouldAllowRedirect(path)) {
                    _logger.LogWarning("TenantResolver: Unknown subdomain {Subdomain} host={Host} path={Path}; redirecting not-found", subdomain, host, path);
                    var q = Uri.EscapeDataString(host);
                    context.Response.Redirect($"/tenant/not-found?host={q}");
                    return;
                }
            }

            bool auth = context.User?.Identity?.IsAuthenticated == true;
            int claimTenantId = 0;
            try {
                if (auth) {
                    var claimTenant = context.User.FindFirst(CRS.Services.Tenant.TenantClaimTypes.TenantId)?.Value;
                    int.TryParse(claimTenant, out claimTenantId);
                    if (claimTenantId == 0) {
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var guid)) {
                            var user = await userManager.FindByIdAsync(userId);
                            if (user != null) claimTenantId = user.TenantId;
                        }
                    }

                    if (tenantFromHost != null && claimTenantId != 0 && claimTenantId != tenantFromHost.Id) {
                        _logger.LogWarning("TenantResolver: Authenticated user tenant mismatch. ClaimTenantId={ClaimTenantId} HostTenantId={HostTenantId}. Signing out.", claimTenantId, tenantFromHost.Id);
                        await signInManager.SignOutAsync();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }

                    if (claimTenantId != 0) {
                        try {
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
                        } catch (Exception ex) {
                            _logger.LogError(ex, "TenantResolver: Error loading user tenant {TenantId}", claimTenantId);
                        }
                    }
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "TenantResolver: Unexpected error during authenticated tenant resolution.");
            }

            if (tenantContext.TenantId == null && tenantFromHost != null) {
                tenantContext.TenantId = tenantFromHost.Id;
                tenantContext.TenantName = tenantFromHost.Name;
                tenantContext.Subdomain = tenantFromHost.Subdomain;
                tenantContext.IsActive = tenantFromHost.IsActive;
                tenantContext.BrandingJson = tenantFromHost.BrandingJson;
            }

            tenantContext.IsPlatformHost = string.IsNullOrEmpty(subdomain);

            // Cache resolved values for Blazor circuit bootstrap
            context.Items["tenant.id"] = tenantContext.TenantId;
            context.Items["tenant.name"] = tenantContext.TenantName;
            context.Items["tenant.sub"] = tenantContext.Subdomain;
            context.Items["tenant.platform"] = tenantContext.IsPlatformHost;

            _logger.LogDebug("TenantResolver: host={Host} rootDomain={RootDomain} subdomain={Subdomain} auth={Auth} claimTenantId={ClaimTenantId} resolvedTenantId={ResolvedTenantId} isPlatformHost={IsPlatformHost}", host, rootDomain, subdomain ?? "(none)", auth, claimTenantId, tenantContext.TenantId, tenantContext.IsPlatformHost);

            // Optional: surface diagnostics via headers for manual inspection (dev only)
            if (context.Response.HasStarted == false && context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()) {
                context.Response.Headers["X-Tenant-Host"] = host;
                context.Response.Headers["X-Tenant-Subdomain"] = subdomain ?? "";
                context.Response.Headers["X-Tenant-ResolvedId"] = tenantContext.TenantId?.ToString() ?? "";
                context.Response.Headers["X-Tenant-IsPlatform"] = tenantContext.IsPlatformHost.ToString();
            }

            // Redirect tenant subdomain root to app to avoid flicker on '/' landing
            if (!tenantContext.IsPlatformHost && tenantContext.TenantId != null && path == "/") {
                context.Response.Redirect("/app");
                return;
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
            if (string.IsNullOrWhiteSpace(host)) return null;
            if (Regex.IsMatch(host, @"^\d+\.\d+\.\d+\.\d+$")) return null;

            // Special handling for localhost subdomains (e.g., tenant1.localhost)
            if (host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) {
                var parts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && parts[parts.Length - 1].Equals("localhost", StringComparison.OrdinalIgnoreCase)) {
                    var subdomain = parts[0];
                    if (!string.Equals(subdomain, "www", StringComparison.OrdinalIgnoreCase)) {
                        return subdomain.ToLowerInvariant();
                    }
                }
                return null; // plain localhost
            }

            string? sub = null;
            if (!string.IsNullOrWhiteSpace(rootDomain)) {
                var rd = rootDomain.Trim('.');
                // If rootDomain includes protocol accidentally, strip it
                rd = rd.Replace("http://", "", StringComparison.OrdinalIgnoreCase).Replace("https://", "", StringComparison.OrdinalIgnoreCase);
                if (host.Equals(rd, StringComparison.OrdinalIgnoreCase)) return null;
                if (host.EndsWith("." + rd, StringComparison.OrdinalIgnoreCase)) {
                    var remaining = host[..^(rd.Length + 1)]; // portion before .rootDomain
                    // Use first label only (support multi-level subdomains but treat first as tenant slug)
                    var first = remaining.Split('.', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(first) && !string.Equals(first, "www", StringComparison.OrdinalIgnoreCase)) sub = first.ToLowerInvariant();
                }
            }
            if (sub != null) return sub;
            // Fallback generic heuristic: require >=3 labels
            var hostParts = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (hostParts.Length >= 3) {
                var candidate = hostParts[0];
                if (!string.Equals(candidate, "www", StringComparison.OrdinalIgnoreCase)) return candidate.ToLowerInvariant();
            }
            return null;
        }
    }
}