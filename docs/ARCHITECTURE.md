# Foresight — Architecture Plan

> **Capital intelligence platform that turns reserve data into forward visibility and actionable insight.**

---

## Product Suite Overview

**Foresight** is a multi-product SaaS platform serving community association reserve specialists and management companies. It comprises three applications built on a shared foundation:

| Product | Purpose | Domain |
|---|---|---|
| **Horizon** | Decision intelligence layer — reserve study authoring, funding strategy, scenario modeling, sustainability analysis, and board-ready narratives | Specialist / Consultant |
| **Pulse** | Operational awareness layer — continuous portfolio-level visibility, alerts, and readiness signals for management companies | Management Company |
| **Foresight Web** | Marketing website — product information, pricing, blog, and tenant signup/onboarding flow | Public / Prospects |

### Data Relationship

```
Horizon (writes) ──► Reserve Studies, Funding Plans, Narratives
                                    │
                                    ▼
Pulse  (reads)  ──► Portfolio dashboards, Alerts, Readiness signals
```

Horizon is the **authoring engine** — it creates reserve studies, funding plans, and narratives. Pulse is the **consumption layer** — it reads Horizon's output and provides management companies with portfolio-level operational awareness. Foresight Web is the **front door** — it handles marketing and routes signups into the platform.

---

## Solution Structure

```
Foresight.sln
│
├── Foresight.Shared/              # .NET 10 class library — shared foundation
│   ├── Models/                    # Domain entities (Tenant, ReserveStudy, Community, etc.)
│   ├── Data/                      # ApplicationDbContext, migrations, seed data
│   ├── Core/                      # Reserve study calculator engine, enums, result models
│   └── Services/                  # Shared interfaces (ITenantContext, ITenantScoped, etc.)
│
├── Horizon/                       # Blazor Server (MudBlazor) — authoring product
│   ├── Components/                # Blazor pages, layouts, account management
│   ├── Controllers/               # API endpoints (billing, workflows, uploads)
│   ├── Services/                  # Horizon-only services (narrative, PDF, billing, etc.)
│   ├── Hubs/                      # SignalR hubs
│   ├── Jobs/                      # Background jobs (Coravel)
│   └── Middleware/                # Tenant resolution, security headers, etc.
│
├── Pulse/                         # Blazor Server (MudBlazor) — operational awareness product
│   ├── Components/                # Blazor pages, layouts
│   ├── Services/                  # Pulse-only services (alerts, portfolio, readiness)
│   │   ├── Alerts/                # Threshold monitoring, notification engine
│   │   ├── Portfolio/             # Multi-community aggregation, trend analysis
│   │   └── Readiness/            # Readiness scoring, compliance tracking
│   ├── Hubs/                      # Real-time push for alert state changes
│   └── Models/                    # Pulse-specific entities (alerts, signals, snapshots)
│
├── Foresight.Web/                 # Blazor Web App (static SSR) — marketing + signup
│   ├── Components/                # Marketing pages, pricing, blog
│   └── Services/                  # Signup orchestration, content rendering
│
├── Horizon.Tests/                 # Unit/integration tests for Horizon
├── Pulse.Tests/                   # Unit/integration tests for Pulse
└── Foresight.Shared.Tests/        # Unit tests for shared core logic
```

---

## Foresight.Shared — Shared Foundation

The shared class library is the **single source of truth** for domain models, database schema, and core calculation logic. Both Horizon and Pulse reference it.

### What Lives in Foresight.Shared

| Category | Examples |
|---|---|
| **Domain Models** | `Tenant`, `ReserveStudy`, `Community`, `BuildingElement`, `Contact`, `FundingStatusLevel`, `BaseModel`, all enums |
| **Data Layer** | `ApplicationDbContext`, `ApplicationUser`, EF migrations, seed data |
| **Core Engine** | `Core/ReserveStudy/` — calculator, funding plan service, expenditure schedule, contribution strategies |
| **Tenant Contracts** | `ITenantContext`, `ITenantScoped`, `TenantClaimTypes` |
| **Shared Interfaces** | Read-only service contracts that both apps can implement |

### What Stays in Horizon (Not Shared)

| Category | Examples |
|---|---|
| **Narrative Engine** | Template rendering, HTML composition, PDF conversion |
| **Billing** | Stripe integration, subscription management, invoicing |
| **Workflow** | Study workflow orchestration, proposal management |
| **PDF/Reports** | QuestPDF reports, Excel exports, report zipping |
| **Communication** | Email sending, newsletter campaigns, mailbox service |
| **Background Jobs** | Coravel schedulers, trial expiration, invoice reminders |

