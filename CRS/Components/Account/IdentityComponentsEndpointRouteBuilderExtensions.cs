using System.Security.Claims;
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
    }
}
