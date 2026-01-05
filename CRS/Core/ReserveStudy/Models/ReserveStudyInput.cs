using CRS.Core.ReserveCalculator.Enums;

namespace CRS.Core.ReserveCalculator.Models;

/// <summary>
/// Complete input for a reserve study calculation.
/// Contains all parameters and components needed to generate a funding plan.
/// </summary>
public class ReserveStudyInput
{
    /// <summary>
    /// The first calendar year of the projection.
    /// </summary>
    public int StartYear { get; set; }

    /// <summary>
    /// Number of years to project (typically 30).
    /// </summary>
    public int ProjectionYears { get; set; } = 30;

    /// <summary>
    /// Current reserve fund balance at the start of the study.
    /// </summary>
    public decimal StartingBalance { get; set; }

    /// <summary>
    /// Default annual inflation rate for component costs.
    /// Can be overridden per component.
    /// </summary>
    public decimal InflationRateDefault { get; set; } = 0.03m; // 3%

    /// <summary>
    /// Annual interest rate earned on reserve fund balance.
    /// </summary>
    public decimal InterestRateAnnual { get; set; } = 0.02m; // 2%

    /// <summary>
    /// Method for calculating interest on the fund balance.
    /// </summary>
    public InterestModel InterestModel { get; set; } = InterestModel.MonthlySimulation;

    /// <summary>
    /// How frequently contributions are made to the fund.
    /// </summary>
    public ContributionFrequency ContributionFrequency { get; set; } = ContributionFrequency.Monthly;

    /// <summary>
    /// When during the period contributions are applied.
    /// </summary>
    public Timing ContributionTiming { get; set; } = Timing.StartOfPeriod;

    /// <summary>
    /// When during the year expenditures are applied.
    /// </summary>
    public ExpenditureTiming ExpenditureTiming { get; set; } = ExpenditureTiming.MidYear;

    /// <summary>
    /// Policy for rounding monetary calculations.
    /// </summary>
    public RoundingPolicy RoundingPolicy { get; set; } = RoundingPolicy.PerComponentPerYear;

    /// <summary>
    /// Strategy for calculating annual contributions.
    /// </summary>
    public ContributionStrategy ContributionStrategy { get; set; } = ContributionStrategy.FixedAnnual;

    /// <summary>
    /// Initial annual contribution amount (used as base for escalating strategies).
    /// </summary>
    public decimal InitialAnnualContribution { get; set; }

    /// <summary>
    /// Annual rate of contribution increase for EscalatingPercent strategy.
    /// </summary>
    public decimal ContributionEscalationRate { get; set; } = 0.03m; // 3%

    /// <summary>
    /// List of components to include in the study.
    /// </summary>
    public List<ReserveComponentInput> Components { get; set; } = new();

    /// <summary>
    /// Gets the end year of the projection (StartYear + ProjectionYears - 1).
    /// </summary>
    public int EndYear => StartYear + ProjectionYears - 1;

    /// <summary>
    /// Validates all input parameters and components.
    /// </summary>
    /// <returns>A list of validation errors, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        // Basic validation
        if (ProjectionYears <= 0)
            errors.Add("ProjectionYears must be positive.");

        if (ProjectionYears > 100)
            errors.Add("ProjectionYears cannot exceed 100 years.");

        if (StartYear < 1900 || StartYear > 2200)
            errors.Add("StartYear must be between 1900 and 2200.");

        if (StartingBalance < 0)
            errors.Add("StartingBalance cannot be negative.");

        // Rate validations
        if (!Money.IsValidRate(InflationRateDefault, -0.2m, 0.5m))
            errors.Add("InflationRateDefault is out of reasonable range (-20% to 50%).");

        if (!Money.IsValidRate(InterestRateAnnual, -0.1m, 0.3m))
            errors.Add("InterestRateAnnual is out of reasonable range (-10% to 30%).");

        if (!Money.IsValidRate(ContributionEscalationRate, -0.2m, 0.5m))
            errors.Add("ContributionEscalationRate is out of reasonable range (-20% to 50%).");

        if (InitialAnnualContribution < 0)
            errors.Add("InitialAnnualContribution cannot be negative.");

        // Component validations
        if (Components.Count == 0)
            errors.Add("At least one component is required.");

        foreach (var component in Components)
        {
            var componentErrors = component.Validate();
            errors.AddRange(componentErrors);
        }

        // Check for duplicate component names (warning, not error)
        var duplicateNames = Components
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var name in duplicateNames)
        {
            errors.Add($"Warning: Duplicate component name '{name}' found.");
        }

        return errors;
    }

    /// <summary>
    /// Creates a deep copy of this input for scenario variations.
    /// </summary>
    public ReserveStudyInput Clone()
    {
        return new ReserveStudyInput
        {
            StartYear = StartYear,
            ProjectionYears = ProjectionYears,
            StartingBalance = StartingBalance,
            InflationRateDefault = InflationRateDefault,
            InterestRateAnnual = InterestRateAnnual,
            InterestModel = InterestModel,
            ContributionFrequency = ContributionFrequency,
            ContributionTiming = ContributionTiming,
            ExpenditureTiming = ExpenditureTiming,
            RoundingPolicy = RoundingPolicy,
            ContributionStrategy = ContributionStrategy,
            InitialAnnualContribution = InitialAnnualContribution,
            ContributionEscalationRate = ContributionEscalationRate,
            Components = Components.Select(c => new ReserveComponentInput
            {
                Id = c.Id,
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
            }).ToList()
        };
    }
}
