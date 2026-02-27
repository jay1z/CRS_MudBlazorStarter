Goal:
Implement a clean role-based authorization system that supports:
- Platform-level roles (for the SaaS owner)
- Tenant-level roles (for each customer company)
- External HOA/customer roles (to view final reports)

Please do NOT remove existing working auth/login logic, but extend it.

### Role Model

Implement these roles:

Platform-level (global, not tied to a tenant):
- PlatformAdmin
- PlatformSupport (optional, limited global read/support)

Tenant-level (per tenant):
- TenantOwner (or TenantAdmin)
- TenantSpecialist
- TenantViewer (read-only internal staff)

External (per HOA/customer account within a tenant):
- HOAUser
- HOAAuditor (optional, mostly read-only)

Assumptions:
- The app already has an ApplicationUser entity (ASP.NET Core Identity or equivalent).
- There is a Tenant entity with TenantId used in the data model.
- We want to support the same person having different roles in different tenants.

### Data Model Changes

1. Add a Role entity (if not already present) with at least:
   - RoleId
   - Name (string)
   - Scope (enum or string: "Platform", "Tenant", "External")

2. Add a UserRoleAssignment entity to support many-to-many between ApplicationUser and Role:
   - UserRoleAssignmentId
   - UserId (FK to ApplicationUser)
   - RoleId (FK to Role)
   - TenantId (nullable FK to Tenant, null for platform-level roles)

3. Ensure Tenant is already modeled with:
   - TenantId
   - Name
   - Subdomain
   - SubscriptionTierId (or similar)

4. Set up EF Core relationships:
   - ApplicationUser has many UserRoleAssignments
   - Role has many UserRoleAssignments
   - Tenant has many UserRoleAssignments (tenant-scoped roles)

### Seeding and Setup

1. Seed the Role table with the roles listed above.
2. Seed at least one PlatformAdmin user (my account) and assign:
   - A platform-level UserRoleAssignment with Role = PlatformAdmin and TenantId = null.

### Claims and Authorization

1. On successful login, determine the "current tenant" based on:
   - Subdomain routing

2. Once the current tenant is known, load all UserRoleAssignments for the current user where:
   - TenantId = current tenant OR TenantId = null (platform-level).
3. For each role, emit claims like:
   - "role" = Role.Name
   - "tenant" = TenantId (for tenant-scoped roles)
   Optionally, use a custom claim type like "alx_role" or "alx_tenant".

4. Configure ASP.NET Core policy-based authorization in Program.cs / Startup:
   - Policies like "RequirePlatformAdmin", "RequireTenantOwner", "RequireTenantSpecialist", etc.
   - Each policy should check both the role claim and, for tenant roles, that the tenant claim matches the current tenant.

Example policies (conceptual, you generate the actual C#):
- RequirePlatformAdmin: must have role PlatformAdmin.
- RequireTenantOwner: must have role TenantOwner for the current TenantId.
- RequireTenantStaff: role TenantOwner or TenantSpecialist for the current TenantId.
- RequireTenantViewer: TenantOwner, TenantSpecialist, or TenantViewer for the current TenantId.
- RequireHOAUser: role HOAUser for the current TenantId (and associated customer account).

### Blazor Integration

1. Add [Authorize] attributes to pages and components:
   - Platform admin pages: [Authorize(Policy = "RequirePlatformAdmin")]
   - Tenant configuration pages: [Authorize(Policy = "RequireTenantOwner")]
   - Reserve study CRUD pages: [Authorize(Policy = "RequireTenantStaff")]
   - Report publishing pages: [Authorize(Policy = "RequireTenantOwner")]
   - Read-only internal dashboards: [Authorize(Policy = "RequireTenantViewer")]
   - Customer/HOA portal pages: [Authorize(Policy = "RequireHOAUser")]

2. Use <AuthorizeView> in components to conditionally show/hide buttons:
   - Show "Publish report" only for TenantOwner.
   - Show "Edit study" for TenantStaff.
   - Show "Download final report" for HOAUser and HOAAuditor.

3. Ensure the current tenant context is available in the Blazor DI container:
   - Create a ITenantContext service that exposes CurrentTenantId and CurrentTenant.
   - Use it both when building claims and when applying tenant filters.

4. Add any additional pages or functionality necessary to support these additional roles.
   - For example, a "Manage Users" page for TenantOwner to assign roles within their tenant.
   - Add NavMenu links where necessary, conditionally shown based on roles.

### Tenant Isolation

1. For all tenant-scoped queries (properties, reserve studies, reports), enforce:
   - Filtering by CurrentTenantId at the repository/db context level.
   - Also ensure authorization checks run before returning sensitive data.

2. PlatformAdmin can optionally bypass tenant filters when using global admin screens.

### UX and Safety

1. If a user tries to access a page without required role for the current tenant, return 403 and a friendly message.
2. If the user has roles in multiple tenants, only show for the current tenant.
3. Add debugging logs to show what roles/claims were loaded at login.

Please:

- Generate the EF Core model updates, configuration, and migrations.
- Configure the authorization policies.
- Add example usages in a few Blazor pages using [Authorize] and <AuthorizeView>.
- Do not break existing login or multi-tenant selection code; extend it instead.
- Keep code organized, adding folders like "Authorization", "Tenants", and "Security" if needed.
