using CRS.Components;
using CRS.Components.Account;
using CRS.Data;
using CRS.Helpers;
using CRS.Models;
using CRS.Services;
using CRS.Services.Email;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
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

app.Run();

void ConfigureLogging(WebApplicationBuilder builder) {
    var dashboardConnectionString = builder.Configuration.GetConnectionString("DashboardConnection") ??
        throw new InvalidOperationException("Connection string 'DashboardConnection' not found.");

    builder.Host.UseSerilog((context, loggerConfiguration) => {
        loggerConfiguration
        .WriteTo.Console()
        .WriteTo.MSSqlServer(
            connectionString: dashboardConnectionString,
            sinkOptions: new MSSqlServerSinkOptions {
                TableName = "LogEvents",
                AutoCreateSqlTable = true,
                AutoCreateSqlDatabase = true
            })
            // Add these enrichers to provide more context
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithMachineName()
            .Enrich.WithProperty("Application", "CRS");
    });
}

void ConfigureServices(WebApplicationBuilder builder) {
    // Core services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<UserStateService>();
    builder.Services.AddMudServices();

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

    // Application services
    builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
    builder.Services.AddSingleton<ThemeService>();
    builder.Services.AddScoped<INavigationService, NavigationService>();
    builder.Services.AddScoped<IContactService, ContactService>();
    builder.Services.AddScoped<IReserveStudyService, ReserveStudyService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

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
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
}

void ConfigureDatabases(WebApplicationBuilder builder) {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    builder.Services.AddDbContextFactory<ApplicationDbContext>(
        options => options.UseSqlServer(connectionString),
        ServiceLifetime.Scoped);
}

void ConfigureIdentity(WebApplicationBuilder builder) {
    builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddSignInManager<SignInManager<ApplicationUser>>()
        .AddDefaultTokenProviders();

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

    // Register RoleManager
    builder.Services.AddScoped<RoleManager<IdentityRole>>(sp => {
        var roleStore = new RoleStore<IdentityRole>(sp.GetRequiredService<ApplicationDbContext>());
        var options = sp.GetRequiredService<IOptions<IdentityOptions>>();
        var logger = sp.GetRequiredService<ILogger<RoleManager<IdentityRole>>>();
        var keyNormalizer = sp.GetRequiredService<ILookupNormalizer>();
        var errors = sp.GetRequiredService<IdentityErrorDescriber>();
        var services = sp.GetRequiredService<IServiceProvider>();
        var validators = new List<IRoleValidator<IdentityRole>>();
        return new RoleManager<IdentityRole>(roleStore, validators, keyNormalizer, errors, logger);
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
    app.UseAntiforgery();

    // Map endpoints
    app.MapStaticAssets();
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    app.MapAdditionalIdentityEndpoints();
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
