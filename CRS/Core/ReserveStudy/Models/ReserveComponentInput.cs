using Horizon.Core.ReserveCalculator.Enums;

namespace Horizon.Core.ReserveCalculator.Models;

/// <summary>
/// Input for a single reserve component in the study.
/// A component represents an asset or system that requires future funding.
/// </summary>
public class ReserveComponentInput
{
    /// <summary>
    /// Unique identifier for this component (optional, used for tracking).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Name of the component (e.g., "Roof Replacement", "Pool Resurfacing").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Category for grouping components (e.g., "Building Exterior", "Amenities").
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// The calculation method for this component's expenditure schedule.
    /// </summary>
    public ComponentMethod Method { get; set; } = ComponentMethod.Replacement;

    /// <summary>
    /// Current estimated cost of the component in today's dollars.
    /// This is the base cost before inflation adjustments.
    /// </summary>
    public decimal CurrentCost { get; set; }

    /// <summary>
    /// Component-specific inflation rate override.
    /// If null, the study's default inflation rate is used.
    /// </summary>
    public decimal? InflationRateOverride { get; set; }

    #region Replacement Method Fields

    /// <summary>
    /// The year this component was last serviced/replaced.
    /// Used to calculate remaining life: (LastServiceYear + UsefulLifeYears) - StartYear
    /// </summary>
    public int? LastServiceYear { get; set; }

    /// <summary>
    /// The expected useful life of the component in years.
    /// After this period, the component needs replacement.
    /// </summary>
    public int? UsefulLifeYears { get; set; }

    /// <summary>
    /// Override for the remaining life calculation.
    /// If set, this value is used directly instead of calculating from LastServiceYear.
    /// </summary>
    public int? RemainingLifeOverrideYears { get; set; }

    #endregion

    #region PRN (Periodic Repair/Maintenance) Method Fields

    /// <summary>
    /// The cycle interval in years for recurring costs.
    /// 1 = annual, 3 = every 3 years, etc.
    /// </summary>
    public int? CycleYears { get; set; }

    /// <summary>
    /// Override for the annual/periodic cost (instead of CurrentCost).
    /// Useful when PRN cost differs from replacement cost.
    /// </summary>
    public decimal? AnnualCostOverride { get; set; }

    #endregion

    /// <summary>
    /// Gets the effective inflation rate for this component.
    /// </summary>
    /// <param name="defaultRate">The study's default inflation rate.</param>
    /// <returns>The inflation rate to use for this component.</returns>
    public decimal GetEffectiveInflationRate(decimal defaultRate)
    {
        return InflationRateOverride ?? defaultRate;
    }

    /// <summary>
    /// Calculates the remaining life for a Replacement component.
    /// </summary>
    /// <param name="startYear">The study's start year.</param>
    /// <returns>The remaining life in years, or null if not applicable.</returns>
    public int? CalculateRemainingLife(int startYear)
    {
        if (RemainingLifeOverrideYears.HasValue)
            return RemainingLifeOverrideYears.Value;

        if (LastServiceYear.HasValue && UsefulLifeYears.HasValue)
        {
            return (LastServiceYear.Value + UsefulLifeYears.Value) - startYear;
        }

        return null;
    }

    /// <summary>
    /// Validates the component has required fields for its method.
    /// </summary>
    /// <returns>A list of validation error messages, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Component name is required.");

        if (CurrentCost < 0)
            errors.Add($"Component '{Name}': CurrentCost cannot be negative.");

        switch (Method)
        {
            case ComponentMethod.Replacement:
            case ComponentMethod.Combo:
                if (!UsefulLifeYears.HasValue && !RemainingLifeOverrideYears.HasValue)
                    errors.Add($"Component '{Name}': Replacement method requires UsefulLifeYears or RemainingLifeOverrideYears.");
                if (UsefulLifeYears.HasValue && UsefulLifeYears.Value <= 0)
                    errors.Add($"Component '{Name}': UsefulLifeYears must be positive.");
                break;
        }

        if (Method == ComponentMethod.PRN || Method == ComponentMethod.Combo)
        {
            if (CycleYears.HasValue && CycleYears.Value <= 0)
                errors.Add($"Component '{Name}': CycleYears must be positive.");
        }

        if (InflationRateOverride.HasValue && !Money.IsValidRate(InflationRateOverride.Value, -0.2m, 0.5m))
            errors.Add($"Component '{Name}': InflationRateOverride is out of reasonable range (-20% to 50%).");

        return errors;
    }
}
