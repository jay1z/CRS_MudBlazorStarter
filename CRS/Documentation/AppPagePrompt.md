You are Copilot assisting with a Blazor Server + MudBlazor SaaS application called “ALX Reserve Cloud”.

## High-Level Context

- Multitenant SaaS.
- Each **tenant is mapped to a subdomain**: e.g. `tenant1.alxreservecloud.com`.
- Each tenant has a **single app homepage** at `/app`.
- The same `/app` route is used for all tenants; behavior and visible content are **role-based**.
- Data model includes entities like: Tenant, Properties, ReserveStudies, Reports, CustomerAccounts, CustomerUsers, etc.
- Roles are:

  - `PlatformAdmin`
  - `PlatformSupport`
  - `TenantOwner` (or `TenantAdmin`)
  - `TenantSpecialist`
  - `TenantViewer`
  - `HOAUser` (external customer)
  - `HOAAuditor` (external compliance/audit role)

### Hard Constraints

- **No tenant picker on the homepage.**  
  The current tenant is determined purely by the subdomain. To switch tenants, the user logs out and logs back in via another subdomain.
- **No tenant impersonation** yet.  
  Do not design or implement impersonation tools/UI.

### General UX Rules for `/app`

1. `/app` is role-aware:
   - Same route and overall layout shell for all roles.
   - Actual **widgets, data, and actions** vary by the user's role within that tenant.
2. Always display:
   - Tenant branding (name + logo).
   - A clear indication of the **current role**.
3. Use **read-only vs read/write** semantics depending on role:
   - Viewer/auditor roles are read-only.
   - Specialist/owner/admin roles can perform actions.
4. Implement each visual block as a **widget/section component**, so we can reuse them and control visibility via role-based rules.

---

## Widget Matrix (By Role)

Design the homepage as a composition of the following widgets/sections.  
Implement each widget as a distinct, reusable component (e.g., MudBlazor cards, tables, chips, etc.). You can make recommendations on how to implement this.

**Columns:**

- Widget: Name of the widget.
- Description: What it shows/does.
- Role columns: `PA` = PlatformAdmin, `PS` = PlatformSupport, `TO` = TenantOwner, `TS` = TenantSpecialist, `TV` = TenantViewer, `HU` = HOAUser, `HA` = HOAAuditor.

Use ✓ to indicate where each widget appears.

