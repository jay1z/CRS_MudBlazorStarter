namespace Horizon.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines when expenditures are applied during the year.
/// </summary>
public enum ExpenditureTiming
{
    /// <summary>
    /// All expenditures for the year occur at the start of the year (month 1).
    /// </summary>
    StartOfYear,

    /// <summary>
    /// All expenditures for the year occur at mid-year (month 6).
    /// </summary>
    MidYear,

    /// <summary>
    /// All expenditures for the year occur at the end of the year (month 12).
    /// </summary>
    EndOfYear,

    /// <summary>
    /// Expenditures are spread evenly across all 12 months.
    /// </summary>
    MonthlySpread
}
