using Horizon.Core.ReserveCalculator.Models;
using Horizon.Models.ReserveStudyCalculator;

namespace Horizon.Services.ReserveCalculator;

/// <summary>
/// Resolves effective settings by combining tenant defaults with scenario overrides.
/// Maps EF entities to Core calculation input models.
/// </summary>
public class SettingsResolver
{
    /// <summary>
    /// Resolves effective settings from tenant defaults and scenario overrides.
    /// </summary>
    /// <param name="tenantSettings">Tenant default settings.</param>
    /// <param name="scenario">Scenario with optional overrides.</param>
    /// <returns>Resolved effective settings with source tracking.</returns>
    public EffectiveReserveSettings Resolve(
        TenantReserveSettings tenantSettings,
        ReserveStudyScenario scenario)
    {
        var effective = new EffectiveReserveSettings();

        // Required scenario fields (always from scenario)
        effective.StartYear = scenario.StartYear;
        effective.Sources[nameof(effective.StartYear)] = SettingSource.ScenarioRequired;

        effective.StartingBalance = scenario.StartingBalance;
        effective.Sources[nameof(effective.StartingBalance)] = SettingSource.ScenarioRequired;

        // Overridable settings: scenario override ?? tenant default
        effective.ProjectionYears = scenario.OverrideProjectionYears 
            ?? tenantSettings.DefaultProjectionYears;
        effective.Sources[nameof(effective.ProjectionYears)] = scenario.OverrideProjectionYears.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.InflationRate = scenario.OverrideInflationRate 
            ?? tenantSettings.DefaultInflationRate;
        effective.Sources[nameof(effective.InflationRate)] = scenario.OverrideInflationRate.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.InterestRateAnnual = scenario.OverrideInterestRateAnnual 
            ?? tenantSettings.DefaultInterestRateAnnual;
        effective.Sources[nameof(effective.InterestRateAnnual)] = scenario.OverrideInterestRateAnnual.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.InterestModel = scenario.OverrideInterestModel 
            ?? tenantSettings.DefaultInterestModel;
        effective.Sources[nameof(effective.InterestModel)] = scenario.OverrideInterestModel.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.ContributionStrategy = scenario.OverrideContributionStrategy 
            ?? tenantSettings.DefaultContributionStrategy;
        effective.Sources[nameof(effective.ContributionStrategy)] = scenario.OverrideContributionStrategy.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.InitialAnnualContribution = scenario.OverrideInitialAnnualContribution 
            ?? tenantSettings.DefaultInitialAnnualContribution;
        effective.Sources[nameof(effective.InitialAnnualContribution)] = scenario.OverrideInitialAnnualContribution.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.ContributionEscalationRate = scenario.OverrideContributionEscalationRate 
            ?? tenantSettings.DefaultContributionEscalationRate;
        effective.Sources[nameof(effective.ContributionEscalationRate)] = scenario.OverrideContributionEscalationRate.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.ContributionFrequency = scenario.OverrideContributionFrequency 
            ?? tenantSettings.DefaultContributionFrequency;
        effective.Sources[nameof(effective.ContributionFrequency)] = scenario.OverrideContributionFrequency.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.ContributionTiming = scenario.OverrideContributionTiming 
            ?? tenantSettings.DefaultContributionTiming;
        effective.Sources[nameof(effective.ContributionTiming)] = scenario.OverrideContributionTiming.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.ExpenditureTiming = scenario.OverrideExpenditureTiming 
            ?? tenantSettings.DefaultExpenditureTiming;
        effective.Sources[nameof(effective.ExpenditureTiming)] = scenario.OverrideExpenditureTiming.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        effective.RoundingPolicy = scenario.OverrideRoundingPolicy 
            ?? tenantSettings.DefaultRoundingPolicy;
        effective.Sources[nameof(effective.RoundingPolicy)] = scenario.OverrideRoundingPolicy.HasValue 
            ? SettingSource.ScenarioOverride 
            : SettingSource.TenantDefault;

        return effective;
    }

    /// <summary>
    /// Maps effective settings and components to a Core ReserveStudyInput for calculation.
    /// </summary>
    /// <param name="effectiveSettings">Resolved effective settings.</param>
    /// <param name="components">Components from the scenario.</param>
    /// <returns>Input model ready for the pure calculator.</returns>
    public ReserveStudyInput BuildCalculatorInput(
        EffectiveReserveSettings effectiveSettings,
        IEnumerable<ReserveScenarioComponent> components)
    {
        var input = new ReserveStudyInput
        {
            StartYear = effectiveSettings.StartYear,
            StartingBalance = effectiveSettings.StartingBalance,
            ProjectionYears = effectiveSettings.ProjectionYears,
            InflationRateDefault = effectiveSettings.InflationRate,
            InterestRateAnnual = effectiveSettings.InterestRateAnnual,
            InterestModel = effectiveSettings.InterestModel,
            ContributionStrategy = effectiveSettings.ContributionStrategy,
            InitialAnnualContribution = effectiveSettings.InitialAnnualContribution,
            ContributionEscalationRate = effectiveSettings.ContributionEscalationRate,
            ContributionFrequency = effectiveSettings.ContributionFrequency,
            ContributionTiming = effectiveSettings.ContributionTiming,
            ExpenditureTiming = effectiveSettings.ExpenditureTiming,
            RoundingPolicy = effectiveSettings.RoundingPolicy,
            Components = components
                .Where(c => c.DateDeleted == null)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(MapComponent)
                .ToList()
        };

        return input;
    }

    /// <summary>
    /// Maps an EF component entity to a Core component input.
    /// </summary>
    private ReserveComponentInput MapComponent(ReserveScenarioComponent entity)
    {
        return new ReserveComponentInput
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Category = entity.Category,
            Method = entity.Method,
            CurrentCost = entity.CurrentCost,
            InflationRateOverride = entity.InflationRateOverride,
            LastServiceYear = entity.LastServiceYear,
            UsefulLifeYears = entity.UsefulLifeYears,
            RemainingLifeOverrideYears = entity.RemainingLifeOverrideYears,
            CycleYears = entity.CycleYears,
            AnnualCostOverride = entity.AnnualCostOverride
        };
    }

    /// <summary>
    /// Creates default tenant settings with reasonable initial values.
    /// </summary>
    public static TenantReserveSettings CreateDefaultSettings(int tenantId)
    {
        return new TenantReserveSettings
        {
            TenantId = tenantId,
            DefaultProjectionYears = 30,
            DefaultInflationRate = 0.03m,
            DefaultInterestRateAnnual = 0.02m,
            DefaultInterestModel = Core.ReserveCalculator.Enums.InterestModel.MonthlySimulation,
            DefaultContributionStrategy = Core.ReserveCalculator.Enums.ContributionStrategy.EscalatingPercent,
            DefaultInitialAnnualContribution = 50000m,
            DefaultContributionEscalationRate = 0.03m,
            DefaultContributionFrequency = Core.ReserveCalculator.Enums.ContributionFrequency.Monthly,
            DefaultContributionTiming = Core.ReserveCalculator.Enums.Timing.StartOfPeriod,
            DefaultExpenditureTiming = Core.ReserveCalculator.Enums.ExpenditureTiming.MidYear,
            DefaultRoundingPolicy = Core.ReserveCalculator.Enums.RoundingPolicy.PerComponentPerYear,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
