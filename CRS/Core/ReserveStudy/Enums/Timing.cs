namespace Horizon.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines when during a period (year or month) a transaction occurs.
/// </summary>
public enum Timing
{
    /// <summary>
    /// Transaction occurs at the beginning of the period.
    /// </summary>
    StartOfPeriod,

    /// <summary>
    /// Transaction occurs at the midpoint of the period.
    /// </summary>
    MidPeriod,

    /// <summary>
    /// Transaction occurs at the end of the period.
    /// </summary>
    EndOfPeriod
}