| Widget ID | Widget Name                         | Description                                                                                                                                                                | PA | PS | TO | TS | TV | HU | HA |
|-----------|--------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----|----|----|----|----|----|----|
| W1        | HeroHeader                           | Tenant logo + name, short tagline, and “You are logged in as: [Role]”. No tenant picker, no impersonation controls.                                                       | ✓  | ✓  | ✓  | ✓  | ✓  | ✓  | ✓  |
| W2        | GlobalNotifications                  | High-level system messages: token errors, scheduled maintenance, important alerts for this tenant/account.                                                                | ✓  | ✓  | ✓  | ✓  | ✓  | ✓  | ✓  |
| W3        | TenantKPIsSummary                    | Counts: properties, active reserve studies (by status), final reports (recent period), active customer accounts (for this tenant).                                        | ✓  | ✓  | ✓  | (light) | ✓ |    |    |
| W4        | UsageAndBillingPanel                 | Subscription tier, status, usage vs limits (properties, storage, etc.), next billing date.                                                                                | ✓  | (read-only, optional) | ✓ |    |    |    |    |
| W5        | TenantPipelineBoard                  | Reserve studies by status (Draft / In Progress / In Review / Final / Archived). High-level view over the *whole tenant*.                                                  | ✓  | ✓  | ✓  | (filtered to assigned) | ✓ (read-only) |    |    |
| W6        | MyWorkQueue                          | List of reserve studies/tasks **assigned to the current user** (title, property, status, due date).                                                                        |    |    | (optional) | ✓  |    |    |    |
| W7        | PropertiesOverview                   | Summary table of properties with key info: last study date, next due, key flags.                                                                                          | ✓  | ✓  | ✓  | ✓  | ✓ (read-only) |    | ✓ (scoped where applicable) |
| W8        | CustomerAccountsOverview             | Internal-only view: list of CustomerAccounts with counts of properties, recent activity, etc.                                                                             | ✓  | ✓  | ✓  |    |    |    |    |
| W9        | ReportsAccessPanel                   | Access to final reports: cards or table with filters (by property, year, status) + download links.                                                                       | ✓  | ✓  | ✓  | ✓  | ✓ (read-only) | ✓  | ✓  |
| W10       | ActivityFeed                         | Chronological feed: “Study X moved from Draft → In Review”, “Report Y finalized”, “User Z invited”.                                                                       | ✓  | ✓  | ✓  | ✓  | ✓ (read-only) | (optional subset relevant to HU) | (optional subset) |
| W11       | SupportTicketsPanel                  | For internal platform roles: incoming support issues from this tenant; for tenant roles: open tickets with platform (if you choose to surface that).                      | ✓  | ✓  | (tenant → platform tickets) |    |    | (HU → tenant tickets) |    |
| W12       | ComplianceOverviewPanel              | For regulators/auditors: matrix of properties vs required study cycle with status (On-time / Approaching / Overdue).                                                      |    |    | ✓ (high-level) |    | ✓ (read-only) |    | ✓  |
| W13       | QuickActionsAdmin                    | Admin-only quick action buttons: manage users/roles, manage subscription, tenant-level settings.                                                                          | ✓  |    | ✓  |    |    |    |    |
| W14       | QuickActionsSpecialist               | Task-oriented quick actions: “Start New Study”, “Go to My Properties”, “Generate Draft Report”.                                                                           |    |    | (optional) | ✓  |    |    |    |
| W15       | HelpfulLinksAndDocs                  | Links to help docs, FAQs, contact info, support, and tenant-specific resources.                                                                                           | ✓  | ✓  | ✓  | ✓  | ✓  | ✓  | ✓  |
| W16       | HOAAccountSummary                    | For HOA users: their account name, properties they own, primary contact at the tenant, simple status of their studies/reports.                                            |    |    |    |    |    | ✓  |    |
| W17       | HOAMilestonesTimeline                | For HOA users: timeline of past and future reserve studies, upcoming due dates, and key milestones specific to their account.                                             |    |    |    |    |    | ✓  | ✓ (if scoped) |

Notes:
- “(light)” or “(optional)” means show a simplified/filtered version.
- `HU` and `HA` are always scoped to their allowed CustomerAccounts, not the entire tenant.

---

## Page Section Proposals by Role

Structure the `/app` homepage into **vertically stacked sections**, each composed of one or more widgets from above. Ordering is important for UX.

### 1. PlatformAdmin Homepage Layout

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`
   - `W2 GlobalNotifications`

2. **Tenant Health & Usage**
   - `W3 TenantKPIsSummary`
   - `W4 UsageAndBillingPanel`

3. **Pipeline & Activity**
   - `W5 TenantPipelineBoard`
   - `W10 ActivityFeed`

4. **Customers & Properties**
   - `W7 PropertiesOverview` (high-level)
   - `W8 CustomerAccountsOverview`

5. **Support & Admin Actions**
   - `W11 SupportTicketsPanel` (platform view of this tenant’s tickets)
   - `W13 QuickActionsAdmin`
   - `W15 HelpfulLinksAndDocs`

### 2. PlatformSupport Homepage Layout

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`
   - `W2 GlobalNotifications`

2. **Tenant Overview**
   - `W3 TenantKPIsSummary` (read-only)
   - `W5 TenantPipelineBoard` (read-only status board)

3. **Activity & Troubleshooting**
   - `W10 ActivityFeed`
   - `W11 SupportTicketsPanel` (support-focused ticket view)

4. **Context & Docs**
   - `W7 PropertiesOverview` (simplified)
   - `W15 HelpfulLinksAndDocs`

No billing management, no subscription editing, no impersonation.

### 3. TenantOwner / TenantAdmin Homepage Layout

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`
   - `W2 GlobalNotifications`

2. **Business KPIs**
   - `W3 TenantKPIsSummary`
   - `W5 TenantPipelineBoard` (full tenant view)

3. **Team & Customers**
   - `W7 PropertiesOverview` (key columns oriented for owner)
   - `W8 CustomerAccountsOverview`

4. **Compliance & Planning**
   - `W12 ComplianceOverviewPanel` (simplified view: upcoming/overdue studies across properties)

5. **Admin & Billing**
   - `W4 UsageAndBillingPanel`
   - `W13 QuickActionsAdmin`

6. **Support & Help**
   - `W11 SupportTicketsPanel` (tenant → platform tickets)
   - `W15 HelpfulLinksAndDocs`

### 4. TenantSpecialist Homepage Layout

This should be work-queue focused.

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`
   - `W2 GlobalNotifications`

