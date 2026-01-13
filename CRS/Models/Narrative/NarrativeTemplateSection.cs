using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Services.Tenant;

namespace CRS.Models.NarrativeTemplates;

/// <summary>
/// Represents a section in the narrative report template.
/// Sections are the major divisions of the report (Cover, Executive Summary, Narrative, Exhibits, etc.).
/// TenantId = null indicates a global default that can be overridden by tenant-specific entries.
/// </summary>
public class NarrativeTemplateSection : BaseModel, ITenantScoped
{
    /// <summary>
    /// Tenant ID (null = global default).
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Unique key identifying the section (e.g., "Cover", "ExecutiveSummary", "Narrative", "Exhibits").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string SectionKey { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the section.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Whether this section is enabled for rendering.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Sort order for rendering (lower = earlier).
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Optional description or notes about this section.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Whether this section should start on a new page.
    /// </summary>
    public bool PageBreakBefore { get; set; } = false;

    /// <summary>
    /// CSS class to apply to the section wrapper.
    /// </summary>
    [MaxLength(200)]
    public string? CssClass { get; set; }

    /// <summary>
    /// Navigation blocks for this section.
    /// </summary>
    public List<NarrativeTemplateBlock>? Blocks { get; set; }
}

/// <summary>
/// Standard section keys used in reserve study narratives.
/// </summary>
public static class NarrativeSectionKeys
{
    public const string Cover = "Cover";
    public const string ExhibitsIntro = "ExhibitsIntro";
    public const string ExecutiveSummary = "ExecutiveSummary";
    public const string FundingSummary = "FundingSummary";
    public const string SignaturePage = "SignaturePage";
    public const string NarrativeEvaluation = "NarrativeEvaluation";
    public const string NarrativeAssumptions = "NarrativeAssumptions";
    public const string NarrativeOperatingVsReserves = "NarrativeOperatingVsReserves";
    public const string NarrativeSources = "NarrativeSources";
    public const string NarrativeComponents = "NarrativeComponents";
    public const string NarrativeCharts = "NarrativeCharts";
    public const string NarrativeConclusion = "NarrativeConclusion";
    public const string InfoFurnished = "InfoFurnished";
    public const string Definitions = "Definitions";
    public const string ExhibitPhotos = "ExhibitPhotos";
    public const string ExhibitTables = "ExhibitTables";
    public const string ExhibitCharts = "ExhibitCharts";
    public const string ExhibitDisclosures = "ExhibitDisclosures";
}
