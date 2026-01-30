using CRS.Core.ReserveCalculator.Enums;
using CRS.Core.ReserveCalculator.Models;
using CRS.Models;
using CRS.Models.ReserveStudyCalculator;
using Microsoft.Extensions.Logging;

namespace CRS.Services.ReserveCalculator;

/// <summary>
/// Adapts ReserveStudy entities and their elements/components to calculator inputs.
/// Provides conversion from the EF domain model to the pure calculator input model.
/// </summary>
public class ReserveStudyAdapter
{
    private readonly ILogger? _logger;

    public ReserveStudyAdapter(ILogger? logger = null)
    {
        _logger = logger;
    }
    /// <summary>
    /// Builds calculator input from a ReserveStudy entity with its proposal and elements.
    /// Uses the study's financial data and converts elements to components.
    /// </summary>
    /// <param name="study">The reserve study with loaded elements.</param>
    /// <param name="tenantSettings">Tenant default settings for fallback values.</param>
    /// <returns>A ReserveStudyInput ready for calculation.</returns>
    public ReserveStudyInput BuildInputFromStudy(
        ReserveStudy study,
        TenantReserveSettings tenantSettings)
    {
        // Get financial info - primary source of financial data
        var financialInfo = study.FinancialInfo;

        // Starting balance: prefer FinancialInfo.JanuaryFirstReserveBalance, fallback to study.CurrentReserveFunds
        var startingBalance = financialInfo?.JanuaryFirstReserveBalance 
            ?? study.CurrentReserveFunds 
            ?? 0m;

        // Annual contribution: prefer FinancialInfo.BudgetedContributionCurrentYear, 
        // fallback to study.MonthlyReserveContribution * 12
        var annualContribution = financialInfo?.BudgetedContributionCurrentYear 
            ?? (study.MonthlyReserveContribution ?? 0m) * 12m;

        // Interest rate: prefer FinancialInfo.InterestRateOnReserveFunds, fallback to study/tenant
        // NOTE: All rate sources now store in decimal form (e.g., 0.05 for 5%)
        var interestRate = financialInfo?.InterestRateOnReserveFunds 
            ?? study.AnnualInterestRate 
            ?? tenantSettings.DefaultInterestRateAnnual;

        var input = new ReserveStudyInput
        {
            // Use study date or current year
            StartYear = study.StudyDate?.Year ?? DateTime.UtcNow.Year,

            // Projection years from tenant settings
            ProjectionYears = tenantSettings.DefaultProjectionYears,

            // Starting balance from FinancialInfo or study
            StartingBalance = startingBalance,

            // Rates
            InflationRateDefault = study.AnnualInflationRate ?? tenantSettings.DefaultInflationRate,
            InterestRateAnnual = interestRate,

            // Annual contribution from FinancialInfo or study
            InitialAnnualContribution = annualContribution,

            // Use tenant defaults for other settings
            InterestModel = tenantSettings.DefaultInterestModel,
            ContributionFrequency = tenantSettings.DefaultContributionFrequency,
            ContributionTiming = tenantSettings.DefaultContributionTiming,
            ExpenditureTiming = tenantSettings.DefaultExpenditureTiming,
            RoundingPolicy = tenantSettings.DefaultRoundingPolicy,
            ContributionStrategy = tenantSettings.DefaultContributionStrategy,
            ContributionEscalationRate = tenantSettings.DefaultContributionEscalationRate,

            // Convert elements to components
            Components = ConvertElementsToComponents(study)
        };

        return input;
    }

