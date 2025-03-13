using CRS.Components;
using CRS.Components.Account;
using CRS.Data;
using CRS.Helpers;
using CRS.Services;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using MudBlazor.Services;

using Serilog;

var builder = WebApplication.CreateBuilder(args);
//builder.Logging.ClearProviders();
//builder.Logging.AddConsole();
builder.Host.UseSerilog((context, loggerConfiguration) => {
    loggerConfiguration.WriteTo.Console();
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<UserStateService>();
// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
//builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddRazorComponents(options => options.DetailedErrors = builder.Environment.IsDevelopment()).AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<IPasswordHelper, PasswordHelper>();
//builder.Services.AddScoped<HashingService>();
builder.Services.AddSingleton<ThemeService>();
builder.Services.AddScoped<INavigationService, NavigationService>();

builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
}).AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContextFactory<ApplicationDbContext>(options => options.UseSqlServer(connectionString), ServiceLifetime.Scoped);
//ServerVersion serverVersion = ServerVersion.AutoDetect(connectionString);
//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseMySql(connectionString, serverVersion, null));

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<SignInManager<ApplicationUser>>()
    .AddDefaultTokenProviders();

// Section:UserManager Lifetime Begin
// This section is for UserManager Lifetime
var userManagerServiceDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(UserManager<ApplicationUser>));
if (userManagerServiceDescriptor != null) {
    builder.Services.Remove(userManagerServiceDescriptor);
}

var userStoreServiceDescriptor = builder.Services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IUserStore<ApplicationUser>));
if (userStoreServiceDescriptor != null) {
    builder.Services.Remove(userStoreServiceDescriptor);
}

//builder.Services.AddTransient<DbContext, ApplicationDbContext>();
//builder.Services.AddTransient<UserManager<ApplicationUser>>();
//builder.Services.AddTransient<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>>();

builder.Services.AddScoped<UserManager<ApplicationUser>>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>>();
// Section:UserManager Lifetime End

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();
// Seed roles
using (var scope = app.Services.CreateScope()) {
    var services = scope.ServiceProvider;
    await SeedManager.SeedRolesAsync(services);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();

    // Seed the admin user
    using (var scope = app.Services.CreateScope()) {
        var services = scope.ServiceProvider;
        await SeedManager.SeedAdminUserAsync(services);
        await SeedManager.SeedTestUsersAsync(services);
    }
}
else {
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseMigrationsEndPoint();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
