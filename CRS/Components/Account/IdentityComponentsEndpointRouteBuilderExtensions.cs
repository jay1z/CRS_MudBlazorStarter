using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CRS.Components.Account.Pages;
using CRS.Components.Account.Pages.Manage;
using CRS.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing
{
    internal static class IdentityComponentsEndpointRouteBuilderExtensions
    {
        // These endpoints are required by the Identity Razor components defined in the /Components/Account/Pages directory of this project.
        public static IEndpointConventionBuilder MapAdditionalIdentityEndpoints(this IEndpointRouteBuilder endpoints)
        {
            ArgumentNullException.ThrowIfNull(endpoints);

            var accountGroup = endpoints.MapGroup("/Account");

            accountGroup.MapPost("/PerformExternalLogin", (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider,
                [FromForm] string returnUrl) =>
            {
                IEnumerable<KeyValuePair<string, StringValues>> query = [
                    new("ReturnUrl", returnUrl),
                    new("Action", ExternalLogin.LoginCallbackAction)];

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/ExternalLogin",
                    QueryString.Create(query));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return TypedResults.Challenge(properties, [provider]);
            });

            // Helper to aggressively remove auth cookies including legacy domain-scoped ones
            static void RemoveAuthCookies(HttpContext context, IOptionsMonitor<CookieAuthenticationOptions> options, IConfiguration config)
            {
                var appOpts = options.Get(IdentityConstants.ApplicationScheme);
                var configured = appOpts.Cookie.Name ?? ".AspNetCore.Identity.Application";
                var names = new[] { configured, ".AspNetCore.Identity.Application", ".AspNetCore.Cookies" };

                // Host-scoped
                foreach (var name in names)
                {
                    context.Response.Cookies.Delete(name, new CookieOptions { Path = "/" });
                }

                // Legacy domain-scoped
                var root = config["App:RootDomain"]?.Trim('.') ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(root))
                {
                    foreach (var name in names)
                    {
                        context.Response.Cookies.Delete(name, new CookieOptions { Path = "/", Domain = "." + root });
                    }
                }
            }

            static string CoerceSafeReturnUrl(string? returnUrl)
            {
                if (string.IsNullOrWhiteSpace(returnUrl)) return "/";
                var candidate = "/" + returnUrl.TrimStart('/');
                // Ensure relative only
                return Uri.IsWellFormedUriString(candidate, UriKind.Relative) ? candidate : "/";
            }

            accountGroup.MapPost("/Logout", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromServices] IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
                [FromServices] IConfiguration config,
                [FromForm] string? returnUrl) =>
            {
                await signInManager.SignOutAsync();
                RemoveAuthCookies(context, cookieOptions, config);
                var target = CoerceSafeReturnUrl(returnUrl);
                return TypedResults.Redirect(target);
            }).DisableAntiforgery();

            // Allow GET /Account/Logout for convenience
            // GET /Account/Logout also disables antiforgery to avoid 400s if proxies strip headers
            accountGroup.MapGet("/Logout", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromServices] IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
                [FromServices] IConfiguration config,
                [FromQuery] string? returnUrl) =>
            {
                await signInManager.SignOutAsync();
                RemoveAuthCookies(context, cookieOptions, config);
                var target = CoerceSafeReturnUrl(returnUrl);
                return TypedResults.Redirect(target);
            }).DisableAntiforgery();

            var manageGroup = accountGroup.MapGroup("/Manage").RequireAuthorization();

            manageGroup.MapPost("/LinkExternalLogin", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromForm] string provider) =>
            {
                // Clear the existing external cookie to ensure a clean login process
                await context.SignOutAsync(IdentityConstants.ExternalScheme);

                var redirectUrl = UriHelper.BuildRelative(
                    context.Request.PathBase,
                    "/Account/Manage/ExternalLogins",
                    QueryString.Create("Action", ExternalLogins.LinkLoginCallbackAction));

                var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, signInManager.UserManager.GetUserId(context.User));
                return TypedResults.Challenge(properties, [provider]);
            });

            var loggerFactory = endpoints.ServiceProvider.GetRequiredService<ILoggerFactory>();
            var downloadLogger = loggerFactory.CreateLogger("DownloadPersonalData");

            // Endpoint to sign in a user by ID (used after registration in Blazor components)
            accountGroup.MapGet("/LoginCallback", async (
                HttpContext context,
                [FromServices] SignInManager<ApplicationUser> signInManager,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromQuery] string userId,
                [FromQuery] string token,
                [FromQuery] string? returnUrl) =>
            {
                // Validate the token (simple HMAC of userId + timestamp, valid for 5 minutes)
                if (!ValidateLoginToken(userId, token))
                {
                    return Results.BadRequest("Invalid or expired login token");
                }

                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Results.NotFound("User not found");
                }

                await signInManager.SignInAsync(user, isPersistent: true);

                var target = CoerceSafeReturnUrl(returnUrl);
                return TypedResults.LocalRedirect(target);
            });

            manageGroup.MapPost("/DownloadPersonalData", async (
                HttpContext context,
                [FromServices] UserManager<ApplicationUser> userManager,
                [FromServices] AuthenticationStateProvider authenticationStateProvider) =>
            {
                var user = await userManager.GetUserAsync(context.User);
                if (user is null)
                {
                    return Results.NotFound($"Unable to load user with ID '{userManager.GetUserId(context.User)}'.");
                }

                var userId = await userManager.GetUserIdAsync(user);
                downloadLogger.LogInformation("User with ID '{UserId}' asked for their personal data.", userId);

                // Only include personal data for download
                var personalData = new Dictionary<string, string>();
                var personalDataProps = typeof(ApplicationUser).GetProperties().Where(
                    prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
                foreach (var p in personalDataProps)
                {
                    personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
                }

                var logins = await userManager.GetLoginsAsync(user);
                foreach (var l in logins)
                {
                    personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
                }

                personalData.Add("Authenticator Key", (await userManager.GetAuthenticatorKeyAsync(user))!);
                var fileBytes = JsonSerializer.SerializeToUtf8Bytes(personalData);

                context.Response.Headers.TryAdd("Content-Disposition", "attachment; filename=PersonalData.json");
                return TypedResults.File(fileBytes, contentType: "application/json", fileDownloadName: "PersonalData.json");
            });

            return accountGroup;
        }

        // Simple token generation for login callback (valid for 5 minutes)
        private static readonly byte[] TokenKey = Encoding.UTF8.GetBytes("CRS-Login-Callback-Secret-Key-2024");

        public static string GenerateLoginToken(string userId)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var data = $"{userId}:{timestamp}";
            using var hmac = new HMACSHA256(TokenKey);
            var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{timestamp}:{hash}"));
        }

        private static bool ValidateLoginToken(string userId, string? token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            try
            {
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
                var parts = decoded.Split(':');
                if (parts.Length != 2) return false;

                var timestamp = long.Parse(parts[0]);
                var providedHash = parts[1];

                // Check if token is expired (5 minute window)
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now - timestamp > 300) return false;

                // Verify hash
                var data = $"{userId}:{timestamp}";
                using var hmac = new HMACSHA256(TokenKey);
                var expectedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
                return providedHash == expectedHash;
            }
            catch
            {
                return false;
            }
        }
    }
}
