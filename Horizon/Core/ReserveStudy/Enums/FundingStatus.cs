namespace Horizon.Core.ReserveCalculator.Enums;

/// <summary>
/// Funding status classification based on percent funded.
/// Industry-standard thresholds per National Reserve Study Standards (NRSS).
/// </summary>
public enum FundingStatus
{
    /// <summary>
    /// Strong funding: 70% or higher of fully funded balance.
    /// Low risk of special assessment.
    /// </summary>
    Strong,

    /// <summary>
    /// Fair funding: 30% to 69% of fully funded balance.
    /// Moderate risk, should consider increasing contributions.
    /// </summary>
    Fair,

    /// <summary>
    /// Weak funding: Below 30% of fully funded balance.
    /// High risk of special assessment or deferred maintenance.
    /// </summary>
    Weak
}
