namespace CRS.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines the expenditure calculation method for a reserve component.
/// </summary>
public enum ComponentMethod
{
    /// <summary>
    /// Replacement method: Component is replaced at end of useful life,
    /// then every UsefulLifeYears thereafter.
    /// </summary>
    Replacement,

    /// <summary>
    /// Periodic Repair/Maintenance (PRN) method: Recurring costs at regular intervals.
    /// If CycleYears = 1, cost occurs every year.
    /// </summary>
    PRN,

    /// <summary>
    /// Combination of Replacement and PRN methods.
    /// Both schedules are calculated and combined.
    /// </summary>
    Combo
}
