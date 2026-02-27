using System.ComponentModel.DataAnnotations;

using Horizon.Services.Tenant;

namespace Horizon.Models.NarrativeTemplates;

/// <summary>
/// Represents an optional insert paragraph that can be conditionally included in the narrative.
/// Inserts are feature-flagged content that can be enabled/disabled per tenant.
/// TenantId = null indicates a global default that can be overridden by tenant-specific entries.
/// </summary>
public class NarrativeInsert : BaseModel, ITenantScoped
{
    /// <summary>
    /// Tenant ID (null = global default).
    /// </summary>
    public int TenantId { get; set; }

    /// <summary>
    /// Unique key identifying the insert (e.g., "SpecialAssessmentWarning", "InflationDisclaimer").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string InsertKey { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the insert.
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// The HTML template content with placeholders.
    /// </summary>
    [Required]
    public string HtmlTemplate { get; set; } = string.Empty;

    /// <summary>
    /// Whether this insert is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Optional JSON condition for conditional rendering.
    /// Example: {"hasLoan": true} - only render when study has a loan.
    /// </summary>
    public string? AppliesWhenJson { get; set; }

    /// <summary>
    /// Optional description or notes about this insert.
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// The section key where this insert should be placed.
    /// </summary>
    [MaxLength(100)]
    public string? TargetSectionKey { get; set; }

    /// <summary>
    /// The block key after which this insert should appear.
    /// </summary>
    [MaxLength(100)]
    public string? InsertAfterBlockKey { get; set; }

    /// <summary>
    /// Sort order when multiple inserts target the same location.
    /// </summary>
    public int SortOrder { get; set; }
}

/// <summary>
/// Standard insert keys for common optional content.
/// </summary>
public static class NarrativeInsertKeys
{
    public const string SpecialAssessmentWarning = "SpecialAssessmentWarning";
    public const string LoanDisclosure = "LoanDisclosure";
    public const string InflationDisclaimer = "InflationDisclaimer";
    public const string StateLawReference = "StateLawReference";
    public const string ProfessionalCredentials = "ProfessionalCredentials";
    public const string LimitationsDisclaimer = "LimitationsDisclaimer";
    public const string SiteVisitNotes = "SiteVisitNotes";
    public const string DeferredMaintenanceWarning = "DeferredMaintenanceWarning";
    public const string UnderfundedWarning = "UnderfundedWarning";
}
