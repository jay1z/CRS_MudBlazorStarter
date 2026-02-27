using Horizon.Services.Tenant;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Horizon.Data;
using Horizon.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Horizon.Middleware {
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

            Horizon.Models.Tenant? tenantFromHost = null;
            if (!string.IsNullOrEmpty(subdomain)) {
                try {
                    await using var db = await dbFactory.CreateDbContextAsync();
                    tenantFromHost = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain);
                } catch (Exception ex) {
                    _logger.LogError(ex, "TenantResolver: DB error resolving tenant for subdomain {Subdomain}", subdomain);
                }

                if (tenantFromHost == null && ShouldAllowRedirect(path)) {
                    // Check if this is a deferred checkout that's still being processed
                    var queryString = context.Request.QueryString.Value;
                    if (queryString != null && queryString.Contains("deferred=1")) {
                        // Tenant doesn't exist yet - webhook may still be processing
                        // Redirect to platform billing success page to poll for completion
                        var sessionId = context.Request.Query["session_id"].FirstOrDefault();
                        var platformHost = rootDomain ?? "alxreservecloud.com";
                        var scheme = context.Request.Scheme;
                        var redirectUrl = $"{scheme}://www.{platformHost}/billing/success?RequestContainsDeferred=true&session_id={sessionId}";
                        _logger.LogInformation("TenantResolver: Tenant not found for subdomain {Subdomain}, but deferred checkout detected. Redirecting to {Url}", subdomain, redirectUrl);
                        context.Response.Redirect(redirectUrl);
                        return;
                    }

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
                    var claimTenant = context.User.FindFirst(Horizon.Services.Tenant.TenantClaimTypes.TenantId)?.Value;
                    int.TryParse(claimTenant, out claimTenantId);
                    if (claimTenantId == 0) {
                        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                        if (!string.IsNullOrWhiteSpace(userId) && Guid.TryParse(userId, out var guid)) {
                            var user = await userManager.FindByIdAsync(userId);
                            if (user != null) claimTenantId = user.TenantId;
                        }
                    }

                    // SECURITY: Only enforce tenant isolation in production
                    // In development, allow platform admins to access any tenant via subdomain
                    var allowCrossTenantAccess = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
                    var isPlatformAdmin = context.User.IsInRole("PlatformAdmin");
                    
                    if (tenantFromHost != null && claimTenantId != 0 && claimTenantId != tenantFromHost.Id) {
                        // Allow platform admins to access any tenant in development
                        if (allowCrossTenantAccess && isPlatformAdmin) {
                            _logger.LogInformation("TenantResolver: Platform admin accessing different tenant. ClaimTenantId={ClaimTenantId} HostTenantId={HostTenantId}. Allowing access.", claimTenantId, tenantFromHost.Id);
                            // Override tenant context to match the host
                            tenantContext.TenantId = tenantFromHost.Id;
                            tenantContext.TenantName = tenantFromHost.Name;
                            tenantContext.Subdomain = tenantFromHost.Subdomain;
                            tenantContext.IsActive = tenantFromHost.IsActive;
                            tenantContext.BrandingJson = tenantFromHost.BrandingJson;
                            tenantContext.IsResolvedByLogin = false;
                        } else {
                            _logger.LogWarning("TenantResolver: Authenticated user tenant mismatch. ClaimTenantId={ClaimTenantId} HostTenantId={HostTenantId}. Signing out.", claimTenantId, tenantFromHost.Id);
                            await signInManager.SignOutAsync();
                            context.Response.Redirect("/Account/Login");
                            return;
                        }
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
            // BUT: preserve deferred checkout flow by checking for deferred param
            if (!tenantContext.IsPlatformHost && tenantContext.TenantId != null && path == "/") {
                var queryString = context.Request.QueryString.Value;

                // If this is a deferred checkout completion, redirect to billing success page with params
                if (queryString != null && queryString.Contains("deferred=1")) {
                    var sessionId = context.Request.Query["session_id"].FirstOrDefault();
                    var redirectUrl = $"/billing/success?RequestContainsDeferred=true&session_id={sessionId}";
                    _logger.LogInformation("TenantResolver: Deferred checkout detected, redirecting to {Url}", redirectUrl);
                    context.Response.Redirect(redirectUrl);
                    return;
                }

                context.Response.Redirect("/dashboard");
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