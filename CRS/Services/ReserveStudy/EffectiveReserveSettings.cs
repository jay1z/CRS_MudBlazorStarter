using CRS.Core.ReserveCalculator.Enums;

namespace CRS.Services.ReserveCalculator;

/// <summary>
/// Resolved effective settings for a reserve study scenario.
/// Combines tenant defaults with scenario-specific overrides.
/// </summary>
public class EffectiveReserveSettings
{
    /// <summary>
    /// First calendar year of the projection.
    /// </summary>
    public int StartYear { get; set; }

    /// <summary>
    /// Starting reserve fund balance.
    /// </summary>
    public decimal StartingBalance { get; set; }

    /// <summary>
    /// Number of years for projection.
    /// </summary>
    public int ProjectionYears { get; set; }

    /// <summary>
    /// Annual inflation rate for component costs.
    /// </summary>
    public decimal InflationRate { get; set; }

    /// <summary>
    /// Annual interest rate on fund balance.
    /// </summary>
    public decimal InterestRateAnnual { get; set; }

    /// <summary>
    /// Interest calculation model.
    /// </summary>
    public InterestModel InterestModel { get; set; }

    /// <summary>
    /// Contribution strategy.
    /// </summary>
    public ContributionStrategy ContributionStrategy { get; set; }

    /// <summary>
    /// Initial annual contribution amount.
    /// </summary>
    public decimal InitialAnnualContribution { get; set; }

    /// <summary>
    /// Contribution escalation rate.
    /// </summary>
    public decimal ContributionEscalationRate { get; set; }

    /// <summary>
    /// Contribution frequency.
    /// </summary>
    public ContributionFrequency ContributionFrequency { get; set; }

    /// <summary>
    /// Contribution timing within period.
    /// </summary>
    public Timing ContributionTiming { get; set; }

    /// <summary>
    /// Expenditure timing within year.
    /// </summary>
    public ExpenditureTiming ExpenditureTiming { get; set; }

    /// <summary>
    /// Rounding policy.
    /// </summary>
    public RoundingPolicy RoundingPolicy { get; set; }

    #region Source Tracking (for UI display)

    /// <summary>
    /// Tracks which settings are from tenant defaults vs scenario overrides.
    /// </summary>
    public Dictionary<string, SettingSource> Sources { get; set; } = new();

    #endregion
}

/// <summary>
/// Indicates the source of a setting value.
/// </summary>
public enum SettingSource
{
    /// <summary>
    /// Value comes from tenant default settings.
    /// </summary>
    TenantDefault,

    /// <summary>
    /// Value is overridden at the scenario level.
    /// </summary>
    ScenarioOverride,

    /// <summary>
    /// Value is a required scenario-specific field.
    /// </summary>
    ScenarioRequired
}
