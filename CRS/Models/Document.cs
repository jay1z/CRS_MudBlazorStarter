using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Represents a file/document stored in the system.
/// Documents can be associated with reserve studies, communities, or be standalone.
/// </summary>
public class Document : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Optional associations
    [ForeignKey(nameof(ReserveStudy))]
    public Guid? ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    [ForeignKey(nameof(Community))]
    public Guid? CommunityId { get; set; }
    public Community? Community { get; set; }

    // File details
    [Required]
    [MaxLength(500)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? OriginalFileName { get; set; }

    [Required]
    [MaxLength(2048)]
    public string StorageUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DocumentType Type { get; set; } = DocumentType.Other;

    // Upload tracking
    [ForeignKey(nameof(UploadedBy))]
    public Guid UploadedByUserId { get; set; }
    public ApplicationUser? UploadedBy { get; set; }

    // Visibility
    public bool IsPublic { get; set; } = false;

    [MaxLength(1000)]
    public string? Description { get; set; }

    // Soft delete is handled by BaseModel.DateDeleted
}

/// <summary>
/// Types of documents that can be stored in the system.
/// </summary>
public enum DocumentType
{
    Other = 0,
    Report = 1,
    Invoice = 2,
    Photo = 3,
    Contract = 4,
    FinancialStatement = 5,
    ProposalDocument = 6,
    SiteVisitNotes = 7,
    Insurance = 8,
    Warranty = 9,
    MaintenanceRecord = 10
}
