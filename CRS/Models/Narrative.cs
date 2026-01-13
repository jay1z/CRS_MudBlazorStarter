using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Represents the narrative content for a reserve study report.
/// Contains section-based text content that accompanies the financial analysis.
/// </summary>
public class Narrative : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Required link to reserve study
    [Required]
    [ForeignKey(nameof(ReserveStudy))]
    public Guid ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    // Version tracking
    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = "1.0";

    // Status
    public NarrativeStatus Status { get; set; } = NarrativeStatus.Draft;

    // ═══════════════════════════════════════════════════════════════
    // NARRATIVE SECTIONS (Rich text/HTML content)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Executive summary - high-level overview for board members.
    /// Typically 1-2 paragraphs summarizing key findings and recommendations.
    /// </summary>
    public string? ExecutiveSummary { get; set; }

    /// <summary>
    /// Introduction section - purpose and scope of the study.
    /// </summary>
    public string? Introduction { get; set; }

    /// <summary>
    /// Property description - details about the community/property.
    /// Includes location, age, number of units, building types, etc.
    /// </summary>
    public string? PropertyDescription { get; set; }

    /// <summary>
    /// Methodology - how the study was conducted.
    /// Explains the approach, site visit details, data gathering methods.
    /// </summary>
    public string? Methodology { get; set; }

    /// <summary>
    /// Findings - detailed analysis results.
    /// Component conditions, observations from site visit, key concerns.
    /// </summary>
    public string? Findings { get; set; }

    /// <summary>
    /// Funding analysis - explanation of the funding plan calculations.
    /// Discusses contribution rates, funding methods, projections.
    /// </summary>
    public string? FundingAnalysis { get; set; }

    /// <summary>
    /// Recommendations - suggested actions for the association.
    /// Prioritized list of maintenance items, funding adjustments, etc.
    /// </summary>
    public string? Recommendations { get; set; }

    /// <summary>
    /// Conclusion - closing remarks and next steps.
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// Additional notes - any supplementary information.
    /// </summary>
    public string? AdditionalNotes { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // TEMPLATE TRACKING
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// The template used to initialize this narrative, if any.
    /// </summary>
    [MaxLength(100)]
    public string? TemplateUsed { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // AUTHORSHIP AND REVIEW
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// User who created/is writing the narrative.
    /// </summary>
    [ForeignKey(nameof(Author))]
    public Guid? AuthorUserId { get; set; }
    public ApplicationUser? Author { get; set; }

    /// <summary>
    /// When the narrative was started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the narrative was marked complete.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who completed/finalized the narrative.
    /// </summary>
    [ForeignKey(nameof(CompletedBy))]
    public Guid? CompletedByUserId { get; set; }
    public ApplicationUser? CompletedBy { get; set; }

    /// <summary>
    /// When the narrative was last reviewed.
    /// </summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>
    /// User who reviewed the narrative.
    /// </summary>
    [ForeignKey(nameof(ReviewedBy))]
    public Guid? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    /// <summary>
    /// Internal review notes/feedback.
    /// </summary>
    [MaxLength(2000)]
    public string? ReviewNotes { get; set; }

    // ═══════════════════════════════════════════════════════════════
    // WORD COUNT TRACKING
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Total word count across all sections.
    /// Updated when content is saved.
    /// </summary>
    public int TotalWordCount { get; set; }

    /// <summary>
    /// Calculates the total word count from all sections.
    /// </summary>
    public int CalculateWordCount()
    {
        var allText = string.Join(" ",
            ExecutiveSummary ?? "",
            Introduction ?? "",
            PropertyDescription ?? "",
            Methodology ?? "",
            Findings ?? "",
            FundingAnalysis ?? "",
            Recommendations ?? "",
            Conclusion ?? "",
            AdditionalNotes ?? "");

        // Strip HTML tags for accurate count
        var plainText = System.Text.RegularExpressions.Regex.Replace(allText, "<[^>]*>", " ");
        var words = plainText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    /// <summary>
    /// Checks if all required sections have content.
    /// </summary>
    public bool HasRequiredSections()
    {
        return !string.IsNullOrWhiteSpace(ExecutiveSummary) &&
               !string.IsNullOrWhiteSpace(Introduction) &&
               !string.IsNullOrWhiteSpace(PropertyDescription) &&
               !string.IsNullOrWhiteSpace(Findings) &&
               !string.IsNullOrWhiteSpace(Recommendations) &&
               !string.IsNullOrWhiteSpace(Conclusion);
    }

    /// <summary>
    /// Gets a list of sections that are missing content.
    /// </summary>
    public List<string> GetMissingSections()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(ExecutiveSummary)) missing.Add("Executive Summary");
        if (string.IsNullOrWhiteSpace(Introduction)) missing.Add("Introduction");
        if (string.IsNullOrWhiteSpace(PropertyDescription)) missing.Add("Property Description");
        if (string.IsNullOrWhiteSpace(Findings)) missing.Add("Findings");
        if (string.IsNullOrWhiteSpace(Recommendations)) missing.Add("Recommendations");
        if (string.IsNullOrWhiteSpace(Conclusion)) missing.Add("Conclusion");
        return missing;
    }
}

/// <summary>
/// Status of a narrative in the creation/review workflow.
/// </summary>
public enum NarrativeStatus
{
    /// <summary>
    /// Narrative is being drafted.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Narrative is in progress (some sections complete).
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Narrative submitted for internal review.
    /// </summary>
    PendingReview = 2,

    /// <summary>
    /// Narrative is being reviewed.
    /// </summary>
    InReview = 3,

    /// <summary>
    /// Revisions requested by reviewer.
    /// </summary>
    RevisionRequired = 4,

    /// <summary>
    /// Narrative approved and ready for report generation.
    /// </summary>
    Approved = 5,

    /// <summary>
    /// Narrative has been included in a published report.
    /// </summary>
    Published = 6,

    /// <summary>
    /// Narrative was superseded by a newer version.
    /// </summary>
    Superseded = 7,

    /// <summary>
    /// Narrative was archived.
    /// </summary>
    Archived = 8
}