    /// <summary>
    /// Builds calculator input from a ReserveStudyScenario with its components.
    /// Merges scenario overrides with tenant defaults.
    /// </summary>
    /// <param name="scenario">The scenario with loaded components.</param>
    /// <param name="tenantSettings">Tenant default settings.</param>
    /// <returns>A ReserveStudyInput ready for calculation.</returns>
    public ReserveStudyInput BuildInputFromScenario(
        ReserveStudyScenario scenario,
        TenantReserveSettings tenantSettings)
    {
        var input = new ReserveStudyInput
        {
            // Scenario-specific required fields
            StartYear = scenario.StartYear,
            StartingBalance = scenario.StartingBalance,
            
            // Override or default for each setting
            ProjectionYears = scenario.OverrideProjectionYears ?? tenantSettings.DefaultProjectionYears,
            InflationRateDefault = scenario.OverrideInflationRate ?? tenantSettings.DefaultInflationRate,
            InterestRateAnnual = scenario.OverrideInterestRateAnnual ?? tenantSettings.DefaultInterestRateAnnual,
            InterestModel = scenario.OverrideInterestModel ?? tenantSettings.DefaultInterestModel,
            ContributionFrequency = scenario.OverrideContributionFrequency ?? tenantSettings.DefaultContributionFrequency,
            ContributionTiming = scenario.OverrideContributionTiming ?? tenantSettings.DefaultContributionTiming,
            ExpenditureTiming = scenario.OverrideExpenditureTiming ?? tenantSettings.DefaultExpenditureTiming,
            RoundingPolicy = scenario.OverrideRoundingPolicy ?? tenantSettings.DefaultRoundingPolicy,
            ContributionStrategy = scenario.OverrideContributionStrategy ?? tenantSettings.DefaultContributionStrategy,
            InitialAnnualContribution = scenario.OverrideInitialAnnualContribution ?? tenantSettings.DefaultInitialAnnualContribution,
            ContributionEscalationRate = scenario.OverrideContributionEscalationRate ?? tenantSettings.DefaultContributionEscalationRate,
            
            // Convert scenario components
            Components = ConvertScenarioComponents(scenario.Components)
        };

        return input;
    }

    /// <summary>
    /// Converts ReserveStudy elements (Building, Common, Additional) to calculator components.
    /// Elements without cost data are assigned estimated costs based on element type.
    /// </summary>
    private List<ReserveComponentInput> ConvertElementsToComponents(ReserveStudy study)
    {
        var components = new List<ReserveComponentInput>();
        var buildingConverted = 0;
        var buildingSkipped = 0;
        var commonConverted = 0;
        var commonSkipped = 0;
        var additionalConverted = 0;
        var additionalSkipped = 0;

        // Convert building elements
        if (study.ReserveStudyBuildingElements != null)
        {
            _logger?.LogInformation("ReserveStudyAdapter: Converting {Count} building elements", 
                study.ReserveStudyBuildingElements.Count);

            foreach (var element in study.ReserveStudyBuildingElements)
            {
                var component = ConvertBuildingElement(element);
                if (component != null)
                {
                    components.Add(component);
                    buildingConverted++;
                }
                else
                {
                    buildingSkipped++;
                }
            }
        }

        // Convert common elements
        if (study.ReserveStudyCommonElements != null)
        {
            _logger?.LogInformation("ReserveStudyAdapter: Converting {Count} common elements", 
                study.ReserveStudyCommonElements.Count);

            foreach (var element in study.ReserveStudyCommonElements)
            {
                var component = ConvertCommonElement(element);
                if (component != null)
                {
                    components.Add(component);
                    commonConverted++;
                }
                else
                {
                    commonSkipped++;
                }
            }
        }

        // Convert additional elements
        if (study.ReserveStudyAdditionalElements != null)
        {
            _logger?.LogInformation("ReserveStudyAdapter: Converting {Count} additional elements", 
                study.ReserveStudyAdditionalElements.Count);

            foreach (var element in study.ReserveStudyAdditionalElements)
            {
                var component = ConvertAdditionalElement(element);
                if (component != null)
                {
                    components.Add(component);
                    additionalConverted++;
                }
                else
                {
                    additionalSkipped++;
                }
            }
        }

        _logger?.LogInformation(
            "ReserveStudyAdapter: Conversion complete - Building: {BC} converted/{BS} skipped, " +
            "Common: {CC} converted/{CS} skipped, Additional: {AC} converted/{AS} skipped, Total: {Total}",
            buildingConverted, buildingSkipped,
            commonConverted, commonSkipped,
            additionalConverted, additionalSkipped,
            components.Count);

        return components;
    }

