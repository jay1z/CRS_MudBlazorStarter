using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Represents a support ticket submitted by a user.
/// </summary>
public class SupportTicket : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketCategory Category { get; set; } = TicketCategory.General;

    // Created by user
    [ForeignKey(nameof(CreatedByUser))]
    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }

    // Assigned to staff member
    [ForeignKey(nameof(AssignedToUser))]
    public Guid? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }

    // Optional link to related reserve study
    [ForeignKey(nameof(ReserveStudy))]
    public Guid? ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    [MaxLength(2000)]
    public string? Resolution { get; set; }
}

/// <summary>
/// Status of a support ticket.
/// </summary>
public enum TicketStatus
{
    Open = 0,
    InProgress = 1,
    WaitingOnCustomer = 2,
    WaitingOnThirdParty = 3,
    Resolved = 4,
    Closed = 5,
    Cancelled = 6
}

/// <summary>
/// Priority levels for support tickets.
/// </summary>
public enum TicketPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3,
    Critical = 4
}

/// <summary>
/// Categories for support tickets.
/// </summary>
public enum TicketCategory
{
    General = 0,
    Technical = 1,
    Billing = 2,
    FeatureRequest = 3,
    Bug = 4,
    Training = 5,
    DataRequest = 6,
    AccountAccess = 7
}