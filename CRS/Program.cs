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
using CRS.Services.NarrativeReport; // narrative template services

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Components.Server.Circuits; // add for CircuitHandler

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

// Configure Coravel scheduler for background jobs
app.Services.UseScheduler(scheduler =>
{
    // Run tenant lifecycle job daily at 2 AM
    scheduler
        .Schedule<CRS.Jobs.TenantLifecycleJob>()
        .DailyAtHour(2)
        .PreventOverlapping(nameof(CRS.Jobs.TenantLifecycleJob));

    // Run late interest calculation daily at 3 AM
    scheduler
        .Schedule<CRS.Jobs.LateInterestInvocable>()
        .DailyAtHour(3)
        .PreventOverlapping(nameof(CRS.Jobs.LateInterestInvocable));
        
    // Run auto-archive job daily at 4 AM
    scheduler
        .Schedule<CRS.Jobs.AutoArchiveInvocable>()
        .DailyAtHour(4)
        .PreventOverlapping(nameof(CRS.Jobs.AutoArchiveInvocable));

    // Run automated invoice reminders daily at 8 AM
    scheduler
        .Schedule<CRS.Jobs.InvoiceReminderInvocable>()
        .DailyAtHour(8)
        .PreventOverlapping(nameof(CRS.Jobs.InvoiceReminderInvocable));
});

