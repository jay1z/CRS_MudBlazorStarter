using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Data;
using Horizon.Services.Tenant;

namespace Horizon.Models;

/// <summary>
/// Represents a note or comment on a reserve study.
/// Supports internal staff notes and client-visible comments with threaded replies.
/// </summary>
public class StudyNote : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Required link to reserve study
    [Required]
    [ForeignKey(nameof(ReserveStudy))]
    public Guid ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    // Author
    [Required]
    [ForeignKey(nameof(Author))]
    public Guid AuthorUserId { get; set; }
    public ApplicationUser? Author { get; set; }

    // Content
    [Required]
    public string Content { get; set; } = string.Empty;

    // Visibility
    public NoteVisibility Visibility { get; set; } = NoteVisibility.Internal;

    // Organization
    public bool IsPinned { get; set; } = false;

    public bool IsResolved { get; set; } = false;

    public DateTime? ResolvedAt { get; set; }

    [ForeignKey(nameof(ResolvedBy))]
    public Guid? ResolvedByUserId { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }

    // Threading support
    [ForeignKey(nameof(ParentNote))]
    public Guid? ParentNoteId { get; set; }
    public StudyNote? ParentNote { get; set; }

    // Navigation for replies
    public ICollection<StudyNote>? Replies { get; set; }

    // Note type for categorization
    public NoteType Type { get; set; } = NoteType.General;

    // Optional link to specific workflow status
    [MaxLength(100)]
    public string? RelatedToStatus { get; set; }

    // Mentions (stored as comma-separated user IDs)
    [MaxLength(1000)]
    public string? MentionedUserIds { get; set; }
}

/// <summary>
/// Visibility levels for study notes.
/// </summary>
public enum NoteVisibility
{
    /// <summary>Staff only - not visible to HOA users</summary>
    Internal = 0,

    /// <summary>Visible to the HOA user/client</summary>
    ClientVisible = 1,

    /// <summary>Only visible to the author</summary>
    Private = 2
}

/// <summary>
/// Types of study notes for categorization.
/// </summary>
public enum NoteType
{
    General = 0,
    Question = 1,
    ActionItem = 2,
    Issue = 3,
    Resolution = 4,
    ClientFeedback = 5,
    InternalReview = 6,
    StatusUpdate = 7
}
