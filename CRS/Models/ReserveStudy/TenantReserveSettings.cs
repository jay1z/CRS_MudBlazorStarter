using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Core.ReserveCalculator.Enums;
using CRS.Services.Tenant;

namespace CRS.Models.ReserveStudyCalculator;

/// <summary>
/// Tenant-specific default settings for reserve study calculations.
/// These defaults can be overridden at the scenario level.
/// </summary>
[Table("TenantReserveSettings")]
public class TenantReserveSettings : ITenantScoped
{
    /// <summary>
    /// Primary key - matches TenantId (one settings row per tenant).
    /// </summary>
    [Key]
    public int TenantId { get; set; }

    /// <summary>
    /// Default number of years for projections (typically 30).
    /// </summary>
    public int DefaultProjectionYears { get; set; } = 30;

    /// <summary>
    /// Default annual inflation rate for component costs (e.g., 0.03 = 3%).
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal DefaultInflationRate { get; set; } = 0.03m;

    /// <summary>
    /// Default annual interest rate on reserve fund balance (e.g., 0.02 = 2%).
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal DefaultInterestRateAnnual { get; set; } = 0.02m;

    /// <summary>
    /// Default interest calculation model.
    /// </summary>
    public InterestModel DefaultInterestModel { get; set; } = InterestModel.MonthlySimulation;

    /// <summary>
    /// Default contribution strategy.
    /// </summary>
    public ContributionStrategy DefaultContributionStrategy { get; set; } = ContributionStrategy.EscalatingPercent;

    /// <summary>
    /// Default initial annual contribution amount.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal DefaultInitialAnnualContribution { get; set; } = 50000m;

    /// <summary>
    /// Default annual escalation rate for contributions (e.g., 0.03 = 3%).
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal DefaultContributionEscalationRate { get; set; } = 0.03m;

    /// <summary>
    /// Default contribution frequency.
    /// </summary>
    public ContributionFrequency DefaultContributionFrequency { get; set; } = ContributionFrequency.Monthly;

    /// <summary>
    /// Default timing for contributions within the period.
    /// </summary>
    public Timing DefaultContributionTiming { get; set; } = Timing.StartOfPeriod;

    /// <summary>
    /// Default timing for expenditures within the year.
    /// </summary>
    public ExpenditureTiming DefaultExpenditureTiming { get; set; } = ExpenditureTiming.MidYear;

    /// <summary>
    /// Default rounding policy for calculations.
    /// </summary>
    public RoundingPolicy DefaultRoundingPolicy { get; set; } = RoundingPolicy.PerComponentPerYear;

    /// <summary>
    /// When these settings were last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who last updated these settings.
    /// </summary>
    public Guid? UpdatedByUserId { get; set; }

    /// <summary>
    /// Navigation to tenant.
    /// </summary>
    [ForeignKey(nameof(TenantId))]
    public virtual Models.Tenant? Tenant { get; set; }
}
