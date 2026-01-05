namespace CRS.Core.ReserveCalculator.Models;

/// <summary>
/// Schedule of expenditures generated from reserve components.
/// </summary>
public class ExpenditureSchedule
{
    /// <summary>
    /// Individual component schedules keyed by component ID or name.
    /// Each array is indexed by year (0-based, index 0 = year 1).
    /// </summary>
    public Dictionary<string, decimal[]> ComponentSchedules { get; set; } = new();

    /// <summary>
    /// Total expenditures per year (sum of all component schedules).
    /// Array is indexed by year (0-based, index 0 = year 1).
    /// </summary>
    public decimal[] TotalExpendituresByYear { get; set; } = Array.Empty<decimal>();

    /// <summary>
    /// Gets the total expenditures across all years.
    /// </summary>
    public decimal GrandTotal => TotalExpendituresByYear.Sum();

    /// <summary>
    /// Gets the number of years in the schedule.
    /// </summary>
    public int YearCount => TotalExpendituresByYear.Length;

    /// <summary>
    /// Gets the expenditure for a specific component in a specific year.
    /// </summary>
    /// <param name="componentKey">The component key (ID or name).</param>
    /// <param name="yearIndex">The year index (1-based).</param>
    /// <returns>The expenditure amount, or 0 if not found.</returns>
    public decimal GetComponentExpenditure(string componentKey, int yearIndex)
    {
        if (ComponentSchedules.TryGetValue(componentKey, out var schedule))
        {
            int arrayIndex = yearIndex - 1;
            if (arrayIndex >= 0 && arrayIndex < schedule.Length)
                return schedule[arrayIndex];
        }
        return 0m;
    }
}
