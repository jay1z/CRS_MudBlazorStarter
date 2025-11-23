using Coravel;
using Coravel.Events.Interfaces;

using CRS.Components;
using CRS.Components.Account;
using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Hubs;
using CRS.Middleware;
using CRS.Services;
using CRS.Services.Email;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using CRS.Services.MultiTenancy;
using CRS.Models.Workflow; // Added for workflow example
using CRS.Services.License; // add
using CRS.Services.Billing; // added billing services

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using MudBlazor.Services;

using Serilog;
using Serilog.Sinks.MSSqlServer;

using Ganss.Xss;
using System.Text.RegularExpressions;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);
ConfigureServices(builder);

var app = builder.Build();
await ConfigurePipeline(app);

var provider = app.Services;
IEventRegistration registration = provider.ConfigureEvents();
registration.Register<ReserveStudyCreatedEvent>().Subscribe<ReserveStudyCreatedListener>();
registration.Register<ProposalSentEvent>().Subscribe<ProposalSentListener>();
registration.Register<ProposalApprovedEvent>().Subscribe<ProposalApprovedListener>();
registration.Register<FinancialInfoRequestedEvent>().Subscribe<FinancialInfoRequestedListener>();
registration.Register<FinancialInfoSubmittedEvent>().Subscribe<FinancialInfoSubmittedListener>();
registration.Register<ReserveStudyCompletedEvent>().Subscribe<ReserveStudyCompletedListener>();

app.Run();

void ConfigureLogging(WebApplicationBuilder builder) {
    // Use Serilog configuration from appsettings.*
    builder.Host.UseSerilog((context, services, loggerConfiguration) => {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);
    });
}

