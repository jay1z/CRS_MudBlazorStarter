# Foresight Platform — Continuation Prompt

> **Copy everything below the line into a new Copilot Chat session to resume work.**

---

## Context

Read `docs/ARCHITECTURE.md` for the full Foresight platform architecture plan. This is a multi-product SaaS platform:

- **Foresight** = product suite name. Tagline: "Capital intelligence platform that turns reserve data into forward visibility and actionable insight."
- **Horizon** = existing Blazor Server (MudBlazor) app — reserve study authoring, funding plans, narratives
- **Pulse** = new Blazor Server (MudBlazor) app — operational awareness, portfolio dashboards, alerts for management companies
- **Foresight.Web** = new Blazor Web App (static SSR) — marketing website + signup
- **Foresight.Shared** = .NET 10 class library — shared models, core engine, tenant contracts

## What Has Been Completed

1. **Solution renamed** from `CRS.sln` → `Foresight.sln`
2. **`Foresight.Shared` class library created** (net10.0) and added to solution
3. **Foundational types extracted** from Horizon into Foresight.Shared:
   - `Foresight.Shared/Services/Tenant/ITenantScoped.cs` (namespace: `Horizon.Services.Tenant`)
   - `Foresight.Shared/Services/Tenant/ITenantContext.cs` (namespace: `Horizon.Services.Tenant`)
   - `Foresight.Shared/Services/Tenant/TenantClaimTypes.cs` (namespace: `Horizon.Services.Tenant`)
   - `Foresight.Shared/Models/BaseModel.cs` (namespace: `Horizon.Models`)
   - `Foresight.Shared/Models/Tenant.cs` (namespace: `Horizon.Models`) — includes `TenantProvisioningStatus`, `SubscriptionTier`, `SubscriptionStatus`, `SubscriptionTierDefaults`
   - `Foresight.Shared/Data/ApplicationUser.cs` (namespace: `Horizon.Data`) — includes `StatusEnum`
   - `Foresight.Shared/Core/ReserveStudy/Enums/` — all 7 enums (FundingStatus, ContributionStrategy, ContributionFrequency, ComponentMethod, ExpenditureTiming, InterestModel, RoundingPolicy, Timing)
   - `Foresight.Shared/Core/ReserveStudy/Models/` — ReserveStudyInput, ReserveStudyResult, YearResult, ComponentSummary, ExpenditureSchedule, CategoryAllocation, GraphSeries, ReserveComponentInput
   - `Foresight.Shared/Core/ReserveStudy/Services/` — ReserveStudyCalculator, ExpenditureScheduleService, ContributionStrategyService, FundingPlanService, FullyFundedBalanceService
   - `Foresight.Shared/Core/ReserveStudy/Money.cs`
4. **Originals removed** from Horizon (no duplicates)
5. **Horizon references Foresight.Shared** via `<ProjectReference>`
6. **Build succeeds** — 0 errors, 168/168 tests pass

### Critical Design Decision: Namespaces

Types in `Foresight.Shared` currently **keep their original `Horizon.*` namespaces** (e.g., `Horizon.Models`, `Horizon.Data`, `Horizon.Services.Tenant`, `Horizon.Core.ReserveCalculator.*`). This was intentional — it allowed extracting to a separate assembly with zero code changes in Horizon. Namespace rename to `Foresight.Shared.*` is a future phase.

### Deferred: ApplicationDbContext

`Horizon/Data/ApplicationDbContext.cs` was NOT moved because it references 40+ Horizon-specific model types (Invoice, Proposal, KanbanTask, etc.). Moving it would require moving ALL models. Options for when Pulse needs DB access:
- **Option A:** Extract a `SharedDbContext` base class in Foresight.Shared with core entities (Tenant, Community, ReserveStudy), and have Horizon's `ApplicationDbContext` inherit from it
- **Option B:** Give Pulse its own read-only `PulseDbContext` that maps only the tables it needs
- **Option A is recommended** per the architecture plan

## Remaining Work — In Priority Order

### Phase 2A: Continue Shared Library Extraction

Move additional models from Horizon to Foresight.Shared that Pulse will need to read. These models currently live in `Horizon/Models/` and use namespace `Horizon.Models`:

- `Community.cs` — core entity that Pulse dashboards aggregate
- `ReserveStudy.cs` — the main study entity (already partially depends on moved types)
- `BuildingElement.cs`, `CommonElement.cs`, `ReserveStudyAdditionalElement.cs` — component data
- `ReserveStudyBuildingElement.cs`, `ReserveStudyCommonElement.cs` — junction models
- `FundingStatusLevel.cs` — funding health indicator
- `IReserveStudyElement.cs`, `IContact.cs` — shared interfaces
- `Address.cs` — used by Community
- `Contact.cs`, `PropertyManager.cs` — community contacts
- `ElementOption.cs`, `ServiceContact.cs`, `ElementDependency.cs` — element metadata

For each: copy to Foresight.Shared, remove from Horizon, verify build. Keep original `Horizon.Models` namespace for now.

### Phase 2B: Extract SharedDbContext Base Class

1. Create `Foresight.Shared/Data/SharedDbContext.cs` with `DbSet`s for core entities only (Tenant, Community, ReserveStudy, BuildingElement, etc.)
2. Make Horizon's `ApplicationDbContext` inherit from `SharedDbContext` instead of `IdentityDbContext` directly
3. Move migration infrastructure to Foresight.Shared
4. Verify migrations still work

### Phase 3: Create Pulse Project

1. Create a new **MudBlazor Web App** project named `Pulse` targeting net10.0
2. Add `<ProjectReference>` to `Foresight.Shared`
3. Set up `Pulse/Program.cs`:
   - Configure shared DbContext (or PulseDbContext) with same connection string
   - Configure ASP.NET Identity (shared users)
   - Add MudBlazor services
   - Add tenant resolution middleware
   - Add Serilog logging
4. Create directory structure per `docs/ARCHITECTURE.md`:
   - `Pulse/Components/` — pages and layouts
   - `Pulse/Services/Alerts/` — threshold monitoring
   - `Pulse/Services/Portfolio/` — multi-community aggregation
   - `Pulse/Services/Readiness/` — readiness scoring
   - `Pulse/Hubs/` — SignalR for real-time alerts
   - `Pulse/Models/` — Pulse-specific entities (PulseAlert, ReadinessSignal, PortfolioSnapshot, etc.)
5. Create `Pulse.Tests` xUnit project

### Phase 4: Create Foresight.Web Project

1. Create a new **Blazor Web App** project named `Foresight.Web` targeting net10.0 (static SSR mode)
2. Add `<ProjectReference>` to `Foresight.Shared` (minimal)
3. Create marketing page shells: Home, Horizon product, Pulse product, Pricing, Blog, Contact, Signup
4. Wire signup flow to create Tenant → Stripe checkout → redirect to Horizon

### Phase 5: Namespace Rename (Optional, Low Priority)

Rename all `Horizon.*` namespaces in Foresight.Shared to `Foresight.Shared.*`:
- `Horizon.Models` → `Foresight.Shared.Models`
- `Horizon.Data` → `Foresight.Shared.Data`
- `Horizon.Services.Tenant` → `Foresight.Shared.Tenant`
- `Horizon.Core.ReserveCalculator.*` → `Foresight.Shared.Core.ReserveCalculator.*`
- Add `global using` directives or update all consuming code in Horizon

## Instructions

Pick up from whichever phase makes sense. Before starting, read `docs/ARCHITECTURE.md` for full context. Always verify `dotnet build Foresight.sln` succeeds after each step. Run `dotnet test` after any model or service changes.