2. **My Work**
   - `W6 MyWorkQueue` (primary focal widget, with emphasis)
   - `W14 QuickActionsSpecialist`

3. **Tenant Context (light)**
   - `W5 TenantPipelineBoard` (filtered view: highlight items assigned to current specialist, plus some overall context)
   - `W7 PropertiesOverview` (only key columns needed for navigation)

4. **Recent Activity**
   - `W10 ActivityFeed` (filtered to events relevant to this specialist, plus some tenant-wide items)

5. **Help**
   - `W15 HelpfulLinksAndDocs` (with emphasis on “How to perform a study”, etc.)

No billing, no user management.

### 5. TenantViewer Homepage Layout

Read-only internal role.

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`
   - `W2 GlobalNotifications`

2. **Portfolio View**
   - `W3 TenantKPIsSummary` (read-only)
   - `W7 PropertiesOverview` (read-only, more detailed table)

3. **Studies & Reports**
   - `W5 TenantPipelineBoard` (read-only)
   - `W9 ReportsAccessPanel` (view/download only)

4. **Activity & Docs**
   - `W10 ActivityFeed`
   - `W15 HelpfulLinksAndDocs`

No actions that mutate data (no new studies, no user management, no billing).

### 6. HOAUser Homepage Layout

External customer portal for a specific CustomerAccount.

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`  
     - Tenant branding.
     - Clearly shows they are an external HOA user and names their HOA account.

2. **Account Summary**
   - `W16 HOAAccountSummary`  
     - List of properties for this HOA.
     - Who to contact at the tenant (name, email, phone).

3. **Reports & Documents**
   - `W9 ReportsAccessPanel`  
     - Filtered to this HOA’s properties.  
     - Emphasize latest final report per property + archive.

4. **Milestones & Status**
   - `W17 HOAMilestonesTimeline`  
     - Past and future study dates, upcoming deadlines.

5. **Communication & Help**
   - A subset of `W11 SupportTicketsPanel` (if you support HOA → tenant messages/tickets)
   - `W15 HelpfulLinksAndDocs` (HOA-facing docs & support links only)

No internal tenant data, no billing controls, no access to other HOAs.

### 7. HOAAuditor Homepage Layout

External, but focused on compliance across their allowed scope.

Order of sections:

1. **Header Section**
   - `W1 HeroHeader`
   - `W2 GlobalNotifications`

2. **Compliance Overview**
   - `W12 ComplianceOverviewPanel`  
     - Matrix or summary: properties vs required studies, status (On Time / Approaching / Overdue).

3. **Reports & Evidence**
   - `W9 ReportsAccessPanel`  
     - Possibly with filters tailored to audit needs (by year, property, status).

4. **Properties Context (optional)**
   - `W7 PropertiesOverview`  
     - Only the columns auditors care about: last study date, next due, risk flags.

5. **Activity & Docs**
   - `W10 ActivityFeed` (subset: finalizations, approvals, major changes)
   - `W15 HelpfulLinksAndDocs` (policy docs, audit guidelines, etc.)

Read-only only. No ability to change data.

---

## Implementation Guidance for You (Copilot)

1. **Do not generate any tenant picker or impersonation UI.**
2. Implement the `/app` page as a single route that:
   - Resolves the current tenant from the subdomain.
   - Resolves the user’s **effective role** in that tenant.
   - Selects and composes the appropriate widgets per the matrix and layouts above.
3. Implement each widget (`W1`–`W17`) as a separate, reusable component or clearly separated section:
   - The components should receive only the data they need (via view models or parameters).
   - You may use MudBlazor components (cards, tables, chips, carousels, etc.).
4. Ensure that:
   - Viewer/auditor roles have **no write actions** rendered.
   - The layout degrades gracefully on smaller screens but keeps the same logical grouping.
5. Use clear naming (e.g., `TenantHomeHeroHeader`, `TenantHomeMyWorkQueueSection`, etc.) and keep role-based visibility logic easy to follow and test.

Use this specification to design and implement the role-aware `/app` homepage UX, strictly following the widget matrix and layout definitions above, while respecting the constraints of **no tenant picker** and **no impersonation**.
