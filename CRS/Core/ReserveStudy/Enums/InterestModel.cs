namespace Horizon.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines how interest is calculated on the reserve fund balance.
/// </summary>
public enum InterestModel
{
    /// <summary>
    /// Interest is calculated once per year using the average balance for the year.
    /// Simpler but less accurate than monthly simulation.
    /// </summary>
    AnnualAverageBalance,

    /// <summary>
    /// Interest is calculated monthly, simulating actual account behavior.
    /// More accurate and matches Excel-like cash-flow models.
    /// </summary>
    MonthlySimulation
}
