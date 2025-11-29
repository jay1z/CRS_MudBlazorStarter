using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CRS.Middleware
{
    /// <summary>
    /// Middleware to enforce "Coming Soon" mode where only a specific admin email can access the site.
    /// All other users see the Coming Soon page.
    /// </summary>
    public class ComingSoonMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ComingSoonMiddleware> _logger;

        public ComingSoonMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ComingSoonMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var isEnabled = _configuration.GetValue<bool>("ComingSoon:Enabled", false);

            // If Coming Soon mode is not enabled, proceed normally
            if (!isEnabled)
            {
                await _next(context);
                return;
            }

            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            // Always allow access to essential routes
            if (ShouldBypassComingSoon(path))
            {
                await _next(context);
                return;
            }

            // Check if user is authenticated
            var user = context.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var userEmail = user.FindFirst(ClaimTypes.Email)?.Value ?? 
                               user.FindFirst(ClaimTypes.Name)?.Value;

                var allowedEmail = _configuration.GetValue<string>("ComingSoon:AllowedEmail");

                // If the authenticated user matches the allowed email, let them through
                if (!string.IsNullOrWhiteSpace(userEmail) && 
                    !string.IsNullOrWhiteSpace(allowedEmail) &&
                    userEmail.Equals(allowedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("ComingSoon: Allowing access for admin user {Email}", userEmail);
                    await _next(context);
                    return;
                }
            }

            // Redirect to Coming Soon page if not the allowed user
            if (!path.StartsWith("/comingsoon"))
            {
                _logger.LogDebug("ComingSoon: Redirecting to Coming Soon page from {Path}", path);
                context.Response.Redirect("/comingsoon");
                return;
            }

            await _next(context);
        }

        private static bool ShouldBypassComingSoon(string path)
        {
            return path.StartsWith("/comingsoon") ||
                   path.StartsWith("/account/login") ||
                   path.StartsWith("/account/logout") ||
                   path.StartsWith("/health") ||
                   path.StartsWith("/api") ||
                   path.StartsWith("/_framework") ||
                   path.StartsWith("/_content") ||
                   path.StartsWith("/css") ||
                   path.StartsWith("/js") ||
                   path.StartsWith("/images") ||
                   path.StartsWith("/favicon.ico") ||
                   path.StartsWith("/kanbanhub");
        }
    }

    public static class ComingSoonMiddlewareExtensions
    {
        public static IApplicationBuilder UseComingSoon(this IApplicationBuilder app)
            => app.UseMiddleware<ComingSoonMiddleware>();
    }
}
