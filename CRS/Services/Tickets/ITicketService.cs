using Horizon.Data;
using Horizon.Models;

namespace Horizon.Services.Tickets;

public interface ITicketService
{
    // Query methods
    Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SupportTicket>> GetAllTicketsAsync(TicketFilter? filter = null, CancellationToken ct = default);
    Task<SupportTicket?> GetTicketByIdAsync(Guid ticketId, CancellationToken ct = default);
    
    // Ticket lifecycle
    Task<SupportTicket> CreateTicketAsync(SupportTicket ticket, Guid createdByUserId, CancellationToken ct = default);
    Task<bool> UpdateTicketAsync(Guid ticketId, TicketUpdateRequest update, CancellationToken ct = default);
    Task<bool> AssignTicketAsync(Guid ticketId, Guid? assignedToUserId, CancellationToken ct = default);
    Task<bool> ResolveTicketAsync(Guid ticketId, string? resolution = null, CancellationToken ct = default);
    Task<bool> CloseTicketAsync(Guid ticketId, string? resolution = null, CancellationToken ct = default);
    Task<bool> ReopenTicketAsync(Guid ticketId, CancellationToken ct = default);
    
    // Comments
    Task<IReadOnlyList<TicketComment>> GetCommentsAsync(Guid ticketId, bool includeInternal, CancellationToken ct = default);
    Task<TicketComment> AddCommentAsync(Guid ticketId, string content, Guid authorUserId, bool isFromStaff, CommentVisibility visibility, CancellationToken ct = default);
    
    // Platform admin methods (cross-tenant)
    Task<IReadOnlyList<SupportTicket>> GetAllTicketsForPlatformAsync(TicketFilter? filter = null, CancellationToken ct = default);
    
    // Staff/assignment
    Task<IReadOnlyList<ApplicationUser>> GetAssignableUsersAsync(CancellationToken ct = default);
}

/// <summary>
/// Filter options for querying tickets.
/// </summary>
public class TicketFilter
{
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? ReserveStudyId { get; set; }
    public bool IncludeClosed { get; set; } = false;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

/// <summary>
/// Request object for updating a ticket.
/// </summary>
public class TicketUpdateRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public TicketCategory? Category { get; set; }
}