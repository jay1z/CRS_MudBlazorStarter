namespace CRS.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines when and how monetary values are rounded during calculations.
/// </summary>
public enum RoundingPolicy
{
    /// <summary>
    /// Each component's cost is rounded to 2 decimal places per year before summing.
    /// This matches typical Excel behavior where each cell is formatted/rounded individually.
    /// </summary>
    PerComponentPerYear,

    /// <summary>
    /// Component costs are kept as raw decimals; only the yearly totals are rounded.
    /// May produce slightly different results than per-component rounding.
    /// </summary>
    PerYearTotalsOnly
}
