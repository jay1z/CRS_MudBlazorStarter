namespace CRS.Core.ReserveCalculator.Models;

/// <summary>
/// Allocation of expenditures by category across the projection period.
/// </summary>
public class CategoryAllocation
{
    /// <summary>
    /// The category name.
    /// </summary>
    public required string Category { get; set; }

    /// <summary>
    /// Total spending for this category across all years.
    /// </summary>
    public decimal TotalSpend { get; set; }

    /// <summary>
    /// Percentage of total expenditures allocated to this category.
    /// </summary>
    public decimal PercentOfTotal { get; set; }

    /// <summary>
    /// Number of components in this category.
    /// </summary>
    public int ComponentCount { get; set; }
}
