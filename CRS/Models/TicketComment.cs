using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Data;
using Horizon.Services.Tenant;

namespace Horizon.Models;

/// <summary>
/// Represents a comment or reply on a support ticket.
/// Supports both staff and customer comments with visibility controls.
/// </summary>
public class TicketComment : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }

    /// <summary>
    /// The ticket this comment belongs to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Ticket))]
    public Guid TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    /// <summary>
    /// User who authored this comment.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Author))]
    public Guid AuthorUserId { get; set; }
    public ApplicationUser? Author { get; set; }

    /// <summary>
    /// The comment content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether this comment is from a staff member (platform admin/support or tenant staff).
    /// Used for styling and to distinguish customer vs staff responses.
    /// </summary>
    public bool IsFromStaff { get; set; } = false;

    /// <summary>
    /// Visibility of this comment.
    /// Internal comments are only visible to staff, not customers.
    /// </summary>
    public CommentVisibility Visibility { get; set; } = CommentVisibility.Public;

    /// <summary>
    /// When the comment was posted.
    /// </summary>
    public DateTime PostedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: If this comment was edited, when it was last edited.
    /// </summary>
    public DateTime? EditedAt { get; set; }

    /// <summary>
    /// Whether this comment has been edited.
    /// </summary>
    public bool IsEdited { get; set; } = false;
}

/// <summary>
/// Visibility levels for ticket comments.
/// </summary>
public enum CommentVisibility
{
    /// <summary>
    /// Visible to everyone with access to the ticket (customer and staff).
    /// </summary>
    Public = 0,

    /// <summary>
    /// Only visible to staff members (tenant employees, platform admins/support).
    /// Customers cannot see internal comments.
    /// </summary>
    Internal = 1
}
