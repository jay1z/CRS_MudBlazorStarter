using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace CRS.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment env)
    {
        _next = next;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";
        // Clickjacking protection
        headers["X-Frame-Options"] = "DENY";
        // Cross-domain policies
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        // Referrer policy
        headers["Referrer-Policy"] = "no-referrer";
        // Basic Permissions-Policy (previously Feature-Policy)
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        // CSP tuned for Blazor Server + MudBlazor + SignalR
        // Note: adjust if you load external resources (fonts, images)
        var cspSources = new List<string>
        {
            "default-src 'self'",
            "base-uri 'self'",
            "frame-ancestors 'none'",
            // Allow images from self, data URIs, and any https host (needed for some CDN images)
            "img-src 'self' data: https:",
            // Allow styles from self, inline styles (for MudBlazor) and https hosts (CDNs)
            "style-src 'self' 'unsafe-inline' https:",
            // Allow scripts from self, inline (required by some libs), and Stripe's script domain
            "script-src 'self' 'unsafe-inline' https://js.stripe.com",
            // Allow framing content from Stripe for pricing table / checkout widgets
            "frame-src 'self' https://js.stripe.com https://checkout.stripe.com",
            // allow secure websockets and https by default; connect-src can include Stripe API if needed
            "connect-src 'self' https: wss:",
            // Fonts from self, data URIs, and https hosts
            "font-src 'self' data: https:"
        };

        // In development, allow http: and ws: schemes (BrowserLink, local dev servers)
        if (_env.IsDevelopment())
        {
            // Append additional connect-src allowances for non-TLS local dev tooling
            // Replace the connect-src entry with one that includes http: and ws:
            cspSources = cspSources.Select(s => s.StartsWith("connect-src") ? "connect-src 'self' https: wss: http: ws:" : s).ToList();

            // Also allow eval for development if necessary (avoid in production)
            // Add 'unsafe-eval' to script-src if not already present
            cspSources = cspSources.Select(s => {
                if (s.StartsWith("script-src") && !s.Contains("'unsafe-eval'"))
                    s = s + " 'unsafe-eval'";
                // Also allow https: for script-src in development so CDN scripts can load
                if (s.StartsWith("script-src") && !s.Contains("https:"))
                    s = s + " https:";
                return s;
            }).ToList();
        }

        var csp = string.Join("; ", cspSources);
        headers["Content-Security-Policy"] = csp;

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
