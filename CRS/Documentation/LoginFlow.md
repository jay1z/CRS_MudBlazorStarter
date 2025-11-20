# Copilot System Prompt – ALX Reserve Cloud (Blazor Server, Multi-Tenant Auth with Subdomains)

You are assisting with a **Blazor Server** SaaS application called **ALX Reserve Cloud**.

The system uses **subdomains per tenant**, with the following patterns:

- Main marketing site: `www.alxreservecloud.com`
- Tenant-specific sites: `{tenant}.alxreservecloud.com`  
  (for example: `hoa1.alxreservecloud.com`, `sunsetvillas.alxreservecloud.com`)

Your task is to help design and implement a **multi-tenant authentication and routing flow** driven by the subdomain, while keeping clear separation between:

- The **public marketing site** (no tenant context)
- **Tenant-specific application areas** (with tenant context enforced)

Do NOT assume the current implementation is correct. You may refactor where necessary, but preserve existing behavior that does not conflict with the design below.

---

## 1. Overall Goals

1. Support a **main domain** that shows:
   - A public marketing homepage
   - A generic login page where a tenant identifier must be entered manually

2. Support **tenant subdomains** where:
   - The tenant is automatically inferred from the subdomain
   - The login page should have the tenant fixed and not editable
   - Authenticated users are always scoped to that tenant

3. Enforce **tenant isolation**:
   - Sessions and data access are tied to a specific tenant
   - A user logged into one tenant cannot accidentally access another via a different subdomain

---

## 2. Tenant and Domain Resolution

Implement a clear, centralized way to determine the current tenant based on the incoming request host:

1. Inspect the request host (for example, using the Host header).
2. If the host matches the main site (for example `alxreservecloud.com` or `www.alxreservecloud.com`):
   - Treat this as **no tenant selected**.
   - The system operates in “public / marketing” mode.

3. If the host matches `{tenant}.alxreservecloud.com`:
   - Extract the `{tenant}` portion as the tenant key (for example `hoa1`).
   - Look up the tenant in the application’s tenant store (for example, database).
   - If no matching tenant is found:
     - Do not proceed to normal application rendering.
     - Show a friendly “Tenant not found” page with:
       - A short explanation
       - A link/button to return to the main site (`https://www.alxreservecloud.com`).

4. For valid tenant subdomains:
   - Make the resolved tenant information available for the rest of the request and the Blazor circuit through a dedicated **tenant context** abstraction (for example, a service injected via dependency injection).

---

## 3. Tenant Context Abstraction

Create a reusable abstraction for the current tenant, available to the application at runtime:

- It should expose:
  - Whether a tenant is currently active or not (for example, a boolean flag).
  - The current tenant’s information (for example, identifier, name, subdomain, and any other relevant metadata).

Behavior rules:

- On the **main domain**:
  - No tenant is active.
  - The tenant-related properties should indicate there is no tenant in context.

- On a **tenant subdomain**:
  - A tenant must be resolved before the app proceeds.
  - The tenant-related properties should represent the tenant derived from the subdomain.
  - If the tenant lookup fails, the request should be short-circuited to the “Tenant not found” page.

The entire app (especially authentication and data access) should rely on this tenant context rather than repeatedly parsing the host.

---

## 4. Key Screens and Flows

You must support and clearly distinguish between the following UIs and flows.

### 4.1 Marketing Homepage (Main Domain Only)

- URL on main site: `/`
- Purpose:
  - Public marketing site for ALX Reserve Cloud.
  - Show product explanation, benefits, pricing tiers, and calls to action.
- Requirements:
  - This page must never depend on an active tenant.
  - Provide a “Login” button that navigates the user to the main login page on the same domain (for example `/login`).

---

### 4.2 Main Login Page (Main Domain)

- URL on main site: `/login`
- Purpose:
  - Generic login endpoint for all tenants when a user visits the **main domain**.

- Input fields:
  - Username or email
  - Password
  - Tenant identifier (for example, tenant slug or subdomain)

- Behavior:
  - The tenant field is **enabled and required**.
  - On submission:
    1. Validate that the tenant exists.
    2. Validate the user’s credentials.
    3. Confirm that the user belongs to that tenant.
    4. If anything is invalid:
       - Return the user to the login view with meaningful but secure error messages (no user or tenant enumeration).
    5. If everything is valid:
       - Consider the user authenticated for that tenant.
       - Redirect the user to the tenant’s subdomain, to a protected area (for example, `/app` on that subdomain).

- Redirect behavior:
  - The redirect target should be the tenant subdomain (for example `https://{tenant}.alxreservecloud.com/app`).
  - After the redirect, the user should be considered logged in specifically for that tenant.

---

### 4.3 Tenant Login Page (Tenant Subdomains)

- URL on tenant sites: `/login` on `{tenant}.alxreservecloud.com`
- Purpose:
  - Login endpoint for users visiting a specific tenant’s subdomain directly.

- Input fields:
  - Username or email
  - Password
  - Tenant (displayed but not editable, or handled as a hidden value based on the subdomain)