    /// <summary>
    /// Converts a building element to a calculator component.
    /// </summary>
    private ReserveComponentInput? ConvertBuildingElement(ReserveStudyBuildingElement element)
    {
        // Try BuildingElement navigation property name first, then use a fallback based on BuildingElementId
        var name = element.BuildingElement?.Name;

        if (string.IsNullOrEmpty(name))
        {
            // Log the issue - BuildingElement navigation property wasn't loaded
            _logger?.LogWarning(
                "ReserveStudyAdapter: BuildingElement navigation is null for element {ElementId} (BuildingElementId: {BuildingElementId}). " +
                "Ensure .Include(e => e.BuildingElement) is used when loading the study.",
                element.Id, element.BuildingElementId);

            // Use a fallback name based on the BuildingElementId to avoid losing the element entirely
            name = $"Building Element {element.BuildingElementId.ToString()[..8]}";
            _logger?.LogInformation("ReserveStudyAdapter: Using fallback name '{Name}' for building element", name);
        }

        // Extract useful life from min/max element options (use midpoint of the combined range)
        // Try new MinUsefulLifeOption/MaxUsefulLifeOption first, fall back to legacy UsefulLifeOption
        int? usefulLifeYears = GetUsefulLifeFromOptions(
            element.MinUsefulLifeOption,
            element.MaxUsefulLifeOption) ??
            GetMidpointFromRange(
                element.UsefulLifeOption?.MinValue,
                element.UsefulLifeOption?.MaxValue);

        // Use RemainingLifeYears directly if set, otherwise fall back to legacy option
        int? remainingLifeYears = element.RemainingLifeYears ?? 
            GetMidpointFromRange(
                element.RemainingLifeOption?.MinValue,
                element.RemainingLifeOption?.MaxValue);

        // Calculate last service year if we have last serviced date
        int? lastServiceYear = element.LastServiced?.Year;

        // Building elements don't have cost in the current model - use placeholder
        decimal estimatedCost = 10000m;

        _logger?.LogDebug(
            "ReserveStudyAdapter: Converted building element '{Name}' - UsefulLife: {UsefulLife}, RemainingLife: {RemainingLife}",
            name, usefulLifeYears, remainingLifeYears);

        return new ReserveComponentInput
        {
            Id = element.Id.ToString(),
            Name = name,
            Category = "Building",
            Method = ComponentMethod.Replacement,
            CurrentCost = estimatedCost,
            UsefulLifeYears = usefulLifeYears,
            RemainingLifeOverrideYears = remainingLifeYears,
            LastServiceYear = lastServiceYear
        };
    }

