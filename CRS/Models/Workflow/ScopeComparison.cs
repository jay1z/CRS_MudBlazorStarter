using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Models;
using CRS.Services.Tenant;

namespace CRS.Models.Workflow;

/// <summary>
/// Status of a scope comparison between original (HOA estimate) and actual (site visit) counts.
/// </summary>
public enum ScopeComparisonStatus
{
    /// <summary>Not yet compared (site visit not complete).</summary>
    Pending = 0,

    /// <summary>Comparison done, variance is within threshold.</summary>
    WithinThreshold = 1,

    /// <summary>Comparison done, variance exceeds threshold - needs review.</summary>
    ExceedsThreshold = 2,

    /// <summary>Amendment has been sent to HOA for approval.</summary>
    AmendmentPending = 3,

    /// <summary>HOA accepted the amendment.</summary>
    AmendmentAccepted = 4,

    /// <summary>HOA rejected the amendment - needs discussion.</summary>
    AmendmentRejected = 5,

    /// <summary>Staff overrode the variance check and proceeded.</summary>
    Overridden = 6
}

/// <summary>
/// Tracks the comparison between original scope (from HOA request) 
/// and actual scope (discovered during site visit).
/// </summary>
[Table("ScopeComparisons")]
public class ScopeComparison : ITenantScoped
{
    [Key]
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public int TenantId { get; set; }

    /// <summary>
    /// The reserve study this comparison is for.
    /// </summary>
    public Guid ReserveStudyId { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Original scope (from HOA request, captured at proposal acceptance)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Original building element count from HOA estimate.</summary>
    public int OriginalBuildingElementCount { get; set; }

    /// <summary>Original common element count from HOA estimate.</summary>
    public int OriginalCommonElementCount { get; set; }

    /// <summary>Original additional element count from HOA estimate.</summary>
    public int OriginalAdditionalElementCount { get; set; }

    /// <summary>Total original element count.</summary>
    [NotMapped]
    public int OriginalTotalCount => OriginalBuildingElementCount + 
                                      OriginalCommonElementCount + 
                                      OriginalAdditionalElementCount;

    /// <summary>When the original scope was captured.</summary>
    public DateTime OriginalCapturedAt { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Actual scope (discovered during site visit)
    // ─────────────────────────────────────────────────────────────

    /// <summary>Actual building element count from site visit.</summary>
    public int ActualBuildingElementCount { get; set; }

    /// <summary>Actual common element count from site visit.</summary>
    public int ActualCommonElementCount { get; set; }

    /// <summary>Actual additional element count from site visit.</summary>
    public int ActualAdditionalElementCount { get; set; }

    /// <summary>Total actual element count.</summary>
    [NotMapped]
    public int ActualTotalCount => ActualBuildingElementCount + 
                                   ActualCommonElementCount + 
                                   ActualAdditionalElementCount;

    // ─────────────────────────────────────────────────────────────
    // Variance calculations
    // ─────────────────────────────────────────────────────────────

    /// <summary>Absolute variance (positive = more elements found).</summary>
    [NotMapped]
    public int VarianceCount => ActualTotalCount - OriginalTotalCount;

    /// <summary>Variance as a percentage of original count.</summary>
    public decimal VariancePercent => OriginalTotalCount > 0 
        ? Math.Round((decimal)VarianceCount / OriginalTotalCount * 100, 2)
        : 0;

    /// <summary>Whether variance exceeds the configured thresholds.</summary>
    public bool ExceedsThreshold(TenantScopeChangeSettings settings)
    {
        if (settings.Mode == ScopeChangeMode.NoAction)
            return false;

        var absVariance = Math.Abs(VarianceCount);
        var absPercent = Math.Abs(VariancePercent);

        // Exceeds if EITHER threshold is exceeded
        var exceedsPercent = settings.VarianceThresholdPercent > 0 && 
                             absPercent >= settings.VarianceThresholdPercent;
        var exceedsCount = settings.VarianceThresholdCount > 0 && 
                           absVariance >= settings.VarianceThresholdCount;

        return exceedsPercent || exceedsCount;
    }

    // ─────────────────────────────────────────────────────────────
    // Status and audit
    // ─────────────────────────────────────────────────────────────

    /// <summary>Current status of the comparison.</summary>
    public ScopeComparisonStatus Status { get; set; } = ScopeComparisonStatus.Pending;

    /// <summary>When the comparison was performed.</summary>
    public DateTime? ComparedAt { get; set; }

    /// <summary>User who performed the comparison (typically staff completing site visit).</summary>
    public Guid? ComparedByUserId { get; set; }

    /// <summary>Notes about the variance or decision.</summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>If overridden, who overrode it.</summary>
    public Guid? OverriddenByUserId { get; set; }

    /// <summary>If overridden, when.</summary>
    public DateTime? OverriddenAt { get; set; }

    /// <summary>If overridden, the reason.</summary>
    [MaxLength(1000)]
    public string? OverrideReason { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Amendment tracking
    // ─────────────────────────────────────────────────────────────

    /// <summary>The amendment proposal created due to scope variance.</summary>
    public Guid? AmendmentProposalId { get; set; }

    /// <summary>When the amendment was accepted by HOA.</summary>
    public DateTime? AmendmentAcceptedAt { get; set; }

    /// <summary>User who accepted the amendment (HOA representative).</summary>
    public Guid? AmendmentAcceptedByUserId { get; set; }

    /// <summary>When the amendment was rejected by HOA.</summary>
    public DateTime? AmendmentRejectedAt { get; set; }

    /// <summary>Reason provided by HOA for rejecting the amendment.</summary>
    [MaxLength(1000)]
    public string? AmendmentRejectionReason { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Navigation
    // ─────────────────────────────────────────────────────────────

    /// <summary>Navigation to the reserve study.</summary>
    [ForeignKey(nameof(ReserveStudyId))]
    public ReserveStudy? ReserveStudy { get; set; }

    /// <summary>Navigation to the amendment proposal.</summary>
    [ForeignKey(nameof(AmendmentProposalId))]
    public Proposal? AmendmentProposal { get; set; }
}