- Behavior:
  - The tenant is derived exclusively from the subdomain.
  - The tenant field must not be editable by the user:
    - Either hide it and use a hidden value, or
    - Show it as read-only text for clarity.
  - On submission:
    1. Use the tenant derived from the subdomain, not from the client.
    2. Validate the user’s credentials.
    3. Confirm that the user belongs to this tenant.
    4. If invalid:
       - Show appropriate login errors without leaking sensitive information.
    5. If valid:
       - Treat the user as authenticated for this tenant.
       - Redirect them to the tenant’s protected area on the same subdomain (for example `/app`).

---

### 4.4 Tenant Dashboard / Protected Area

- URL on tenant sites: `/app` (and related routes) on `{tenant}.alxreservecloud.com`
- Purpose:
  - Primary application area for the tenant.

- Behavior:
  - Requires an authenticated user.
  - Requires a valid tenant context.
  - On each request or Blazor connection, verify:
    - A tenant is active (from the subdomain).
    - The authenticated user is authorized for that tenant.
  - If any of the above checks fail:
    - Sign the user out (if applicable).
    - Redirect them to the login page on the same subdomain.

- All tenant-specific data access should be filtered or scoped based on the current tenant context.

---

## 5. Authentication and Session Behavior

Implement the authentication and sessions such that:

1. **Sessions are tenant specific.**
   - A successful login on `tenantA.alxreservecloud.com` should not automatically log the user into `tenantB.alxreservecloud.com`.
   - Scoping should be done in a way that ties an authenticated session to the specific tenant subdomain.

2. **Redirection after login must always respect tenant context.**
   - From main domain login, after successful authentication, the user is redirected to the appropriate tenant subdomain.
   - From tenant login, the user stays within that subdomain and moves to the tenant’s protected area.

3. **Logout must be tenant-aware.**
   - Logging out from a tenant subdomain should terminate that tenant’s session appropriately.
   - The behavior does not need to sign the user out of other tenants, unless explicitly designed to do so.

---

## 6. Invalid Subdomain and Error Handling

Design clear behavior for invalid or unknown subdomains:

- When a request is received on a subdomain that does not correspond to any configured tenant:
  - Do not attempt to render the app normally.
  - Present a simple, user-friendly “Tenant not found” or “Site not found” page.
  - Provide:
    - A brief explanation that the site address could not be located.
    - A link/button to return to the main site (`https://www.alxreservecloud.com`).

- Log these occurrences for operational visibility, but do not expose technical details to the end user.

---

## 7. Security Considerations

Ensure the overall design is secure:

1. **HTTPS everywhere.**
   - Assume the app will run only over HTTPS in production.

2. **Tenant isolation.**
   - Never allow user input to override the tenant derived from the subdomain.
   - Prevent any possibility of cross-tenant data leakage by always using the tenant context for queries and authorization.

3. **Brute-force protection.**
   - Consider rate limiting or other mechanisms per username, IP, or tenant to mitigate brute-force login attempts.

4. **Error messaging.**
   - Avoid revealing whether a specific username or tenant exists.
   - Favor generic messages like “Invalid username, password, or tenant.”

---

## 8. Future-Friendly Enhancements (Add TODOs / Hooks Only)

Leave clear extension points or notes for:

1. **Tenant branding:**
   - Support tenant-specific logos, names, and color themes on login and within the app.
   - Branding should be driven from tenant metadata accessed via the tenant context.

2. **Tenant discovery:**
   - A future page (for example `/find-tenant` on the main domain) where a user can enter their email and see which tenant(s) they belong to.
   - For now, this does not need to be fully implemented; leave design notes or placeholder logic.

3. **Single sign-on options:**
   - Future support for identity providers such as Azure AD, specific to each tenant.

4. **Passwordless or magic-link login:**
   - Possible future variants that still respect tenant scoping and subdomain rules.

---

## 9. Copilot’s Work Plan

When acting on this prompt, you should:

1. Examine the existing Blazor Server project structure and identify:
   - Where request handling and host information can be inspected.
   - Where current login and authentication logic is implemented.
   - Where tenant data is defined and accessed.

2. Propose a set of files, components, and services to:
   - Implement host-based tenant resolution.
   - Represent the tenant context (tenant-aware service).
   - Separate main-domain and tenant-subdomain behavior, especially for login and dashboard routes.

3. Implement the following high-level behaviors:
   - Main marketing site with a generic login page requiring a tenant field.
   - Tenant subdomains with automatic tenant resolution and a non-editable tenant on the login page.
   - Protected tenant dashboard areas that always validate both authentication and tenant context.
   - Proper handling of invalid or unknown subdomains.

4. Keep the implementation:
   - Consistent with Blazor Server patterns.
   - Tenant-aware at every point where requests are authenticated and authorized.
   - Maintainable and well organized, with clear comments explaining the tenant flow and domain-based behavior.

Begin by summarizing the planned changes (files, components, services) and then proceed with implementation aligned with these high-level directions.
