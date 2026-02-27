namespace Horizon.Core.ReserveCalculator.Models;

/// <summary>
/// Data series for graphing the funding plan results.
/// Arrays are indexed by year (0-based, so index 0 = year 1).
/// </summary>
public class GraphSeries
{
    /// <summary>
    /// Total expenditures per year.
    /// </summary>
    public decimal[] Expenditures { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// Total contributions per year.
    /// </summary>
    public decimal[] Contributions { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// Ending balance per year.
    /// </summary>
    public decimal[] EndingBalances { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// Interest earned per year.
    /// </summary>
    public decimal[] InterestEarned { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// Calendar years for x-axis labels.
    /// </summary>
    public int[] Years { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Minimum ending balance across all years (for chart scaling).
    /// </summary>
    public decimal MinBalance => EndingBalances.Length > 0 ? EndingBalances.Min() : 0m;

    /// <summary>
    /// Maximum ending balance across all years (for chart scaling).
    /// </summary>
    public decimal MaxBalance => EndingBalances.Length > 0 ? EndingBalances.Max() : 0m;

    /// <summary>
    /// Total expenditures across all years.
    /// </summary>
    public decimal TotalExpenditures => Expenditures.Sum();

    /// <summary>
    /// Total contributions across all years.
    /// </summary>
    public decimal TotalContributions => Contributions.Sum();
}
