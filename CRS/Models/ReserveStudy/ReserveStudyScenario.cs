using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Core.ReserveCalculator.Enums;
using CRS.Services.Tenant;

namespace CRS.Models.ReserveStudyCalculator;

/// <summary>
/// Status of a reserve study scenario.
/// </summary>
public enum ScenarioStatus
{
    /// <summary>
    /// Scenario is being edited.
    /// </summary>
    Draft,

    /// <summary>
    /// Scenario is finalized and published.
    /// </summary>
    Published,

    /// <summary>
    /// Scenario is archived (historical).
    /// </summary>
    Archived
}

/// <summary>
/// A calculation scenario within a reserve study.
/// Each study can have multiple scenarios with different assumptions.
/// Nullable override fields inherit from tenant defaults when null.
/// </summary>
[Table("ReserveStudyScenarios")]
public class ReserveStudyScenario : ITenantScoped
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }

    /// <summary>
    /// The reserve study this scenario belongs to.
    /// Changed to Guid to match ReserveStudy.Id type.
    /// </summary>
    public Guid ReserveStudyId { get; set; }

    /// <summary>
    /// Name of this scenario (e.g., "Base Case", "Conservative", "Aggressive Funding").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = "Default Scenario";

    /// <summary>
    /// Current status of the scenario.
    /// </summary>
    public ScenarioStatus Status { get; set; } = ScenarioStatus.Draft;

    /// <summary>
    /// Description or notes about this scenario.
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    #region Required Fields (always scenario-specific)

    /// <summary>
    /// First calendar year of the projection.
    /// </summary>
    public int StartYear { get; set; }

    /// <summary>
    /// Starting reserve fund balance.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal StartingBalance { get; set; }

    #endregion

    #region Override Fields (null = use tenant default)

    /// <summary>
    /// Override for projection years. Null = use tenant default.
    /// </summary>
    public int? OverrideProjectionYears { get; set; }

    /// <summary>
    /// Override for inflation rate. Null = use tenant default.
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal? OverrideInflationRate { get; set; }

    /// <summary>
    /// Override for interest rate. Null = use tenant default.
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal? OverrideInterestRateAnnual { get; set; }

    /// <summary>
    /// Override for interest model. Null = use tenant default.
    /// </summary>
    public InterestModel? OverrideInterestModel { get; set; }

    /// <summary>
    /// Override for contribution strategy. Null = use tenant default.
    /// </summary>
    public ContributionStrategy? OverrideContributionStrategy { get; set; }

    /// <summary>
    /// Override for initial contribution. Null = use tenant default.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OverrideInitialAnnualContribution { get; set; }

    /// <summary>
    /// Override for escalation rate. Null = use tenant default.
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal? OverrideContributionEscalationRate { get; set; }

    /// <summary>
    /// Override for contribution frequency. Null = use tenant default.
    /// </summary>
    public ContributionFrequency? OverrideContributionFrequency { get; set; }

    /// <summary>
    /// Override for contribution timing. Null = use tenant default.
    /// </summary>
    public Timing? OverrideContributionTiming { get; set; }

    /// <summary>
    /// Override for expenditure timing. Null = use tenant default.
    /// </summary>
    public ExpenditureTiming? OverrideExpenditureTiming { get; set; }

    /// <summary>
    /// Override for rounding policy. Null = use tenant default.
    /// </summary>
    public RoundingPolicy? OverrideRoundingPolicy { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// When this scenario was created.
    /// </summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this scenario was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User who created this scenario.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }

    /// <summary>
    /// User who last modified this scenario.
    /// </summary>
    public Guid? ModifiedByUserId { get; set; }

    /// <summary>
    /// Soft delete timestamp.
    /// </summary>
    public DateTime? DateDeleted { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// The parent reserve study.
    /// </summary>
    [ForeignKey(nameof(ReserveStudyId))]
    public virtual Models.ReserveStudy? ReserveStudy { get; set; }

    /// <summary>
    /// Components included in this scenario.
    /// </summary>
    public virtual ICollection<ReserveScenarioComponent> Components { get; set; } = new List<ReserveScenarioComponent>();

    #endregion

    /// <summary>
    /// Checks if a specific setting is overridden in this scenario.
    /// </summary>
    public bool HasOverride(string settingName) => settingName switch
    {
        nameof(OverrideProjectionYears) => OverrideProjectionYears.HasValue,
        nameof(OverrideInflationRate) => OverrideInflationRate.HasValue,
        nameof(OverrideInterestRateAnnual) => OverrideInterestRateAnnual.HasValue,
        nameof(OverrideInterestModel) => OverrideInterestModel.HasValue,
        nameof(OverrideContributionStrategy) => OverrideContributionStrategy.HasValue,
        nameof(OverrideInitialAnnualContribution) => OverrideInitialAnnualContribution.HasValue,
        nameof(OverrideContributionEscalationRate) => OverrideContributionEscalationRate.HasValue,
        nameof(OverrideContributionFrequency) => OverrideContributionFrequency.HasValue,
        nameof(OverrideContributionTiming) => OverrideContributionTiming.HasValue,
        nameof(OverrideExpenditureTiming) => OverrideExpenditureTiming.HasValue,
        nameof(OverrideRoundingPolicy) => OverrideRoundingPolicy.HasValue,
        _ => false
    };
}
