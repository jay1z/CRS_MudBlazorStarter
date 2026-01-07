namespace CRS.Models.Workflow;

/// <summary>
/// Defines how a tenant handles scope changes detected after a site visit.
/// </summary>
public enum ScopeChangeMode
{
    /// <summary>
    /// No action - proceed with original proposal regardless of variance.
    /// </summary>
    NoAction = 0,

    /// <summary>
    /// Show variance report to staff, staff decides action.
    /// </summary>
    VarianceReportOnly = 1,

    /// <summary>
    /// Show variance, require amendment if threshold exceeded.
    /// </summary>
    VarianceWithAmendment = 2,

    /// <summary>
    /// Always require formal amendment for any scope change.
    /// </summary>
    AlwaysAmendment = 3,

    /// <summary>
    /// Two-phase: Site visit proposal first, then priced study proposal.
    /// </summary>
    TwoPhase = 4
}
