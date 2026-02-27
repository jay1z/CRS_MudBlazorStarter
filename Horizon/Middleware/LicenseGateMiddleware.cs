using System.Threading.Tasks;
using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Tenant;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Middleware
{
    /// <summary>
    /// Middleware that enforces subscription-based access control.
    /// Handles different subscription states: Active, PastDue, GracePeriod, Suspended, etc.
    /// </summary>
    public class LicenseGateMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        
        public LicenseGateMiddleware(RequestDelegate next, IDbContextFactory<ApplicationDbContext> dbFactory) {
            _next = next;
            _dbFactory = dbFactory;
        }
        
        public async Task InvokeAsync(HttpContext context, ITenantContext tenantContext)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            
            // Allow access to public routes and account/billing pages (needed for reactivation)
            if (!tenantContext.TenantId.HasValue || 
                path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase) || 
                path.StartsWith("/billing", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/api/stripe", StringComparison.OrdinalIgnoreCase) || // Stripe webhooks
                path.StartsWith("/tenant", StringComparison.OrdinalIgnoreCase) || // Tenant signup/not-found
                path == "/" || 
                path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("/dev", StringComparison.OrdinalIgnoreCase)) // Dev endpoints
            {
                await _next(context);
                return;
            }
            
            // IMPORTANT: Exempt platform tenant from license checks
            // Platform admins should always have access
            if (tenantContext.TenantId.HasValue && tenantContext.Subdomain?.Equals("platform", StringComparison.OrdinalIgnoreCase) == true)
            {
                await _next(context);
                return;
            }
            
            // Get tenant subscription status from database
            await using var db = await _dbFactory.CreateDbContextAsync();
            var tenant = await db.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantContext.TenantId.Value);
            
            if (tenant == null)
            {
                context.Response.Redirect("/tenant/not-found");
                return;
            }
            
            // Handle different subscription states
            switch (tenant.SubscriptionStatus)
            {
                case SubscriptionStatus.Active:
                case SubscriptionStatus.Trialing:
                    // Full access - no restrictions
                    await _next(context);
                    break;
                    
                case SubscriptionStatus.PastDue:
                    // Full access but show payment warning banner
                    context.Items["ShowPaymentWarning"] = true;
                    context.Items["PaymentWarningMessage"] = "Your payment failed. Please update your payment method to avoid service interruption.";
                    await _next(context);
                    break;
                    
                case SubscriptionStatus.GracePeriod:
                    // Read-only access - block write operations (POST, PUT, DELETE, PATCH)
                    if (IsWriteOperation(context.Request))
                    {
                        // Redirect write attempts to grace period notice page
                        context.Response.Redirect("/Account/Billing/GracePeriod");
                        return;
                    }
                    // Allow read operations but show banner
                    context.Items["ReadOnlyMode"] = true;
                    context.Items["GracePeriodMessage"] = $"Your account is in read-only mode. Update payment by {tenant.GracePeriodEndsAt?.ToString("MMMM dd")} to restore full access.";
                    await _next(context);
                    break;
                    
                case SubscriptionStatus.Suspended:
                case SubscriptionStatus.MarkedForDeletion:
                    // No application access - redirect to reactivation page
                    context.Response.Redirect("/Account/Billing/Reactivate");
                    return;
                    
                case SubscriptionStatus.Canceled:
                case SubscriptionStatus.Unpaid:
                    // No access
                    context.Response.Redirect("/Account/Billing/Reactivate");
                    return;
                    
                case SubscriptionStatus.Incomplete:
                    // Payment pending - show payment completion page
                    context.Response.Redirect("/billing/success");
                    return;
                    
                default:
                    // Unknown state - deny access
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("Account status unknown. Please contact support.");
                    return;
            }
        }
        
        /// <summary>
        /// Determines if the HTTP request is a write operation (modifies data)
        /// </summary>
        private static bool IsWriteOperation(HttpRequest request)
        {
            return request.Method != "GET" && request.Method != "HEAD" && request.Method != "OPTIONS";
        }
    }

    public static class LicenseGateMiddlewareExtensions
    {
        public static IApplicationBuilder UseLicenseGate(this IApplicationBuilder app)
            => app.UseMiddleware<LicenseGateMiddleware>();
    }
}
