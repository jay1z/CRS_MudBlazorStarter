using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace CRS.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
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
        var csp = string.Join("; ", new[]
        {
            "default-src 'self'",
            "base-uri 'self'",
            "frame-ancestors 'none'",
            "img-src 'self' data: https:",
            "style-src 'self' 'unsafe-inline' https:",
            "script-src 'self' 'unsafe-inline'",
            "connect-src 'self' https: wss:",
            "font-src 'self' data: https:"
        });
        headers["Content-Security-Policy"] = csp;

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
