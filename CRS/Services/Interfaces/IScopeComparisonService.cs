using CRS.Models.Workflow;

namespace CRS.Services.Interfaces;

/// <summary>
/// Result of a scope comparison operation.
/// </summary>
public class ScopeComparisonResult
{
    /// <summary>Whether the comparison was successful.</summary>
    public bool IsSuccess { get; set; }

    /// <summary>The scope comparison record.</summary>
    public ScopeComparison? Comparison { get; set; }

    /// <summary>Whether the variance exceeds configured thresholds.</summary>
    public bool ExceedsThreshold { get; set; }

    /// <summary>Whether workflow should be blocked pending resolution.</summary>
    public bool ShouldBlockWorkflow { get; set; }

    /// <summary>Message describing the result.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Error message if comparison failed.</summary>
    public string? ErrorMessage { get; set; }

    public static ScopeComparisonResult Success(ScopeComparison comparison, bool exceedsThreshold, bool shouldBlock, string message)
    {
        return new ScopeComparisonResult
        {
            IsSuccess = true,
            Comparison = comparison,
            ExceedsThreshold = exceedsThreshold,
            ShouldBlockWorkflow = shouldBlock,
            Message = message
        };
    }

    public static ScopeComparisonResult Failure(string error)
    {
        return new ScopeComparisonResult
        {
            IsSuccess = false,
            ErrorMessage = error,
            Message = error
        };
    }
}

/// <summary>
/// Service for managing scope comparisons between original (HOA estimate) 
/// and actual (site visit) element counts.
/// </summary>
public interface IScopeComparisonService
{
    /// <summary>
    /// Captures the original scope from the study request when proposal is accepted.
    /// </summary>
    /// <param name="reserveStudyId">The reserve study ID.</param>
    /// <returns>The created scope comparison record.</returns>
    Task<ScopeComparison> CaptureOriginalScopeAsync(Guid reserveStudyId);

    /// <summary>
    /// Compares current element counts against original scope after site visit.
    /// Updates the scope comparison record and evaluates against thresholds.
    /// </summary>
    /// <param name="reserveStudyId">The reserve study ID.</param>
    /// <param name="userId">The user performing the comparison.</param>
    /// <returns>Result indicating whether variance exceeds thresholds.</returns>
    Task<ScopeComparisonResult> CompareAndEvaluateAsync(Guid reserveStudyId, Guid userId);

    /// <summary>
    /// Gets the scope comparison for a study, if one exists.
    /// </summary>
    /// <param name="reserveStudyId">The reserve study ID.</param>
    /// <returns>The scope comparison record or null.</returns>
    Task<ScopeComparison?> GetByStudyIdAsync(Guid reserveStudyId);

    /// <summary>
    /// Gets the tenant's scope change settings.
    /// </summary>
    /// <param name="tenantId">The tenant ID.</param>
    /// <returns>The settings, or default settings if not configured.</returns>
    Task<TenantScopeChangeSettings> GetSettingsAsync(int tenantId);

    /// <summary>
    /// Updates the tenant's scope change settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveSettingsAsync(TenantScopeChangeSettings settings);

    /// <summary>
    /// Allows staff to override a variance threshold and proceed with the study.
    /// </summary>
    /// <param name="scopeComparisonId">The scope comparison ID.</param>
    /// <param name="userId">The user performing the override.</param>
    /// <param name="reason">Reason for the override.</param>
    Task<ScopeComparison> OverrideVarianceAsync(Guid scopeComparisonId, Guid userId, string reason);

    /// <summary>
    /// Updates the comparison status (e.g., when amendment is sent/accepted/rejected).
    /// </summary>
    /// <param name="scopeComparisonId">The scope comparison ID.</param>
    /// <param name="newStatus">The new status.</param>
    /// <param name="notes">Optional notes.</param>
    Task<ScopeComparison> UpdateStatusAsync(Guid scopeComparisonId, ScopeComparisonStatus newStatus, string? notes = null);

    // ─────────────────────────────────────────────────────────────
    // Amendment workflow methods
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Marks that an amendment proposal has been sent to HOA for approval.
    /// Updates the scope comparison status and links to the amendment proposal.
    /// Also transitions the workflow to AmendmentPending stage.
    /// </summary>
    /// <param name="scopeComparisonId">The scope comparison ID.</param>
    /// <param name="amendmentProposalId">The ID of the amendment proposal.</param>
    /// <param name="actor">The user who sent the amendment.</param>
    Task<ScopeComparison> MarkAmendmentSentAsync(Guid scopeComparisonId, Guid amendmentProposalId, string? actor = null);

    /// <summary>
    /// Records HOA acceptance of the amendment proposal.
    /// Updates the scope comparison status and workflow stage.
    /// </summary>
    /// <param name="scopeComparisonId">The scope comparison ID.</param>
    /// <param name="userId">The HOA user who accepted.</param>
    Task<ScopeComparison> AcceptAmendmentAsync(Guid scopeComparisonId, Guid userId);

    /// <summary>
    /// Records HOA rejection of the amendment proposal.
    /// </summary>
    /// <param name="scopeComparisonId">The scope comparison ID.</param>
    /// <param name="userId">The HOA user who rejected.</param>
    /// <param name="reason">Reason for rejection.</param>
    Task<ScopeComparison> RejectAmendmentAsync(Guid scopeComparisonId, Guid userId, string reason);

    /// <summary>
        /// Gets all pending amendments for a specific HOA user.
        /// Used for dashboard indicators.
        /// </summary>
        /// <param name="userId">The HOA user ID.</param>
        /// <returns>List of scope comparisons with pending amendments for this user's studies.</returns>
        Task<List<ScopeComparison>> GetPendingAmendmentsForUserAsync(Guid userId);

        /// <summary>
        /// Gets the scope comparison with full details including amendment proposal.
        /// </summary>
        /// <param name="scopeComparisonId">The scope comparison ID.</param>
        /// <returns>The scope comparison with navigation properties loaded.</returns>
        Task<ScopeComparison?> GetByIdWithDetailsAsync(Guid scopeComparisonId);

        /// <summary>
        /// Prepares for a revised amendment after the HOA rejected the original.
        /// Resets the scope comparison status to allow creating a new amendment.
        /// </summary>
        /// <param name="scopeComparisonId">The scope comparison ID.</param>
        /// <param name="userId">The staff user initiating the revision.</param>
        /// <returns>The updated scope comparison ready for a new amendment.</returns>
        Task<ScopeComparison> PrepareRevisedAmendmentAsync(Guid scopeComparisonId, Guid userId);
    }