---

## Pulse — Product Details

### Purpose

Pulse provides **management companies** with continuous portfolio-level visibility across all communities they manage. While Horizon focuses on creating individual reserve studies, Pulse focuses on the **aggregate operational picture**.

### Core Features (Planned)

| Feature | Description |
|---|---|
| **Portfolio Dashboard** | Aggregate view of all communities — funding status, upcoming expenditures, at-risk properties |
| **Alert Engine** | Configurable threshold-based alerts (e.g., funding drops below 50%, major expenditure within 2 years) |
| **Readiness Signals** | Compliance and audit-readiness indicators per community |
| **Trend Analysis** | Historical funding trajectory, contribution adequacy trends |
| **Board Digest** | Auto-generated executive summaries for board distribution |
| **Real-time Updates** | SignalR push notifications when study data changes in Horizon |

### Data Access Pattern

Pulse operates as a **read consumer** of Horizon data:

```
┌─────────────────────────────────────────────────────────┐
│                  Shared SQL Database                     │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Tenants     │  │   Studies    │  │ Communities  │  │
│  │ (shared)      │  │ (Horizon     │  │ (shared)     │  │
│  │               │  │  writes)     │  │              │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
│                                                         │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │   Alerts      │  │  Readiness   │  │  Portfolio   │  │
│  │ (Pulse        │  │  Signals     │  │  Snapshots   │  │
│  │  writes)      │  │ (Pulse       │  │ (Pulse       │  │
│  │               │  │  writes)     │  │  writes)     │  │
│  └──────────────┘  └──────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────┘
```

### Pulse-Specific Database Tables

These tables will be added to the shared database but are owned by Pulse:

- `PulseAlerts` — configurable alert definitions and triggered instances
- `PulseReadinessSignals` — per-community readiness/compliance scores
- `PulsePortfolioSnapshots` — point-in-time portfolio aggregations
- `PulseNotificationPreferences` — per-user alert delivery settings
- `PulseBoardDigests` — generated executive summaries

---

## Foresight.Web — Marketing & Signup

### Purpose

The public-facing website for the Foresight platform. Lightweight, fast, SEO-optimized.

### Architecture

| Aspect | Choice |
|---|---|
| **Template** | Blazor Web App (static SSR for fast page loads + SEO) |
| **UI Framework** | MudBlazor (consistent branding with Horizon/Pulse) or lightweight CSS |
| **Authentication** | None for public pages; minimal for signup flow |
| **Database Access** | Minimal — only for tenant provisioning during signup |

### Key Pages

- `/` — Hero, value proposition, product overview
- `/horizon` — Horizon product page
- `/pulse` — Pulse product page
- `/pricing` — Tier comparison and signup CTAs
- `/blog` — Content marketing (rendered with Markdig)
- `/signup` — Tenant creation → Stripe checkout → redirect to Horizon
- `/contact` — Contact form (uses existing `ContactSubmission` model)

### Signup Flow

```
Visitor ──► /pricing ──► Select Tier ──► /signup
                                            │
                                            ▼
                                    Create Tenant (Pending)
                                            │
                                            ▼
                                    Stripe Checkout Session
                                            │
                                            ▼
                                    Stripe Webhook confirms payment
                                            │
                                            ▼
                                    Activate Tenant + Provision Owner
                                            │
                                            ▼
                                    Redirect to horizon.foresight.app
```

---

## Authentication & Multi-Tenancy

### Shared Identity

All three products share the **same ASP.NET Identity database** via `Foresight.Shared`:

- `ApplicationUser` (extends `IdentityUser<Guid>`)
- Tenant-scoped claims (`TenantId`, `TenantRole`)
- Role assignments via `UserRoleAssignment`

### Per-Product Auth Configuration

| Product | Auth Mode | Notes |
|---|---|---|
| **Horizon** | Cookie auth, full Identity UI | Existing account management components |
| **Pulse** | Cookie auth, shared Identity | Same user accounts; Pulse-specific role checks |
| **Foresight.Web** | Minimal / none | Public pages; signup creates tenant but doesn't require login |

### Tenant Resolution

Both Horizon and Pulse use subdomain-based or claim-based tenant resolution via the shared `TenantResolverMiddleware` pattern.

---

## Database & Migrations Strategy

### Phase 1 — Single Migration Assembly (Current Plan)

