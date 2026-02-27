namespace Horizon.Core.ReserveCalculator.Enums;

/// <summary>
/// Defines the strategy for calculating annual contributions to the reserve fund.
/// </summary>
public enum ContributionStrategy
{
    /// <summary>
    /// A fixed annual contribution amount is used each year.
    /// </summary>
    FixedAnnual,

    /// <summary>
    /// Contributions escalate by a fixed percentage each year.
    /// Year N contribution = Initial * (1 + EscalationRate)^(N-1)
    /// </summary>
    EscalatingPercent,

    /// <summary>
    /// Contributions are dynamically adjusted to maintain a non-negative balance.
    /// If projected ending balance would be negative, contribution is increased.
    /// </summary>
    MaintainNonNegativeBalance
}