    /// <summary>
    /// Converts a common element to a calculator component.
    /// </summary>
    private ReserveComponentInput? ConvertCommonElement(ReserveStudyCommonElement element)
    {
        // Try element name first, then CommonElement navigation
        var name = element.ElementName ?? element.CommonElement?.Name;

        if (string.IsNullOrEmpty(name))
        {
            // Log the issue - neither ElementName nor CommonElement navigation property was available
            _logger?.LogWarning(
                "ReserveStudyAdapter: CommonElement name is null for element {ElementId} (CommonElementId: {CommonElementId}). " +
                "Ensure .Include(e => e.CommonElement) is used when loading the study or set ElementName.",
                element.Id, element.CommonElementId);

            // Use a fallback name based on the CommonElementId to avoid losing the element entirely
            name = $"Common Element {element.CommonElementId.ToString()[..8]}";
            _logger?.LogInformation("ReserveStudyAdapter: Using fallback name '{Name}' for common element", name);
        }

        // Use stored values or extract from options
        // Try new MinUsefulLifeOption/MaxUsefulLifeOption first, then UsefulLife property, then legacy option
        int? usefulLifeYears = GetUsefulLifeFromOptions(
            element.MinUsefulLifeOption,
            element.MaxUsefulLifeOption) ??
            element.UsefulLife ?? 
            GetMidpointFromRange(
                element.UsefulLifeOption?.MinValue,
                element.UsefulLifeOption?.MaxValue);

        // Use RemainingLifeYears directly, then RemainingLife property, then legacy option
        int? remainingLifeYears = element.RemainingLifeYears ??
            element.RemainingLife ?? 
            GetMidpointFromRange(
                element.RemainingLifeOption?.MinValue,
                element.RemainingLifeOption?.MaxValue);

        int? lastServiceYear = element.LastServiced?.Year;

        // Use ReplacementCost if available, otherwise placeholder
        decimal cost = element.ReplacementCost ?? 10000m;

        _logger?.LogDebug(
            "ReserveStudyAdapter: Converted common element '{Name}' - UsefulLife: {UsefulLife}, RemainingLife: {RemainingLife}",
            name, usefulLifeYears, remainingLifeYears);

        return new ReserveComponentInput
        {
            Id = element.Id.ToString(),
            Name = name,
            Category = element.Category ?? "Common Area",
            Method = ComponentMethod.Replacement,
            CurrentCost = cost,
            UsefulLifeYears = usefulLifeYears,
            RemainingLifeOverrideYears = remainingLifeYears,
            LastServiceYear = lastServiceYear
        };
    }

    /// <summary>
    /// Converts an additional element to a calculator component.
    /// </summary>
    private ReserveComponentInput? ConvertAdditionalElement(ReserveStudyAdditionalElement element)
    {
        var name = element.Name;
        if (string.IsNullOrEmpty(name))
        {
            _logger?.LogWarning(
                "ReserveStudyAdapter: Additional element {ElementId} has no name, skipping",
                element.Id);
            return null;
        }

        // Try new MinUsefulLifeOption/MaxUsefulLifeOption first, fall back to legacy UsefulLifeOption
        int? usefulLifeYears = GetUsefulLifeFromOptions(
            element.MinUsefulLifeOption,
            element.MaxUsefulLifeOption) ??
            GetMidpointFromRange(
                element.UsefulLifeOption?.MinValue,
                element.UsefulLifeOption?.MaxValue);

        // Use RemainingLifeYears directly if set, otherwise fall back to legacy option
        int? remainingLifeYears = element.RemainingLifeYears ??
            GetMidpointFromRange(
                element.RemainingLifeOption?.MinValue,
                element.RemainingLifeOption?.MaxValue);

        int? lastServiceYear = element.LastServiced?.Year;

        // Additional elements don't have cost in the current model - use placeholder
        decimal estimatedCost = 10000m;

        _logger?.LogDebug(
            "ReserveStudyAdapter: Converted additional element '{Name}' - UsefulLife: {UsefulLife}, RemainingLife: {RemainingLife}",
            name, usefulLifeYears, remainingLifeYears);

        return new ReserveComponentInput
        {
            Id = element.Id.ToString(),
            Name = name,
            Category = "Additional",
            Method = ComponentMethod.Replacement,
            CurrentCost = estimatedCost,
            UsefulLifeYears = usefulLifeYears,
            RemainingLifeOverrideYears = remainingLifeYears,
            LastServiceYear = lastServiceYear
        };
    }

