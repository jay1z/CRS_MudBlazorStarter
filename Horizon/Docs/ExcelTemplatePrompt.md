You are my senior engineer pair-programmer. We are implementing an Excel-replacement “Reserve Study Cash-Flow Calculator” inside a multi-tenant Blazor SaaS (Blazor Server). Use C#/.NET, EF Core, and MudBlazor.

We must support tenant-specific calculation defaults (tenant settings) while allowing per-study/per-scenario overrides. The calculator itself must remain pure and unaware of tenancy/EF/UI. The system must produce deterministic 30-year results matching an Excel-like cash-flow model.

Non-negotiable constraints:
- All math must be in a pure Core library project (no EF, no UI).
- Use `decimal` for money; no float for money.
- Multi-tenant: every persisted entity includes TenantId and is filtered by tenant.
- Calculation results must be deterministic and idempotent for the same effective inputs.
- Provide xUnit Golden Master tests for one scenario.

Proceed step-by-step. After each step, show code changes (file paths + snippets).

========================================================
STEP 0 — Solution structure
Projects:
- ReserveStudy.Core (class library): domain models + calculation engine
- ReserveStudy.Infrastructure (class library): EF Core entities + tenant settings persistence
- ReserveStudy.Web (Blazor Server): UI pages/components
- ReserveStudy.Tests (xUnit): tests for Core engine and settings resolution

========================================================
STEP 1 — Core domain models (calculator contracts)
Create these types in ReserveStudy.Core:

Enums:
- InterestModel { AnnualAverageBalance, MonthlySimulation }
- ContributionFrequency { Annual, Monthly }
- Timing { StartOfPeriod, MidPeriod, EndOfPeriod }
- ExpenditureTiming { StartOfYear, MidYear, EndOfYear, MonthlySpread }
- RoundingPolicy { PerComponentPerYear, PerYearTotalsOnly }
- ContributionStrategy { FixedAnnual, EscalatingPercent, MaintainNonNegativeBalance }
- ComponentMethod { Replacement, PRN, Combo }

Models:
- ReserveStudyInput
  - int StartYear
  - int ProjectionYears (default 30)
  - decimal StartingBalance
  - decimal InflationRateDefault
  - decimal InterestRateAnnual
  - InterestModel InterestModel
  - ContributionFrequency ContributionFrequency
  - Timing ContributionTiming
  - ExpenditureTiming ExpenditureTiming
  - RoundingPolicy RoundingPolicy
  - ContributionStrategy ContributionStrategy
  - decimal InitialAnnualContribution
  - decimal ContributionEscalationRate
  - List<ReserveComponentInput> Components

- ReserveComponentInput
  - string Name
  - string Category
  - ComponentMethod Method
  - decimal CurrentCost
  - decimal? InflationRateOverride
  - Replacement fields:
    - int? LastServiceYear
    - int? UsefulLifeYears
    - int? RemainingLifeOverrideYears
  - PRN fields:
    - int? CycleYears (1 = annual)
    - decimal? AnnualCostOverride

Outputs:
- ReserveStudyResult
  - int StartYear
  - int ProjectionYears
  - IReadOnlyList<YearResult> Years
  - IReadOnlyList<CategoryAllocation> Allocation
  - GraphSeries Graph

- YearResult
  - int YearIndex (1..N)
  - int CalendarYear
  - decimal BeginningBalance
  - decimal Contribution
  - decimal InterestEarned
  - decimal Expenditures
  - decimal EndingBalance

- CategoryAllocation
  - string Category
  - decimal TotalSpend
  - decimal PercentOfTotal

- GraphSeries
  - decimal[] Expenditures
  - decimal[] Contributions
  - decimal[] EndingBalances

Money helpers:
- static class Money
  - Round2(decimal value)
  - Optional: Pow1p(decimal rate, int exponent) with deterministic behavior

========================================================
STEP 2 — Core: Expenditure schedule generator
Implement ExpenditureScheduleService in ReserveStudy.Core:

Public method:
- ExpenditureSchedule Generate(ReserveStudyInput input)

Where ExpenditureSchedule includes:
- Dictionary<string, decimal[]> ComponentSchedules (key by component name or stable id)
- decimal[] TotalExpendituresByYear

