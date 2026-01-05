namespace CRS.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines how frequently contributions are made to the reserve fund.
/// </summary>
public enum ContributionFrequency
{
    /// <summary>
    /// A single annual contribution is made each year.
    /// </summary>
    Annual,

    /// <summary>
    /// Contributions are made monthly (annual amount / 12 per month).
    /// </summary>
    Monthly
}
