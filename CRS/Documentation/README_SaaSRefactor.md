Prompt:

I’m refactoring this Blazor Server application into a multi-tenant SaaS platform.
The project is currently structured for single-tenant, self-hosted use, but I need to reorganize the solution, introduce tenant isolation, and prepare for cloud deployment.

Please carefully review the existing structure and propose or apply changes that align with the following requirements.

🎯 Goal

Transform the existing Blazor Server project into a SaaS-ready architecture that supports:

Multiple tenants (companies) sharing a single deployment

Tenant-aware authentication, authorization, and data access

Cloud file storage with per-tenant organization

Clean folder structure for scalability and maintainability

Centralized updates (one app instance serving all customers)

🧩 Architecture Requirements

Project Type: Blazor Server
Framework: .NET 8 or newer
Database: SQL Server (Entity Framework Core)
Hosting Target: Azure App Service or equivalent

🗂️ Refactor Plan
1️⃣ Folder & Project Structure

Create or reorganize folders as follows:

/ReserveStudies.Web/               # Main Blazor Server app
├── /Pages/                        # Razor pages and components
├── /Components/                   # Reusable components
├── /Areas/                        # Role or tenant-specific UI areas
│    ├── /Client/                  # Client-facing pages
│    ├── /Admin/                   # Admin and internal pages
├── /Services/                     # Business and application services
│    ├── Tenant/
│    │   ├── ITenantContext.cs
│    │   ├── TenantContext.cs
│    │   ├── TenantResolverMiddleware.cs
│    │   └── TenantService.cs
│    ├── File/
│    │   └── FileStorageService.cs
│    ├── License/
│    │   └── LicenseValidationService.cs
├── /Data/
│    ├── AppDbContext.cs
│    ├── Entities/
│    │   ├── Tenant.cs
│    │   ├── User.cs
│    │   ├── Client.cs
│    │   ├── Report.cs
│    └── Migrations/
├── /Middleware/
│    └── TenantResolverMiddleware.cs
├── /Shared/
│    ├── Layouts/
│    │   ├── _TenantLayout.razor
│    │   └── _AdminLayout.razor
│    └── Components/
│        └── BrandingBanner.razor
├── /wwwroot/
│    └── /tenant-assets/
├── appsettings.json
└── Program.cs


Move or rename files where appropriate to match this structure.
Preserve namespaces and fix imports automatically.

2️⃣ Multi-Tenant Foundation

Add a new Tenant entity with:

public class Tenant {
    public int Id { get; set; }
    public string Name { get; set; }
    public string Subdomain { get; set; }
    public bool IsActive { get; set; }
    public string BrandingJson { get; set; }
    public DateTime CreatedAt { get; set; }
}


Add a TenantId column to all tenant-specific entities (Client, Report, etc.).

Create a new ITenantContext service that stores TenantId, TenantName, and other metadata.

Implement a TenantResolverMiddleware that:

Extracts the tenant name from the request host (e.g., acme.reserveapp.com → acme).

Looks up the Tenant record from the DB or cache.

Populates the TenantContext for the request lifetime.

Register it early in the middleware pipeline:

app.UseMiddleware<TenantResolverMiddleware>();

3️⃣ Data Access

Update AppDbContext to include:

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.Entity<Client>().HasQueryFilter(c => c.TenantId == _tenantContext.TenantId);
    modelBuilder.Entity<Report>().HasQueryFilter(r => r.TenantId == _tenantContext.TenantId);
}


Inject ITenantContext into the DbContext (constructor-based injection).

Ensure all new records automatically assign the current TenantId before saving.

4️⃣ Authentication and Identity

Extend your existing ApplicationUser model to include TenantId.

During login, verify that the user’s TenantId matches the tenant resolved by middleware.

Include the TenantId in the user’s claims.

Update your [Authorize] logic to consider role and tenant.

5️⃣ File & Storage Updates

Refactor all file upload/download logic to save under tenant-specific directories:

/tenant-{TenantId}/uploads/
/tenant-{TenantId}/reports/


If local file storage is used, keep the same convention under wwwroot/tenant-assets/.

If Azure Blob or AWS S3 is configured, prefix paths with tenant identifiers automatically.

6️⃣ Licensing & Subscription Validation

Add a background hosted service or a simple check that:

Periodically validates Tenant.IsActive or calls an external license API.

Denies access to inactive tenants by redirecting them to a “License Expired” page.

7️⃣ Branding

Allow each tenant to define their own logo, color scheme, and title in BrandingJson.

Add _TenantLayout.razor that dynamically applies colors and logos based on current tenant context.

8️⃣ Configuration and Startup

Configure wildcard hosting:

builder.WebHost.UseUrls("https://*.reserveapp.com");


Ensure wildcard SSL certificate supports all subdomains.

Add TenantResolverMiddleware before authentication middleware.

9️⃣ Copilot Guidelines

Always preserve existing logic and comments.

When reorganizing, automatically update namespaces.

Add clear comments where new SaaS logic is inserted:

// SaaS Refactor: Added tenant-aware query filter


When creating new folders or moving files, update imports and fix broken references automatically.

Suggest follow-up steps to migrate existing data into the new multi-tenant structure (e.g., assign default TenantId = 1 for legacy records).

🧱 Final Output Goal

After applying these changes, I should have:

A Blazor Server app capable of serving multiple tenants from one deployment

Tenant-aware EF Core filters, authentication, and storage

A clean, modular folder structure ready for continuous deployment to Azure

Minimal disruption to existing business logic and UI components

Important:
Don’t rewrite everything at once.
Suggest stepwise refactors and explain each move (e.g., “Moving AppDbContext to /Data for clarity” or “Adding TenantId to Client entity with migration”).
After each major change, suggest a migration command and any manual adjustments needed.