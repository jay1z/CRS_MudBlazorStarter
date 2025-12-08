using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Represents a generated report document for a reserve study.
/// Tracks report versions, delivery to clients, and publishing status.
/// </summary>
public class GeneratedReport : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Required link to reserve study
    [Required]
    [ForeignKey(nameof(ReserveStudy))]
    public Guid ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    // Report details
    public ReportType Type { get; set; } = ReportType.Draft;

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = "1.0";

    [MaxLength(256)]
    public string? Title { get; set; }

    // File storage
    [Required]
    [MaxLength(2048)]
    public string StorageUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public int PageCount { get; set; }

    // Generation tracking
    [ForeignKey(nameof(GeneratedBy))]
    public Guid GeneratedByUserId { get; set; }
    public ApplicationUser? GeneratedBy { get; set; }

    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // Template information
    [MaxLength(100)]
    public string? TemplateUsed { get; set; }

    [MaxLength(50)]
    public string? OutputFormat { get; set; }

    // Client delivery
    public bool IsPublishedToClient { get; set; } = false;

    public DateTime? PublishedAt { get; set; }

    [ForeignKey(nameof(PublishedBy))]
    public Guid? PublishedByUserId { get; set; }
    public ApplicationUser? PublishedBy { get; set; }

    public DateTime? SentToClientAt { get; set; }

    [MaxLength(256)]
    public string? SentToEmail { get; set; }

    // Review tracking
    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    public DateTime? ReviewedAt { get; set; }

    [ForeignKey(nameof(ReviewedBy))]
    public Guid? ReviewedByUserId { get; set; }
    public ApplicationUser? ReviewedBy { get; set; }

    // Notes
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    // Supersedes previous version
    [ForeignKey(nameof(SupersedesReport))]
    public Guid? SupersedesReportId { get; set; }
    public GeneratedReport? SupersedesReport { get; set; }

    // Download tracking
    public int DownloadCount { get; set; } = 0;

    public DateTime? LastDownloadedAt { get; set; }
}

/// <summary>
/// Types of reports that can be generated.
/// </summary>
public enum ReportType
{
    Draft = 0,
    Final = 1,
    ExecutiveSummary = 2,
    FundingPlan = 3,
    ComponentInventory = 4,
    FullReport = 5,
    UpdateReport = 6,
    Addendum = 7,
    CorrectionNotice = 8
}

/// <summary>
/// Status of a generated report in the review/publish workflow.
/// </summary>
public enum ReportStatus
{
    Draft = 0,
    PendingReview = 1,
    InReview = 2,
    RevisionRequired = 3,
    Approved = 4,
    Published = 5,
    Superseded = 6,
    Archived = 7
}