Rules:
- Inflation factor for yearIndex t (1..N): (1 + inflationRate)^(t-1)
- Replacement:
  - remainingLife = RemainingLifeOverrideYears ?? ((LastServiceYear + UsefulLifeYears) - StartYear)
  - if remainingLife <= 0 => first occurrence yearIndex = 1
  - else yearIndex = remainingLife + 1
  - repeat every UsefulLifeYears within projection horizon
  - cost in that year = CurrentCost * inflationFactor(yearIndex)
- PRN:
  - cycleYears = CycleYears ?? 1
  - base = AnnualCostOverride ?? CurrentCost
  - if cycleYears == 1: every year
  - else: yearIndex = 1, 1+cycleYears, 1+2*cycleYears...
  - cost = base * inflationFactor(yearIndex)
- Combo: add schedules of Replacement + PRN

Rounding:
- Implement rounding based on input.RoundingPolicy:
  - PerComponentPerYear: round each component schedule year cost to 2 decimals before summing totals
  - PerYearTotalsOnly: keep raw decimals per component; round totals per year to 2 decimals at the end
Document the policy.

========================================================
STEP 3 — Core: Contribution strategy service
Implement ContributionStrategyService:

Method:
- decimal[] BuildContributions(ReserveStudyInput input, decimal[] expenditures)

Strategies:
- FixedAnnual:
  - if ContributionFrequency == Annual: annualContribution = InitialAnnualContribution each year
  - if Monthly: convert annual to monthly deposits per year (annual/12) but still return annual totals per year; FundingPlanService will handle timing.
- EscalatingPercent:
  - annualContribution(yearIndex) = InitialAnnualContribution * (1+ContributionEscalationRate)^(yearIndex-1)
- MaintainNonNegativeBalance:
  - iterate year by year with a provisional funding plan estimate; if ending < 0, increase contribution for that year by abs(shortfall) and recompute for that year.
Keep it simple but deterministic; later we can improve.

========================================================
STEP 4 — Core: Funding plan engine with timing models
Implement FundingPlanService:

Method:
- IReadOnlyList<YearResult> BuildPlan(ReserveStudyInput input, decimal[] contributionsAnnual, decimal[] expendituresAnnual)

Handle InterestModel:
A) AnnualAverageBalance:
- interestEarned = averageBalance * InterestRateAnnual
- averageBalance should account for ContributionTiming + ExpenditureTiming:
  - StartOfPeriod vs EndOfPeriod affects average
  - Provide a simple deterministic approximation (document it)

B) MonthlySimulation (preferred to match Excel-like behavior):
- simulate 12 months per year
- monthly rate = (1+annualRate)^(1/12)-1
  - implement a deterministic approach: use double only for computing the monthly rate once, then store as decimal with fixed precision (e.g. 12 or 16 decimal places) and use that consistently
- apply contributions by ContributionFrequency & ContributionTiming:
  - If Annual: deposit once in month 1 (start), month 6 (mid), or month 12 (end)
  - If Monthly: deposit each month
- apply expenditures by ExpenditureTiming:
  - EndOfYear: month 12
  - MidYear: month 6
  - StartOfYear: month 1
  - MonthlySpread: expendituresAnnual/12 each month
- compute month-by-month:
  - balance += deposits
  - balance -= spend
  - interest = balance * monthlyRate
  - balance += interest
Aggregate monthly interest to year’s InterestEarned.
Round InterestEarned and EndingBalance per Money.Round2 only at the year boundary (or per policy if required).

Return YearResult list with rounded values consistent with Money.Round2.

========================================================
STEP 5 — Core: Orchestrator
Implement ReserveStudyCalculator:

Method:
- ReserveStudyResult Calculate(ReserveStudyInput input)

Flow:
- validate inputs (projection > 0, rates reasonable, replacement components have required fields, etc.)
- schedules = ExpenditureScheduleService.Generate(input)
- contributions = ContributionStrategyService.BuildContributions(input, schedules.TotalExpendituresByYear)
- years = FundingPlanService.BuildPlan(input, contributions, schedules.TotalExpendituresByYear)
- allocation:
  - sum component schedules by Category across all years
  - compute percent of total spend
- graph series arrays from totals and year ending balances

========================================================
STEP 6 — Infrastructure: Tenant settings + scenario overrides persistence
In ReserveStudy.Infrastructure, define EF entities with TenantId and query filters.

