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

var builder = WebApplication.CreateBuilder(args);

ConfigureLogging(builder);
ConfigureServices(builder);

var app = builder.Build();
await ConfigurePipeline(app);

var provider = app.Services;
IEventRegistration registration = provider.ConfigureEvents();
registration.Register<ReserveStudyCreatedEvent>().Subscribe<ReserveStudyCreatedListener>();

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
    builder.Services.AddMudServices();
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
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

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

    // SaaS Refactor: register tenant services
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<TenantService>();
    builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TenantClaimsPrincipalFactory>();
    builder.Services.AddScoped<CRS.Services.File.IFileStorageService, CRS.Services.File.FileStorageService>();
    builder.Services.AddScoped<CRS.Services.License.ILicenseValidationService, CRS.Services.License.LicenseValidationService>();
    // Optionally enable this recurring validator later
    // builder.Services.AddHostedService<CRS.Services.License.LicenseValidationBackgroundService>();

    // Authorization policy
    builder.Services.AddTenantPolicies();

    // Register Coravel
    builder.Services.AddMailer(builder.Configuration);
    builder.Services.AddScoped<ReserveStudyCreatedListener>();
    builder.Services.AddEvents();

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

    // Additional services
    builder.Services.AddQuickGridEntityFrameworkAdapter();
    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityEmailSender>();
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
    }
    else {
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

    // Apply tenant theme per-request (skip if unchanged within the request's scope)
    app.Use(async (ctx, next) => {
        var themeSvc = ctx.RequestServices.GetRequiredService<ThemeService>();
        themeSvc.ApplyTenantBrandingIfAvailableAndChanged();
        await next();
    });

    app.UseAntiforgery();

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
    }

    // Map endpoints
    app.MapStaticAssets();
    app.MapHub<KanbanHub>("/kanbanhub");
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    app.MapAdditionalIdentityEndpoints();

    // Health checks
    app.MapHealthChecks("/health");
}

async Task SeedDatabase(WebApplication app) {
    // Seed roles for all environments
    using (var scope = app.Services.CreateScope()) {
        try {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Database.Migrate();
            var services = scope.ServiceProvider;
            await SeedManager.SeedRolesAsync(services);

            // Environment-specific user seeding
            if (app.Environment.IsDevelopment()) {
                await SeedManager.SeedTestUsersAsync(services);
            }
            await SeedManager.SeedAdminUserAsync(services);

        }
        catch (Exception ex) {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
}