void ConfigureServices(WebApplicationBuilder builder) {
    // Core services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<UserStateService>();
    builder.Services.AddScoped<IUserStateService>(sp => sp.GetRequiredService<UserStateService>());
    builder.Services.AddMudServices();
    // HTTP client factory for server-side HttpClient usage in components (e.g., InspectorPanel uploads)
    builder.Services.AddHttpClient();
    builder.Services.AddSignalR();

    // Health checks
    builder.Services.AddHealthChecks();

    // Register DbRetryService
    builder.Services.AddScoped<DbRetryService>();

    // Blazor services
    builder.Services.AddRazorComponents(options =>
        options.DetailedErrors = builder.Environment.IsDevelopment())
        .AddInteractiveServerComponents();

    // Authentication services
    builder.Services.AddCascadingAuthenticationState();
    builder.Services.AddScoped<IdentityUserAccessor>();
    builder.Services.AddScoped<IdentityRedirectManager>();
    builder.Services.AddScoped<AuthenticationStateProvider, CRS.Components.Account.IdentityRevalidatingAuthenticationStateProvider>();

    // Theming services
    // Use scoped ThemeService so it can see scoped ITenantContext
    builder.Services.AddScoped<ThemeService>();

    // Application services
    builder.Services.AddScoped<ICalendarService, CalendarService>();
    builder.Services.AddScoped<ICommunityService, CommunityService>();
    builder.Services.AddScoped<IContactService, ContactService>();
    builder.Services.AddScoped<IDashboardService, DashboardService>();
    builder.Services.AddScoped<IReserveStudyService, ReserveStudyService>();
    builder.Services.AddScoped<IReserveStudyWorkflowService, ReserveStudyWorkflowService>();
    builder.Services.AddScoped<ISignalRService, SignalRService>();
    builder.Services.AddScoped<IKanbanService, KanbanService>(); // Must go after SignalRService

    // Register workflow engine + notifications (new)
    builder.Services.AddScoped<CRS.Services.Workflow.INotificationService, CRS.Services.Workflow.NotificationService>();
    builder.Services.AddScoped<IStudyWorkflowService, CRS.Services.Workflow.StudyWorkflowService>();
    builder.Services.Configure<CRS.Services.Workflow.WorkflowOptions>(builder.Configuration.GetSection("Workflow"));

    // SaaS Refactor: register tenant services
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<TenantService>();

    // Remove Multi-tenant provisioning services and multi-DB resolvers
    // builder.Services.AddSingleton<ITenantDatabaseResolver, DefaultTenantDatabaseResolver>();
    // builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
    // builder.Services.AddScoped<ITenantMigrationService, TenantMigrationService>();
    // builder.Services.AddSingleton<ITenantProvisioningQueue, InMemoryTenantProvisioningQueue>();
    // builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
    // builder.Services.AddHostedService<CRS.Workers.TenantProvisioningWorker>();

    // User management and notification services
    builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
    // Messaging
    builder.Services.AddScoped<CRS.Services.Interfaces.IMessageService, CRS.Services.MessageService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IAppNotificationService, CRS.Services.AppNotificationService>();

    // License validation service (required by Login.razor)
    builder.Services.AddScoped<ILicenseValidationService, LicenseValidationService>();

    // Optionally enable background validator later
    // builder.Services.AddHostedService<LicenseValidationBackgroundService>();

    // Register homepage service
    builder.Services.AddScoped<TenantHomepageService>();

    // Authorization policy
    builder.Services.AddTenantPolicies();

    // Antiforgery options: relax SameSite to ensure cookie is sent on top-level POSTs
    builder.Services.AddAntiforgery(options => {
        options.Cookie.Name = ".CRS.AntiForgery";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

    // Register Coravel
    builder.Services.AddMailer(builder.Configuration);
    builder.Services.AddScoped<ReserveStudyCreatedListener>();
    builder.Services.AddScoped<ProposalSentListener>();
    builder.Services.AddScoped<ProposalApprovedListener>();
    builder.Services.AddScoped<FinancialInfoRequestedListener>();
    builder.Services.AddScoped<FinancialInfoSubmittedListener>();
    builder.Services.AddScoped<ReserveStudyCompletedListener>();
    builder.Services.AddEvents();

    // Register controllers for API endpoints (media upload, etc.)
    builder.Services.AddControllers();

    // Register FluentValidation validators for request DTOs (automatic registration)
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.AddValidatorsFromAssemblyContaining<CRS.Controllers.Requests.Validators.SendProposalRequestValidator>();

    // Add memory cache service
    builder.Services.AddMemoryCache();

    // Register the renderer
    builder.Services.AddScoped<HtmlRenderer>();
    builder.Services.AddScoped<IRazorComponentRenderer, RazorComponentRenderer>();

    // Authentication configuration
    builder.Services.AddAuthentication(options => {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    }).AddIdentityCookies();

    // Database configuration
    ConfigureDatabases(builder);

    // Identity configuration
    ConfigureIdentity(builder);

    // IMPORTANT: Register custom claims principal factory AFTER AddIdentityCore so it overrides the default
    builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TenantClaimsPrincipalFactory>();

    // Additional services
    builder.Services.AddQuickGridEntityFrameworkAdapter();
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();

    // CORS for subdomains (SignalR/JS interop scenarios)
    builder.Services.AddCors(o => {
        o.AddPolicy("AllowSubdomains", policy => {
            var root = builder.Configuration["App:RootDomain"]; // e.g., example.com
            policy.SetIsOriginAllowed(origin => {
                if (string.IsNullOrEmpty(root)) return true; // allow all if not configured
                try {
                    var u = new Uri(origin);
                    var h = u.Host;
                    return h.Equals(root, StringComparison.OrdinalIgnoreCase) || h.EndsWith("." + root, StringComparison.OrdinalIgnoreCase);
                } catch { return false; }
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });

    // Billing configuration (Stripe + URLs)
    builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));
    builder.Services.Configure<BillingUrlOptions>(builder.Configuration.GetSection("Billing:Urls"));
    builder.Services.AddSingleton<IStripeClientFactory, StripeClientFactory>();
    builder.Services.AddScoped<IBillingService, BillingService>();
    builder.Services.AddScoped<IFeatureGuardService, FeatureGuardService>();

}

void ConfigureDatabases(WebApplicationBuilder builder) {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // Register scoped DbContext for consumers like Identity stores, RoleManager/UserManager, and seeding logic
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

    builder.Services.AddDbContextFactory<ApplicationDbContext>(
        options => options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()),
        ServiceLifetime.Scoped);
}

void ConfigureIdentity(WebApplicationBuilder builder) {
    var requireConfirmed = builder.Configuration.GetValue<bool>("Identity:RequireConfirmedAccount", !builder.Environment.IsDevelopment());

    builder.Services.AddIdentityCore<ApplicationUser>(options => {
        // Sign-in policies
        options.SignIn.RequireConfirmedAccount = requireConfirmed;

        // Password policy
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 12;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequiredUniqueChars = 1;

        // Lockout policy
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);

        // User settings
        options.User.RequireUniqueEmail = true;
    })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager<SignInManager<ApplicationUser>>()
        .AddDefaultTokenProviders();

    // Token lifespan (e.g., email confirmation, reset password)
    builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
        o.TokenLifespan = TimeSpan.FromHours(3));

    // Configure Identity cookies for Blazor Server
    builder.Services.ConfigureApplicationCookie(options => {
        options.Cookie.Name = ".CRS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";

        // IMPORTANT: Keep sessions tenant-specific by default.
        // Only share auth cookie across subdomains if explicitly enabled via configuration.
        var rootDomain = builder.Configuration["App:RootDomain"]; // e.g., alxreservecloud.com
        var shareAcross = builder.Configuration.GetValue<bool>("App:ShareAuthAcrossSubdomains", false);
        if (shareAcross && !string.IsNullOrWhiteSpace(rootDomain)) {
            options.Cookie.Domain = "." + rootDomain.Trim('.');
        }
    });

    // Configure UserManager lifetime
    var userManagerServiceDescriptor = builder.Services.FirstOrDefault(
        descriptor => descriptor.ServiceType == typeof(UserManager<ApplicationUser>));
    if (userManagerServiceDescriptor != null) {
        builder.Services.Remove(userManagerServiceDescriptor);
    }

    var userStoreServiceDescriptor = builder.Services.FirstOrDefault(
        descriptor => descriptor.ServiceType == typeof(IUserStore<ApplicationUser>));
    if (userStoreServiceDescriptor != null) {
        builder.Services.Remove(userStoreServiceDescriptor);
    }

    builder.Services.AddScoped<UserManager<ApplicationUser>>();
    builder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>>();

    // Register RoleManager with Guid key type
    builder.Services.AddScoped<RoleManager<IdentityRole<Guid>>>(sp => {
        var roleStore = new RoleStore<IdentityRole<Guid>, ApplicationDbContext, Guid>(sp.GetRequiredService<ApplicationDbContext>());
        var options = sp.GetRequiredService<IOptions<IdentityOptions>>();
        var logger = sp.GetRequiredService<ILogger<RoleManager<IdentityRole<Guid>>>>();
        var keyNormalizer = sp.GetRequiredService<ILookupNormalizer>();
        var errors = sp.GetRequiredService<IdentityErrorDescriber>();
        var services = sp.GetRequiredService<IServiceProvider>();
        var validators = new List<IRoleValidator<IdentityRole<Guid>>>();
        return new RoleManager<IdentityRole<Guid>>(roleStore, validators, keyNormalizer, errors, logger);
    });
}

async Task ConfigurePipeline(WebApplication app) {
    // Perform database seeding
    await SeedDatabase(app);

    // Configure the HTTP request pipeline based on environment
    if (app.Environment.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
    } else {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseMigrationsEndPoint();
    app.UseHttpsRedirection();

    // Security headers
    app.UseSecurityHeaders();

    // SaaS Refactor: resolve tenant before auth
    app.UseMiddleware<TenantResolverMiddleware>();
    app.UseLicenseGate();

    // Add request audit middleware after tenant resolution and before auth
    app.UseMiddleware<RequestAuditMiddleware>();

    app.UseCors("AllowSubdomains");

    // Apply tenant theme per-request (skip if unchanged within the request's scope)
    app.Use(async (ctx, next) => {
        var themeSvc = ctx.RequestServices.GetRequiredService<ThemeService>();
        themeSvc.ApplyTenantBrandingIfAvailableAndChanged();
        await next();
    });

    app.UseAntiforgery();

    // Authenticate and authorize requests
    app.UseAuthentication();
    app.UseAuthorization();

    // Dev helper: promote a user to Admin quickly
    if (app.Environment.IsDevelopment()) {
        app.MapGet("/dev/make-admin", async (string email, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager) => {
            if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest("email is required");
            if (!await roleManager.RoleExistsAsync("Admin")) await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return Results.NotFound($"User '{email}' not found.");
            var inRole = await userManager.IsInRoleAsync(user, "Admin");
            if (!inRole) {
                var r = await userManager.AddToRoleAsync(user, "Admin");
                if (!r.Succeeded) return Results.BadRequest(string.Join(", ", r.Errors.Select(e => e.Description)));
            }
            return Results.Text($"'{email}' is now in Admin role.");
        }).WithDisplayName("Dev: Promote user to Admin");

        // Example workflow transition + notification trigger
        app.MapGet("/dev/workflow/example", async (IStudyWorkflowService engine) => {
            var req = new StudyRequest { TenantId =1, CommunityId = Guid.CreateVersion7() };
            var ok = await engine.TryTransitionAsync(req, StudyStatus.PendingDetails, "dev");
            return Results.Json(new { ok, from = StudyStatus.NewRequest.ToString(), to = StudyStatus.PendingDetails.ToString(), stateChangedAt = req.StateChangedAt });
        }).WithDisplayName("Dev: Workflow example");

        // Tenant preview endpoint only in Development
        app.MapGet("/tenant/preview/{tenantId:int}", async (int tenantId, TenantHomepageService homepageService) => {
            try {
                var homepage = await homepageService.GetByTenantIdAsync(tenantId);
                if (homepage != null && homepage.IsPublished && !string.IsNullOrWhiteSpace(homepage.PublishedHtml)) {
                    return Results.Content(homepage.PublishedHtml!, "text/html");
                }
            } catch { }
            return Results.NotFound();
        }).WithDisplayName("Dev: Tenant homepage preview");
    }

    // Map endpoints
    app.MapStaticAssets();
    app.MapControllers();
    app.MapHub<KanbanHub>("/kanbanhub");

    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    app.MapAdditionalIdentityEndpoints();

    app.MapHealthChecks("/health");
}

async Task SeedDatabase(WebApplication app) {
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        logger.LogInformation("[Seed] Migrating database...");
        await dbContext.Database.MigrateAsync();

        logger.LogInformation("[Seed] Seeding roles...");
        await SeedManager.SeedScopedRolesAsync(services);
        logger.LogInformation("[Seed] Ensuring platform tenant...");
        await SeedManager.SeedTenantsAndAdminsAsync(services);
        logger.LogInformation("[Seed] Seeding platform admin user...");
        await SeedManager.SeedAdminUserAsync(services);
        if (app.Environment.IsDevelopment()) {
            logger.LogInformation("[Seed] Seeding test users...");
            await SeedManager.SeedTestUsersAsync(services);
        }
        logger.LogInformation("[Seed] Completed.");
    } catch (Exception ex) {
        logger.LogError(ex, "Error during database seeding");
    }
}

// DTOs
public record SaveRequest(string Name, string Html, string? ComponentsJson);
public record TenantFindRequest(string Email);
