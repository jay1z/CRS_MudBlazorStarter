using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Services.Tickets;

public class TicketService : ITicketService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenant;

    public TicketService(IDbContextFactory<ApplicationDbContext> dbFactory, ITenantContext tenant)
    {
        _dbFactory = dbFactory;
        _tenant = tenant;
    }

    #region Query Methods

    public async Task<IReadOnlyList<SupportTicket>> GetOpenTicketsAsync(CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return Array.Empty<SupportTicket>();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.SupportTickets
            .AsNoTracking()
            .Where(t => t.TenantId == _tenant.TenantId &&
                       t.Status != TicketStatus.Closed &&
                       t.Status != TicketStatus.Cancelled)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SupportTicket>> GetAllTicketsAsync(TicketFilter? filter = null, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return Array.Empty<SupportTicket>();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.SupportTickets
            .AsNoTracking()
            .Where(t => t.TenantId == _tenant.TenantId);

        query = ApplyFilter(query, filter);

        return await query
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<SupportTicket?> GetTicketByIdAsync(Guid ticketId, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return null;
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.SupportTickets
            .AsNoTracking()
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .Include(t => t.ReserveStudy)
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.TenantId == _tenant.TenantId, ct);
    }

    public async Task<IReadOnlyList<SupportTicket>> GetAllTicketsForPlatformAsync(TicketFilter? filter = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.SupportTickets
            .AsNoTracking()
            .IgnoreQueryFilters();

        query = ApplyFilter(query, filter);

        return await query
            .Include(t => t.CreatedByUser)
            .Include(t => t.AssignedToUser)
            .ToListAsync(ct);
    }

    private static IQueryable<SupportTicket> ApplyFilter(IQueryable<SupportTicket> query, TicketFilter? filter)
    {
        if (filter == null) return query;

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (filter.Category.HasValue)
            query = query.Where(t => t.Category == filter.Category.Value);

        if (filter.AssignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == filter.AssignedToUserId.Value);

        if (filter.CreatedByUserId.HasValue)
            query = query.Where(t => t.CreatedByUserId == filter.CreatedByUserId.Value);

        if (filter.ReserveStudyId.HasValue)
            query = query.Where(t => t.ReserveStudyId == filter.ReserveStudyId.Value);

        if (!filter.IncludeClosed)
            query = query.Where(t => t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled);

        // Order BEFORE applying Skip/Take to ensure consistent pagination
        query = query.OrderByDescending(t => t.CreatedAt);

        var skip = (filter.Page - 1) * filter.PageSize;
        query = query.Skip(skip).Take(filter.PageSize);

        return query;
    }

    #endregion

    #region Ticket Lifecycle

    public async Task<SupportTicket> CreateTicketAsync(SupportTicket ticket, Guid createdByUserId, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) throw new InvalidOperationException("Tenant context required");
        ticket.TenantId = _tenant.TenantId.Value;
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.Status = TicketStatus.Open;
        ticket.CreatedByUserId = createdByUserId;
        
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        // Auto-assign to the reserve study's specialist if linked
        if (ticket.ReserveStudyId.HasValue)
        {
            var study = await db.ReserveStudies
                .AsNoTracking()
                .Where(rs => rs.Id == ticket.ReserveStudyId.Value)
                .Select(rs => new { rs.SpecialistUserId })
                .FirstOrDefaultAsync(ct);
            
            if (study?.SpecialistUserId != null)
            {
                ticket.AssignedToUserId = study.SpecialistUserId;
                ticket.AssignedAt = DateTime.UtcNow;
                ticket.Status = TicketStatus.InProgress;
            }
        }
        
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync(ct);
        return ticket;
    }

    public async Task<bool> UpdateTicketAsync(Guid ticketId, TicketUpdateRequest update, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null) return false;

        if (update.Title != null) ticket.Title = update.Title;
        if (update.Description != null) ticket.Description = update.Description;
        if (update.Status.HasValue) ticket.Status = update.Status.Value;
        if (update.Priority.HasValue) ticket.Priority = update.Priority.Value;
        if (update.Category.HasValue) ticket.Category = update.Category.Value;

        ticket.DateModified = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> AssignTicketAsync(Guid ticketId, Guid? assignedToUserId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null) return false;

        ticket.AssignedToUserId = assignedToUserId;
        ticket.AssignedAt = assignedToUserId.HasValue ? DateTime.UtcNow : null;
        
        // Auto-update status when assigned
        if (assignedToUserId.HasValue && ticket.Status == TicketStatus.Open)
            ticket.Status = TicketStatus.InProgress;

        ticket.DateModified = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResolveTicketAsync(Guid ticketId, string? resolution = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null) return false;

        ticket.Status = TicketStatus.Resolved;
        ticket.ResolvedAt = DateTime.UtcNow;
        if (resolution != null) ticket.Resolution = resolution;

        ticket.DateModified = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> CloseTicketAsync(Guid ticketId, string? resolution = null, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null) return false;

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTime.UtcNow;
        if (resolution != null) ticket.Resolution = resolution;

        ticket.DateModified = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReopenTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ticket = await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket == null) return false;

        ticket.Status = TicketStatus.Open;
        ticket.ResolvedAt = null;
        ticket.ClosedAt = null;

        ticket.DateModified = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return true;
    }

    #endregion

    #region Comments

    public async Task<IReadOnlyList<TicketComment>> GetCommentsAsync(Guid ticketId, bool includeInternal, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var query = db.TicketComments
            .AsNoTracking()
            .Where(c => c.TicketId == ticketId);

        if (!includeInternal)
            query = query.Where(c => c.Visibility == CommentVisibility.Public);

        return await query
            .Include(c => c.Author)
            .OrderBy(c => c.PostedAt)
            .ToListAsync(ct);
    }

    public async Task<TicketComment> AddCommentAsync(Guid ticketId, string content, Guid authorUserId, bool isFromStaff, CommentVisibility visibility, CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) throw new InvalidOperationException("Tenant context required");

        var comment = new TicketComment
        {
            TenantId = _tenant.TenantId.Value,
            TicketId = ticketId,
            Content = content,
            AuthorUserId = authorUserId,
            IsFromStaff = isFromStaff,
            Visibility = visibility,
            PostedAt = DateTime.UtcNow
        };

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        db.TicketComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment;
    }

    #endregion

    #region Staff/Assignment

    public async Task<IReadOnlyList<ApplicationUser>> GetAssignableUsersAsync(CancellationToken ct = default)
    {
        if (!_tenant.TenantId.HasValue) return Array.Empty<ApplicationUser>();
        
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        
        // Get users who are TenantOwner or TenantSpecialist for this tenant
        var assignableRoles = new[] { "TenantOwner", "TenantSpecialist" };
        
        var users = await db.UserRoleAssignments
            .AsNoTracking()
            .Where(ura => ura.TenantId == _tenant.TenantId && 
                          ura.Role != null && 
                          assignableRoles.Contains(ura.Role.Name))
            .Select(ura => ura.User)
            .Where(u => u != null)
            .Distinct()
            .ToListAsync(ct);
        
        return users!;
    }

    #endregion
}