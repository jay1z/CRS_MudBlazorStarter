using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Data;
using Horizon.Services.Tenant;

namespace Horizon.Models;

/// <summary>
/// Represents a photo taken during a site visit for a reserve study.
/// Photos can be associated with specific building or common elements.
/// </summary>
public class SiteVisitPhoto : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Required link to reserve study
    [Required]
    [ForeignKey(nameof(ReserveStudy))]
    public Guid ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    // Optional element association
    public Guid? ElementId { get; set; }

    [MaxLength(50)]
    public string? ElementType { get; set; }

    // Photo storage
    [Required]
    [MaxLength(2048)]
    public string StorageUrl { get; set; } = string.Empty;

    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    // Photo details
    [MaxLength(500)]
    public string? Caption { get; set; }

    public ElementCondition Condition { get; set; } = ElementCondition.NotAssessed;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    // Metadata
    public DateTime PhotoTakenAt { get; set; } = DateTime.UtcNow;

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [ForeignKey(nameof(TakenBy))]
    public Guid TakenByUserId { get; set; }
    public ApplicationUser? TakenBy { get; set; }

    // Organization
    public int SortOrder { get; set; } = 0;

    public bool IncludeInReport { get; set; } = true;

    public bool IsPrimary { get; set; } = false;

    // Photo category for filtering
    public PhotoCategory Category { get; set; } = PhotoCategory.General;
}

/// <summary>
/// Condition assessment for elements in photos.
/// </summary>
public enum ElementCondition
{
    NotAssessed = 0,
    Excellent = 1,
    Good = 2,
    Fair = 3,
    Poor = 4,
    Critical = 5,
    NeedsReplacement = 6
}

/// <summary>
/// Categories for organizing site visit photos.
/// </summary>
public enum PhotoCategory
{
    General = 0,
    Exterior = 1,
    Interior = 2,
    Roof = 3,
    Foundation = 4,
    Mechanical = 5,
    Electrical = 6,
    Plumbing = 7,
    CommonArea = 8,
    Amenity = 9,
    Damage = 10,
    BeforeAfter = 11
}