    /// <summary>
    /// Converts scenario components to calculator input components.
    /// </summary>
    private List<ReserveComponentInput> ConvertScenarioComponents(
        ICollection<ReserveScenarioComponent> scenarioComponents)
    {
        return scenarioComponents
            .Where(c => c.DateDeleted == null)
            .OrderBy(c => c.SortOrder)
            .Select(c => new ReserveComponentInput
            {
                Id = c.Id.ToString(),
                Name = c.Name,
                Category = c.Category,
                Method = c.Method,
                CurrentCost = c.CurrentCost,
                InflationRateOverride = c.InflationRateOverride,
                LastServiceYear = c.LastServiceYear,
                UsefulLifeYears = c.UsefulLifeYears,
                RemainingLifeOverrideYears = c.RemainingLifeOverrideYears,
                CycleYears = c.CycleYears,
                AnnualCostOverride = c.AnnualCostOverride
            })
            .ToList();
    }

    /// <summary>
    /// Gets the useful life from min/max element options.
    /// Calculates the midpoint of the overall range from min option's min value to max option's max value.
    /// </summary>
    private int? GetUsefulLifeFromOptions(ElementOption? minOption, ElementOption? maxOption)
    {
        // If we have both min and max options, use the full range (min's MinValue to max's MaxValue)
        if (minOption != null && maxOption != null)
        {
            int min = minOption.MinValue ?? minOption.MaxValue ?? 0;
            int max = maxOption.MaxValue ?? maxOption.MinValue ?? 0;
            return (min + max) / 2;
        }

        // If we only have one option, use its midpoint
        if (minOption != null)
            return GetMidpointFromRange(minOption.MinValue, minOption.MaxValue);

        if (maxOption != null)
            return GetMidpointFromRange(maxOption.MinValue, maxOption.MaxValue);

        return null;
    }

    /// <summary>
    /// Gets the midpoint value from a min/max range.
    /// </summary>
    private int? GetMidpointFromRange(int? min, int? max)
    {
        if (!min.HasValue && !max.HasValue)
            return null;

        if (!min.HasValue)
            return max;

        if (!max.HasValue)
            return min;

        return (min.Value + max.Value) / 2;
    }

    /// <summary>
    /// Creates a new scenario from an existing ReserveStudy.
    /// Useful for creating calculation scenarios from proposal data.
    /// </summary>
    /// <param name="study">The reserve study to create a scenario from.</param>
    /// <param name="scenarioName">Name for the new scenario.</param>
    /// <returns>A new ReserveStudyScenario populated from study data.</returns>
    public ReserveStudyScenario CreateScenarioFromStudy(ReserveStudy study, string scenarioName = "From Proposal")
    {
        var scenario = new ReserveStudyScenario
        {
            TenantId = study.TenantId,
            ReserveStudyId = study.Id,
            Name = scenarioName,
            Status = ScenarioStatus.Draft,
            Description = $"Generated from reserve study on {DateTime.UtcNow:d}",
            StartYear = study.StudyDate?.Year ?? DateTime.UtcNow.Year,
            StartingBalance = study.CurrentReserveFunds ?? 0m,
            
            // Store study rates as overrides if provided
            OverrideInflationRate = study.AnnualInflationRate,
            OverrideInterestRateAnnual = study.AnnualInterestRate,
            OverrideInitialAnnualContribution = study.MonthlyReserveContribution.HasValue 
                ? study.MonthlyReserveContribution.Value * 12m 
                : null,
            
            DateCreated = DateTime.UtcNow,
            DateModified = DateTime.UtcNow
        };

        // Convert elements to scenario components
        var elementComponents = ConvertElementsToComponents(study);
        var sortOrder = 0;
        foreach (var input in elementComponents)
        {
            scenario.Components.Add(new ReserveScenarioComponent
            {
                TenantId = study.TenantId,
                Name = input.Name,
                Category = input.Category,
                Method = input.Method,
                CurrentCost = input.CurrentCost,
                UsefulLifeYears = input.UsefulLifeYears,
                RemainingLifeOverrideYears = input.RemainingLifeOverrideYears,
                LastServiceYear = input.LastServiceYear,
                CycleYears = input.CycleYears,
                SortOrder = sortOrder++
            });
        }

        return scenario;
    }
}