All migrations live in `Foresight.Shared`. Both Horizon and Pulse reference the same context.

```
dotnet ef migrations add <Name> --project Foresight.Shared --startup-project Horizon
```

### Phase 2 — Separate Migration Assemblies (Future)

If Pulse's schema diverges significantly:
- `Foresight.Shared` — base schema (tenants, studies, communities)
- `Pulse` — Pulse-specific tables only
- Each app configures `MigrationsAssembly()` accordingly

---

## Deployment Topology

```
                        ┌──────────────────┐
                        │  Foresight.Web   │
                        │ foresight.app    │
                        │ (Azure App Svc   │
                        │  or Static Web)  │
                        └────────┬─────────┘
                                 │ signup → provisions Tenant
                                 ▼
    ┌─────────────────────────────────────────────────┐
    │              Azure SQL Database                  │
    │  Tenants │ Studies │ Communities │ Pulse tables  │
    └──────────┬──────────────────────┬───────────────┘
               │                      │
    ┌──────────▼──────────┐ ┌────────▼────────────┐
    │       Horizon        │ │        Pulse         │
    │  horizon.foresight   │ │  pulse.foresight     │
    │  .app                │ │  .app                │
    │  (Azure App Service) │ │  (Azure App Service) │
    │                      │ │                      │
    │  Writes:             │ │  Reads:              │
    │  - Reserve Studies   │ │  - Reserve Studies   │
    │  - Funding Plans     │ │  - Funding Plans     │
    │  - Narratives        │ │  Writes:             │
    │  - Invoices          │ │  - Alerts            │
    │                      │ │  - Readiness Signals │
    └──────────────────────┘ └──────────────────────┘
```

---

## Technology Stack

| Layer | Technology |
|---|---|
| **Runtime** | .NET 10 |
| **UI Framework** | Blazor Server + MudBlazor 9 |
| **ORM** | Entity Framework Core 10 |
| **Database** | Azure SQL Server |
| **Identity** | ASP.NET Core Identity |
| **Real-time** | SignalR |
| **Background Jobs** | Coravel |
| **PDF Generation** | QuestPDF + PuppeteerSharp |
| **Email** | Azure Communication Services + MailKit |
| **Payments** | Stripe (Connect + Billing) |
| **Logging** | Serilog (Console + SQL Server sink) |
| **Validation** | FluentValidation |
| **Storage** | Azure Blob Storage |

---

## Execution Plan — Phase 1

### Step 1: Rename Solution

Rename `CRS.sln` → `Foresight.sln`. Update any CI/CD references.

### Step 2: Create Foresight.Shared

1. Create `Foresight.Shared` class library project (net10.0)
2. Move shared models from `Horizon/Models/` → `Foresight.Shared/Models/`
3. Move `Data/ApplicationDbContext.cs`, `ApplicationUser.cs`, seed data
4. Move `Core/ReserveStudy/` engine
5. Move shared tenant interfaces (`ITenantContext`, `ITenantScoped`, `TenantClaimTypes`)
6. Update namespaces from `Horizon.*` → `Foresight.Shared.*`

### Step 3: Update Horizon

1. Add `<ProjectReference>` to `Foresight.Shared`
2. Update all `using` statements to reference `Foresight.Shared` namespaces
3. Remove moved files
4. Verify build

### Step 4: Create Pulse (Scaffold)

1. Add new MudBlazor Web App project
2. Reference `Foresight.Shared`
3. Set up `Program.cs` with shared DbContext and Identity
4. Create initial Pulse-specific service interfaces

### Step 5: Create Foresight.Web (Scaffold)

1. Add new Blazor Web App project (static SSR)
2. Reference `Foresight.Shared` (minimal)
3. Create marketing page shells
4. Wire up signup flow

---

## Naming Conventions

| Scope | Convention |
|---|---|
| **Solution** | `Foresight.sln` |
| **Shared Library** | `Foresight.Shared` — namespace `Foresight.Shared` |
| **Horizon** | `Horizon` — namespace `Horizon` (unchanged) |
| **Pulse** | `Pulse` — namespace `Pulse` |
| **Marketing Site** | `Foresight.Web` — namespace `Foresight.Web` |
| **Database Schema** | Default (no schema prefix) or `horizon` / `pulse` prefixes for clarity |
| **Test Projects** | `{Product}.Tests` pattern |

---

*Document created: Plan for Foresight platform multi-product architecture.*
*Last updated: This is a living document — update as architecture evolves.*
