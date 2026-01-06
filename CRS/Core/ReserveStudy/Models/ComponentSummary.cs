namespace CRS.Core.ReserveCalculator.Models;

/// <summary>
/// Summary information for a single component, used in reports and narratives.
/// Contains calculated values derived from the expenditure schedule.
/// </summary>
public class ComponentSummary
{
    /// <summary>
    /// Component identifier (from ReserveComponentInput.Id or Name).
    /// </summary>
    public required string ComponentKey { get; set; }

    /// <summary>
    /// Display name of the component.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Category the component belongs to.
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Current replacement/repair cost in today's dollars.
    /// </summary>
    public decimal CurrentCost { get; set; }

    /// <summary>
    /// Total useful life in years.
    /// </summary>
    public int? UsefulLifeYears { get; set; }

    /// <summary>
    /// Remaining life in years from the study start date.
    /// Negative values indicate the component is past due.
    /// </summary>
    public int? RemainingLifeYears { get; set; }

    /// <summary>
    /// The calendar year of the next scheduled expenditure.
    /// Null if no expenditure is scheduled within the projection period.
    /// </summary>
    public int? NextExpenditureYear { get; set; }

    /// <summary>
    /// The inflated cost of the next scheduled expenditure.
    /// </summary>
    public decimal? NextExpenditureCost { get; set; }

    /// <summary>
    /// Total expenditures for this component over the projection period.
    /// </summary>
    public decimal TotalProjectedExpenditures { get; set; }

    /// <summary>
    /// Number of times this component has expenditures during the projection.
    /// </summary>
    public int ExpenditureCount { get; set; }

    /// <summary>
    /// The fully funded balance (ideal reserve) for this component.
    /// Represents the accumulated depreciation-based reserve.
    /// Formula: CurrentCost × (Age / UsefulLife)
    /// </summary>
    public decimal FullyFundedBalance { get; set; }

    /// <summary>
    /// Current accumulated reserve allocation for this component.
    /// Based on proportional share of starting balance.
    /// </summary>
    public decimal CurrentReserve { get; set; }

    /// <summary>
    /// Percent funded for this component (CurrentReserve / FullyFundedBalance × 100).
    /// </summary>
    public decimal PercentFunded => FullyFundedBalance > 0
        ? Money.Round2((CurrentReserve / FullyFundedBalance) * 100m)
        : 100m; // If no reserve needed, consider fully funded

    /// <summary>
    /// The component's age in years (time since last service).
    /// </summary>
    public int? AgeYears { get; set; }

    /// <summary>
    /// Indicates if the component is past its expected useful life.
    /// </summary>
    public bool IsPastDue => RemainingLifeYears.HasValue && RemainingLifeYears.Value < 0;

    /// <summary>
    /// Indicates if the component is due within the next 5 years.
    /// </summary>
    public bool IsDueSoon => RemainingLifeYears.HasValue 
        && RemainingLifeYears.Value >= 0 
        && RemainingLifeYears.Value <= 5;
}