Entities:
- TenantReserveSettings
  - TenantId (PK)
  - DefaultProjectionYears
  - DefaultInflationRate
  - DefaultInterestRateAnnual
  - DefaultInterestModel
  - DefaultContributionStrategy
  - DefaultInitialAnnualContribution
  - DefaultContributionEscalationRate
  - DefaultContributionFrequency
  - DefaultContributionTiming
  - DefaultExpenditureTiming
  - DefaultRoundingPolicy
  - UpdatedAt

- ReserveStudy
  - Id, TenantId, Name, CreatedAt

- ReserveStudyScenario
  - Id, TenantId, ReserveStudyId, Name, Status (Draft/Published)
  - StartYear (required)
  - StartingBalance (required)
  - OverrideProjectionYears (nullable)
  - OverrideInflationRate (nullable)
  - OverrideInterestRateAnnual (nullable)
  - OverrideInterestModel (nullable)
  - OverrideContributionStrategy (nullable)
  - OverrideInitialAnnualContribution (nullable)
  - OverrideContributionEscalationRate (nullable)
  - OverrideContributionFrequency (nullable)
  - OverrideContributionTiming (nullable)
  - OverrideExpenditureTiming (nullable)
  - OverrideRoundingPolicy (nullable)

- ReserveComponent
  - Id, TenantId, ScenarioId
  - Name, Category, Method
  - CurrentCost
  - InflationRateOverride (nullable)
  - LastServiceYear, UsefulLifeYears, RemainingLifeOverrideYears (nullable)
  - CycleYears, AnnualCostOverride (nullable)

DbContext:
- Apply global query filter for TenantId on all tenant-owned entities.
- Add migrations.

========================================================
STEP 7 — Infrastructure/Core bridge: Effective settings resolver
Create a mapping layer (can live in Infrastructure or Web) that builds Core ReserveStudyInput.

Create models:
- EffectiveReserveSettings (not EF; just a composed object)
- SettingsResolver:
  - EffectiveReserveSettings Resolve(TenantReserveSettings tenant, ReserveStudyScenario scenario)

Rules:
- For each setting: effective = scenario.OverrideX ?? tenant.DefaultX
- Scenario required fields always used: StartYear, StartingBalance
- Build ReserveStudyInput using effective settings + components mapped to ReserveComponentInput

Important:
- Calculator remains unaware of TenantId, EF, or overrides.

========================================================
STEP 8 — Web: Tenant settings UI + Scenario UI with “Use tenant default” toggles
Build pages and components in ReserveStudy.Web using MudBlazor:

A) Tenant settings page:
- /settings/reserve
- Edit TenantReserveSettings fields
- Save per tenant

B) Scenario editor page:
- /reserve-studies/{studyId}/scenarios/{scenarioId}
Layout:
- AssumptionsEditor.razor
  - For each overridable field, show:
    - A toggle or checkbox “Use tenant default”
      - If checked: scenario.OverrideX = null; display tenant value as read-only or as placeholder
      - If unchecked: allow editing OverrideX
    - An “Override” chip/indicator when scenario.OverrideX != null
  - Required fields StartYear and StartingBalance always editable (not defaulted)

- ComponentsGrid.razor
  - Editable table of ReserveComponent rows
  - Per-component inflation override supported

- ResultsTabs.razor
  - Recalculate button:
    - load tenant settings + scenario + components
    - resolve Effective settings
    - map to ReserveStudyInput
    - call ReserveStudyCalculator.Calculate
    - render FundingPlanTable and charts
  - Add a banner showing which tenant defaults are in effect.

Do not persist results initially; compute on-demand.
Optionally add a small in-memory cache keyed by ScenarioId + scenario UpdatedAt.

========================================================
STEP 9 — Tests: Golden Master + Settings resolution tests
In ReserveStudy.Tests:
- Write tests for SettingsResolver:
  - scenario overrides take precedence
  - null override uses tenant default
- Write Golden Master test:
  - build a ReserveStudyInput from a known scenario (hard-coded)
  - expected arrays:
    - expectedTotalExpenditures[30]
    - expectedEndingBalances[30]
  - Assert equality after Money.Round2

========================================================
STEP 10 — Documentation & Extensibility
- Add XML docs describing timing rules and rounding policy.
- Make it easy to add new contribution strategies later.
- Ensure validation messages for missing replacement fields.

Now start implementing STEP 0 and continue sequentially, showing code and file changes for each step.
