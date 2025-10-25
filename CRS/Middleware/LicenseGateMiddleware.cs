using System.Threading.Tasks;
using CRS.Services.Tenant;
using Microsoft.AspNetCore.Http;

namespace CRS.Middleware
{
 // Blocks requests for inactive tenants
 public class LicenseGateMiddleware
 {
 private readonly RequestDelegate _next;
 public LicenseGateMiddleware(RequestDelegate next) { _next = next; }
 public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
 {
 // Allow non-tenant routes (e.g., home, account) to pass
 var path = context.Request.Path.Value ?? string.Empty;
 if (!tenantContext.TenantId.HasValue || path.StartsWith("/Account") || path == "/" || path.StartsWith("/health"))
 {
 await _next(context);
 return;
 }
 if (!tenantContext.IsActive)
 {
 context.Response.StatusCode = StatusCodes.Status403Forbidden;
 await context.Response.WriteAsync("Tenant license inactive");
 return;
 }
 await _next(context);
 }
 }

 public static class LicenseGateMiddlewareExtensions
 {
 public static IApplicationBuilder UseLicenseGate(this IApplicationBuilder app)
 => app.UseMiddleware<LicenseGateMiddleware>();
 }
}
