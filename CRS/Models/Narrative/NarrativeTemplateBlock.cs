using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Services.Tenant;

namespace Horizon.Models.NarrativeTemplates;

/// <summary>
/// Represents a block of HTML content within a narrative section.
/// Blocks are the atomic units of template content that can be individually enabled/disabled.
/// TenantId = null indicates a global default that can be overridden by tenant-specific entries.
/// </summary>
public class NarrativeTemplateBlock : BaseModel, ITenantScoped
{
    /// <summary>
    /// Tenant ID (null = global default).
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Section key this block belongs to.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SectionKey { get; set; } = string.Empty;

    /// <summary>
    /// Unique key identifying the block within its section (e.g., "CoverTitle", "ExecutiveSummaryIntro").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string BlockKey { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the block.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// The HTML template content with placeholders.
    /// Placeholders use {PropertyName} syntax (e.g., {AssociationName}, {CityState}).
    /// Special tokens: [[PAGE_BREAK]], [[TABLE:ContributionSchedule]], [[SIGNATURES]], etc.
    /// </summary>
    [Required]
    public string HtmlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Whether this block is enabled for rendering.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Sort order within the section (lower = earlier).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional JSON condition for conditional rendering.
    /// Example: {"studyType": "Full"} - only render for full studies.
    /// </summary>
    public string? AppliesWhenJson { get; set; }

    /// <summary>
    /// Optional description or notes about this block.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// CSS class to apply to this block's wrapper.
    /// </summary>
    [MaxLength(200)]
    public string? CssClass { get; set; }

    /// <summary>
    /// Parent section navigation property.
    /// </summary>
    [ForeignKey("SectionId")]
    public NarrativeTemplateSection? Section { get; set; }
    public Guid? SectionId { get; set; }
}

/// <summary>
/// Standard block keys for common narrative content blocks.
/// </summary>
public static class NarrativeBlockKeys
{
    // Cover
    public const string CoverLogo = "CoverLogo";
    public const string CoverTitle = "CoverTitle";
    public const string CoverImage = "CoverImage";
    public const string CoverAssociationInfo = "CoverAssociationInfo";
    public const string CoverCompanyInfo = "CoverCompanyInfo";

    // Executive Summary
    public const string ExecSummaryIntro = "ExecSummaryIntro";
    public const string ExecSummaryFindings = "ExecSummaryFindings";
    public const string ExecSummaryRecommendations = "ExecSummaryRecommendations";

    // Funding Summary
    public const string FundStatusSummary = "FundStatusSummary";
    public const string ContributionScheduleTable = "ContributionScheduleTable";

    // Signature Page
    public const string SignatureIntro = "SignatureIntro";
    public const string SignatureBlocks = "SignatureBlocks";

    // Narrative Sections
    public const string EvaluationIntro = "EvaluationIntro";
    public const string EvaluationDetails = "EvaluationDetails";
    public const string AssumptionsInflation = "AssumptionsInflation";
    public const string AssumptionsInterest = "AssumptionsInterest";
    public const string OperatingVsReservesGuidance = "OperatingVsReservesGuidance";
    public const string SourcesDescription = "SourcesDescription";
    public const string ComponentsIntro = "ComponentsIntro";
    public const string ComponentsFourPartTest = "ComponentsFourPartTest";
    public const string ChartsExplanation = "ChartsExplanation";
    public const string ConclusionText = "ConclusionText";

    // Tables
    public const string InfoFurnishedTable = "InfoFurnishedTable";
    public const string DefinitionsGlossary = "DefinitionsGlossary";

    // Exhibits
    public const string PhotoGallery = "PhotoGallery";
    public const string SpreadsheetTables = "SpreadsheetTables";
    public const string ChartExhibits = "ChartExhibits";
    public const string DisclosuresLaws = "DisclosuresLaws";
    public const string VendorCredentials = "VendorCredentials";
}
