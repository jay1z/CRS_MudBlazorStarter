using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service interface for managing narrative content for reserve studies.
/// </summary>
public interface INarrativeService
{
    // ═══════════════════════════════════════════════════════════════
    // CRUD OPERATIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the current (non-archived) narrative for a reserve study.
    /// </summary>
    Task<Narrative?> GetByStudyIdAsync(Guid studyId, CancellationToken ct = default);

    /// <summary>
    /// Gets a narrative by its ID.
    /// </summary>
    Task<Narrative?> GetByIdAsync(Guid narrativeId, CancellationToken ct = default);

    /// <summary>
    /// Gets all narratives for a reserve study, including archived versions.
    /// </summary>
    Task<IEnumerable<Narrative>> GetAllByStudyIdAsync(Guid studyId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new narrative for a reserve study.
    /// </summary>
    Task<Narrative> CreateAsync(Guid studyId, Guid authorUserId, string? templateName = null, CancellationToken ct = default);

    /// <summary>
    /// Updates a narrative's content.
    /// </summary>
    Task<bool> UpdateAsync(Narrative narrative, CancellationToken ct = default);

    /// <summary>
    /// Saves a specific section of the narrative.
    /// </summary>
    Task<bool> SaveSectionAsync(Guid narrativeId, string sectionName, string content, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a narrative.
    /// </summary>
    Task<bool> DeleteAsync(Guid narrativeId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // STATUS MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Submits a narrative for review.
    /// </summary>
    Task<bool> SubmitForReviewAsync(Guid narrativeId, CancellationToken ct = default);

    /// <summary>
    /// Approves a narrative.
    /// </summary>
    Task<bool> ApproveAsync(Guid narrativeId, Guid reviewerUserId, CancellationToken ct = default);

    /// <summary>
    /// Requests revisions to a narrative.
    /// </summary>
    Task<bool> RequestRevisionsAsync(Guid narrativeId, Guid reviewerUserId, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Marks a narrative as completed.
    /// </summary>
    Task<bool> CompleteAsync(Guid narrativeId, Guid completedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Marks a narrative as published (included in a report).
    /// </summary>
    Task<bool> MarkAsPublishedAsync(Guid narrativeId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // VALIDATION
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Validates that a narrative has all required sections.
    /// </summary>
    Task<NarrativeValidationResult> ValidateAsync(Guid narrativeId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a study has a completed/approved narrative ready for report generation.
    /// </summary>
    Task<bool> HasApprovedNarrativeAsync(Guid studyId, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // TEMPLATES
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets available narrative templates.
    /// </summary>
    IEnumerable<NarrativeTemplate> GetAvailableTemplates();

    /// <summary>
    /// Applies a template to a narrative, populating sections with template content.
    /// </summary>
    Task<bool> ApplyTemplateAsync(Guid narrativeId, string templateName, CancellationToken ct = default);

    // ═══════════════════════════════════════════════════════════════
    // VERSION MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new version of a narrative, superseding the previous one.
    /// </summary>
    Task<Narrative?> CreateNewVersionAsync(Guid existingNarrativeId, Guid authorUserId, CancellationToken ct = default);
}

/// <summary>
/// Result of narrative validation.
/// </summary>
public record NarrativeValidationResult
{
    public bool IsValid { get; init; }
    public List<string> MissingSections { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public int TotalWordCount { get; init; }
    public int MinimumWordCount { get; init; } = 500;
    public bool MeetsMinimumWordCount => TotalWordCount >= MinimumWordCount;
}

/// <summary>
/// Represents a narrative template with pre-filled content.
/// </summary>
public record NarrativeTemplate
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string? ExecutiveSummary { get; init; }
    public string? Introduction { get; init; }
    public string? PropertyDescription { get; init; }
    public string? Methodology { get; init; }
    public string? Findings { get; init; }
    public string? FundingAnalysis { get; init; }
    public string? Recommendations { get; init; }
    public string? Conclusion { get; init; }
}