var provider = app.Services;
IEventRegistration registration = provider.ConfigureEvents();
registration.Register<ReserveStudyCreatedEvent>().Subscribe<ReserveStudyCreatedListener>();
registration.Register<ProposalSentEvent>().Subscribe<ProposalSentListener>();
registration.Register<ProposalApprovedEvent>().Subscribe<ProposalApprovedListener>();
registration.Register<FinancialInfoRequestedEvent>().Subscribe<FinancialInfoRequestedListener>();
registration.Register<FinancialInfoSubmittedEvent>().Subscribe<FinancialInfoSubmittedListener>();
registration.Register<ReserveStudyCompletedEvent>().Subscribe<ReserveStudyCompletedListener>();
registration.Register<SiteVisitScheduledEvent>().Subscribe<SiteVisitScheduledListener>();

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

    // Register tenant user role resolution service
    builder.Services.AddScoped<ITenantUserRoleService, TenantUserRoleService>();
    // Service for tenant owners to create/manage tenant employees
    builder.Services.AddScoped<ITenantUserService, TenantUserService>();
    builder.Services.AddScoped<ITenantHomepageDataService, TenantHomepageDataService>();
    builder.Services.AddScoped<CRS.Services.Tickets.ITicketService, CRS.Services.Tickets.TicketService>();
    builder.Services.AddScoped<CRS.Services.Customers.ICustomerService, CRS.Services.Customers.CustomerService>();

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
    builder.Services.AddScoped<IElementService, ElementService>();
    builder.Services.AddScoped<IReserveStudyService, ReserveStudyService>();
    builder.Services.AddScoped<IReserveStudyWorkflowService, ReserveStudyWorkflowService>();
    builder.Services.AddScoped<ISignalRService, SignalRService>();
    builder.Services.AddScoped<IKanbanService, KanbanService>(); // Must go after SignalRService

    // Register workflow engine + notifications (new)
    builder.Services.AddScoped<CRS.Services.Workflow.INotificationService, CRS.Services.Workflow.NotificationService>();
    builder.Services.AddScoped<IStudyWorkflowService, CRS.Services.Workflow.StudyWorkflowService>();
    builder.Services.AddScoped<CRS.Services.Workflow.IWorkflowActionService, CRS.Services.Workflow.WorkflowActionService>();
    builder.Services.Configure<CRS.Services.Workflow.WorkflowOptions>(builder.Configuration.GetSection("Workflow"));

    // SaaS Refactor: register tenant services
    builder.Services.AddScoped<ITenantContext, TenantContext>();
    builder.Services.AddScoped<TenantService>();
    builder.Services.AddScoped<CircuitHandler, CRS.Services.Tenant.TenantCircuitHandler>();

    // Layout service for controlling layout state (hide nav, etc.)
    builder.Services.AddScoped<CRS.Services.Layout.ILayoutService, CRS.Services.Layout.LayoutService>();

    // Remove Multi-tenant provisioning services and multi-DB resolvers
    // builder.Services.AddSingleton<ITenantDatabaseResolver, DefaultTenantDatabaseResolver>();
    // builder.Services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
    // builder.Services.AddScoped<ITenantMigrationService, TenantMigrationService>();
    // builder.Services.AddSingleton<ITenantProvisioningQueue, InMemoryTenantProvisioningQueue>
    // builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
    // builder.Services.AddHostedService<CRS.Workers.TenantProvisioningWorker>();

    // User management and notification services
    builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
    // Messaging
    builder.Services.AddScoped<CRS.Services.Interfaces.IMessageService, CRS.Services.MessageService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IAppNotificationService, CRS.Services.AppNotificationService>();
    
    // High Priority Schema Services
    builder.Services.AddScoped<CRS.Services.Interfaces.IDocumentService, CRS.Services.DocumentService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IEmailLogService, CRS.Services.EmailLogService>();
    
    // Proposal PDF Generation
    builder.Services.AddScoped<CRS.Services.Interfaces.IProposalPdfService, CRS.Services.ProposalPdfService>();
    
    // Reserve Study Calculator Services
    builder.Services.AddScoped<CRS.Services.ReserveCalculator.IReserveStudyCalculatorService, CRS.Services.ReserveCalculator.ReserveStudyCalculatorService>();
    builder.Services.AddScoped<CRS.Services.ReserveCalculator.IReserveStudyPdfService, CRS.Services.ReserveCalculator.ReserveStudyPdfService>();
    builder.Services.AddScoped<CRS.Services.ReserveCalculator.IReserveStudyExcelService, CRS.Services.ReserveCalculator.ReserveStudyExcelService>();
    
    // Medium Priority Schema Services
    builder.Services.AddScoped<CRS.Services.Interfaces.ISiteVisitPhotoService, CRS.Services.SiteVisitPhotoService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IStudyNoteService, CRS.Services.StudyNoteService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IGeneratedReportService, CRS.Services.GeneratedReportService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IReportGenerationService, CRS.Services.ReportGenerationService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.INarrativeService, CRS.Services.NarrativeService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.INarrativeStateService, CRS.Services.NarrativeStateService>();

    // Narrative HTML Template Services
    builder.Services.AddScoped<ITokenRenderer, DefaultTokenRenderer>();
    builder.Services.AddScoped<IPlaceholderResolver, PlaceholderResolver>();
    builder.Services.AddScoped<CRS.Services.NarrativeReport.INarrativeHtmlSanitizer, CRS.Services.NarrativeReport.NarrativeHtmlSanitizer>();
    builder.Services.AddScoped<IReserveStudyHtmlComposer, ReserveStudyHtmlComposer>();
    builder.Services.AddScoped<INarrativeTemplateService, NarrativeTemplateService>();
    builder.Services.AddScoped<IReportContextBuilder, ReportContextBuilder>();
    builder.Services.AddSingleton<IPdfConverter, PuppeteerPdfConverter>(); // PuppeteerSharp HTML to PDF converter

    // Scope Comparison Service (for tracking scope variance after site visits)
    builder.Services.AddScoped<CRS.Services.Interfaces.IScopeComparisonService, CRS.Services.Workflow.ScopeComparisonService>();

    // Click-Wrap Agreement Services
    builder.Services.AddScoped<CRS.Services.Interfaces.IProposalAcceptanceService, CRS.Services.ProposalAcceptanceService>();

    // Invoicing Services
    builder.Services.AddScoped<CRS.Services.Interfaces.IInvoiceService, CRS.Services.InvoiceService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IInvoicePdfService, CRS.Services.InvoicePdfService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.IInvoicePaymentService, CRS.Services.InvoicePaymentService>();
    builder.Services.AddScoped<CRS.Services.Interfaces.ICreditMemoService, CRS.Services.CreditMemoService>();
    builder.Services.AddScoped<CRS.Services.ICreditMemoPdfService, CRS.Services.CreditMemoPdfService>();

    // System-wide settings
    builder.Services.AddScoped<CRS.Services.Interfaces.ISystemSettingsService, CRS.Services.SystemSettingsService>();

    // Newsletter/Marketing Services
    builder.Services.AddScoped<CRS.Services.Interfaces.INewsletterService, CRS.Services.NewsletterService>();

    // Report ZIP Service
    builder.Services.AddScoped<CRS.Services.Interfaces.IReportZipService, CRS.Services.ReportZipService>();

    // Tenant Archive Service (for cold storage before deletion)
    builder.Services.AddScoped<CRS.Services.Interfaces.ITenantArchiveService, CRS.Services.TenantArchiveService>();

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
    builder.Services.AddEmailLogging(); // Wrap IMailer with logging
    builder.Services.AddScheduler(); // Add Coravel scheduler for background jobs
    builder.Services.AddScoped<ReserveStudyCreatedListener>();
    builder.Services.AddScoped<ProposalSentListener>();
    builder.Services.AddScoped<ProposalApprovedListener>();
    builder.Services.AddScoped<FinancialInfoRequestedListener>();
    builder.Services.AddScoped<FinancialInfoSubmittedListener>();
    builder.Services.AddScoped<ReserveStudyCompletedListener>();
    builder.Services.AddScoped<SiteVisitScheduledListener>();
    builder.Services.AddEvents();
    
    // Register lifecycle job
    builder.Services.AddScoped<CRS.Jobs.TenantLifecycleJob>();

    // Register invoice jobs
    builder.Services.AddScoped<CRS.Jobs.LateInterestInvocable>();
    builder.Services.AddScoped<CRS.Jobs.InvoiceReminderInvocable>();
    
    // Register auto-archive job
    builder.Services.AddScoped<CRS.Jobs.AutoArchiveInvocable>();

    // Phase 1: Register audit log archiving background service
    builder.Services.AddHostedService<AuditLogArchiveService>();

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
    builder.Services.AddScoped<IProposalPdfService, ProposalPdfService>();

    // CORS for subdomains (SignalR/JS interop scenarios)
    builder.Services.AddCors(o => {
        o.AddPolicy("AllowSubdomains", policy => {
            var root = builder.Configuration["App:RootDomain"]; // e.g., localhost:7056 or example.com
            policy.SetIsOriginAllowed(origin => {
                if (string.IsNullOrEmpty(root)) return true; // allow all if not configured
                try {
                    var u = new Uri(origin);
                    // Extract root domain without port for comparison (Uri.Host excludes port)
                    var rootHost = root.Contains(':') ? root.Split(':')[0] : root;
                    var h = u.Host;
                    return h.Equals(rootHost, StringComparison.OrdinalIgnoreCase) || h.EndsWith("." + rootHost, StringComparison.OrdinalIgnoreCase);
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
    
    builder.Services.AddScoped<CRS.Services.Provisioning.IOwnerProvisioningService, CRS.Services.Provisioning.OwnerProvisioningService>();

    // Azure Blob Storage for logo uploads
        var azureStorageConnectionString = builder.Configuration.GetConnectionString("AzureStorage");
        if (!string.IsNullOrWhiteSpace(azureStorageConnectionString)) {
            builder.Services.AddSingleton(x => new Azure.Storage.Blobs.BlobServiceClient(azureStorageConnectionString));
            builder.Services.AddScoped<CRS.Services.Storage.ILogoStorageService, CRS.Services.Storage.LogoStorageService>();
            builder.Services.AddScoped<CRS.Services.Storage.IPhotoStorageService, CRS.Services.Storage.PhotoStorageService>();
            builder.Services.AddScoped<CRS.Services.Storage.IDocumentStorageService, CRS.Services.Storage.DocumentStorageService>();
        } else {
            // Fallback to null service if no connection string (development)
            builder.Services.AddScoped<CRS.Services.Storage.ILogoStorageService, CRS.Services.Storage.NullLogoStorageService>();
            builder.Services.AddScoped<CRS.Services.Storage.IPhotoStorageService, CRS.Services.Storage.NullPhotoStorageService>();
            builder.Services.AddScoped<CRS.Services.Storage.IDocumentStorageService, CRS.Services.Storage.NullDocumentStorageService>();
        }
    }

void ConfigureDatabases(WebApplicationBuilder builder) {
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
        throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

    // Register scoped DbContext for consumers like Identity stores, RoleManager/UserManager, and seeding logic
    builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    });

    // Register the factory as singleton - it will resolve scoped dependencies per-call
    builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>>(sp =>
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure())
            .Options;
        
        // Use IServiceScopeFactory to create scopes for resolving scoped dependencies
        return new ScopedDbContextFactory(options, sp.GetRequiredService<IServiceScopeFactory>());
    });
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

        // User settings
        options.User.RequireUniqueEmail = true; // Keep unique emails
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
    builder.Services.AddScoped<IUserStore<ApplicationUser>, CustomUserStore>();

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
        // Dev helper: dump current principal claims for troubleshooting
        app.MapGet("/dev/whoami", (System.Security.Claims.ClaimsPrincipal user) => {
            var claims = user?.Claims.Select(c => new { c.Type, c.Value }) ?? Enumerable.Empty<object>();
            return Results.Json(claims);
        }).WithDisplayName("Dev: WhoAmI (claims)").RequireAuthorization();

        app.UseDeveloperExceptionPage();
    } else {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseMigrationsEndPoint();
    app.UseHttpsRedirection();

    // Security headers
    app.UseSecurityHeaders();

    // Coming Soon mode (if enabled)
    app.UseComingSoon();

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

    // Dev helper endpoints
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
            
            // Dev helper: assign scoped role (tenant or platform)
            app.MapGet("/dev/assign-role", async (
                string email,
                string roleName,
                int? tenantId,
                UserManager<ApplicationUser> userManager,
                IDbContextFactory<ApplicationDbContext> dbFactory) => {
                
                if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest("email is required");
                if (string.IsNullOrWhiteSpace(roleName)) return Results.BadRequest("roleName is required");
                
                var user = await userManager.FindByEmailAsync(email);
                if (user == null) return Results.NotFound($"User '{email}' not found.");
                
                await using var db = await dbFactory.CreateDbContextAsync();
                
                // Get or create the role
                var role = await db.Roles2.FirstOrDefaultAsync(r => r.Name == roleName);
                if (role == null) {
                    role = new CRS.Models.Security.Role {
                        Name = roleName,
                        Scope = tenantId.HasValue ? CRS.Models.Security.RoleScope.Tenant : CRS.Models.Security.RoleScope.Platform
                    };
                    db.Roles2.Add(role);
                    await db.SaveChangesAsync();
                }
                
                // Check if assignment already exists
                var exists = await db.UserRoleAssignments.AnyAsync(a =>
                    a.UserId == user.Id &&
                    a.RoleId == role.Id &&
                    a.TenantId == tenantId);
                
                if (exists) {
                    return Results.Ok(new { message = $"User '{email}' already has role '{roleName}' for tenant {tenantId?.ToString() ?? "platform"}" });
                }
                
                // Create assignment
                db.UserRoleAssignments.Add(new CRS.Models.Security.UserRoleAssignment {
                    UserId = user.Id,
                    RoleId = role.Id,
                    TenantId = tenantId
                });
                await db.SaveChangesAsync();
                
                return Results.Ok(new {
                    message = $"Assigned role '{roleName}' to user '{email}' for tenant {tenantId?.ToString() ?? "platform"}",
                    userId = user.Id,
                    roleId = role.Id,
                    tenantId = tenantId
                });
            }).WithDisplayName("Dev: Assign scoped role to user");
            
            // Dev helper: fix users with null passwords by generating temporary password
            app.MapGet("/dev/fix-null-password", async (string email, UserManager<ApplicationUser> userManager, ApplicationDbContext db) => {
            if (string.IsNullOrWhiteSpace(email)) return Results.BadRequest("email is required");
            var user = await userManager.FindByEmailAsync(email);
            if (user == null) return Results.NotFound($"User '{email}' not found.");
            
            // Check if user has a password
            var hasPassword = await userManager.HasPasswordAsync(user);
            if (hasPassword) {
                return Results.Ok(new { message = $"User '{email}' already has a password. No action needed." });
            }
            
            // Use fixed password for testing in Development
            var tempPassword = "Letmeinnow1_";
            
            // Add password to user
            var result = await userManager.AddPasswordAsync(user, tempPassword);
            if (!result.Succeeded) {
                return Results.BadRequest(new { error = "Failed to add password", errors = result.Errors.Select(e => e.Description) });
            }
            
            return Results.Ok(new { 
                message = $"Password added for user '{email}'",
                temporaryPassword = tempPassword,
                warning = "Please share this password securely with the user and ask them to change it immediately."
            });
        }).WithDisplayName("Dev: Fix user with null password");

        // Example workflow transition + notification trigger
        app.MapGet("/dev/workflow/example", async (IStudyWorkflowService engine) => {
            var req = new StudyRequest { TenantId =1, CommunityId = Guid.CreateVersion7() };
            var ok = await engine.TryTransitionAsync(req, StudyStatus.ProposalCreated, "dev");
            return Results.Json(new { ok, from = StudyStatus.RequestCreated.ToString(), to = StudyStatus.ProposalCreated.ToString(), stateChangedAt = req.StateChangedAt });
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

    var configuredRootDomain = builder.Configuration["App:RootDomain"];
    if (!string.IsNullOrWhiteSpace(configuredRootDomain)) {
        var sanitized = configuredRootDomain.Trim().Trim('.').Replace("http://", "", StringComparison.OrdinalIgnoreCase).Replace("https://", "", StringComparison.OrdinalIgnoreCase);
        if (configuredRootDomain != sanitized) {
            Serilog.Log.Warning("[Startup] App:RootDomain sanitized from '{Original}' to '{Sanitized}'", configuredRootDomain, sanitized);
        } else {
            Serilog.Log.Information("[Startup] App:RootDomain set to '{RootDomain}'", sanitized);
        }
    }
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
        //if (app.Environment.IsDevelopment()) {
        //    logger.LogInformation("[Seed] Seeding test users...");
        //    await SeedManager.SeedTestUsersAsync(services);
        //}
        logger.LogInformation("[Seed] Completed.");
    } catch (Exception ex) {
        logger.LogError(ex, "Error during database seeding");
    }
}

// DTOs
public record SaveRequest(string Name, string Html, string? ComponentsJson);
public record TenantFindRequest(string Email);

// Custom factory that creates DbContext with scoped dependencies
class ScopedDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly IServiceScopeFactory _scopeFactory;
    
    public ScopedDbContextFactory(DbContextOptions<ApplicationDbContext> options, IServiceScopeFactory scopeFactory)
    {
        _options = options;
        _scopeFactory = scopeFactory;
    }
    
    public ApplicationDbContext CreateDbContext()
    {
        // Create a scope to properly resolve scoped services
        // This ensures ITenantContext and IHttpContextAccessor are resolved from the correct scope
        var scope = _scopeFactory.CreateScope();
        var httpContextAccessor = scope.ServiceProvider.GetService<IHttpContextAccessor>();
        var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();
        
        return new ApplicationDbContext(_options, httpContextAccessor, tenantContext);
    }
}
