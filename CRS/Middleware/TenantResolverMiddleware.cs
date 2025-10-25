using CRS.Services.Tenant;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using CRS.Data;
using CRS.Models;
using Microsoft.EntityFrameworkCore;

namespace CRS.Middleware {
    // SaaS Refactor: Middleware to resolve tenant from host and populate context
    public class TenantResolverMiddleware {
        private readonly RequestDelegate _next;
        public TenantResolverMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext, IDbContextFactory<ApplicationDbContext> dbFactory) {
            var host = context.Request.Host.Host; // e.g., acme.reserveapp.com
            var subdomain = ExtractSubdomain(host);

            if (!string.IsNullOrEmpty(subdomain)) {
                await using var db = await dbFactory.CreateDbContextAsync();
                var tenant = await db.Set<Tenant>().AsNoTracking().FirstOrDefaultAsync(t => t.Subdomain == subdomain);
                if (tenant != null) {
                    tenantContext.TenantId = tenant.Id;
                    tenantContext.TenantName = tenant.Name;
                    tenantContext.Subdomain = tenant.Subdomain;
                    tenantContext.IsActive = tenant.IsActive;
                    tenantContext.BrandingJson = tenant.BrandingJson; // pass branding
                }
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