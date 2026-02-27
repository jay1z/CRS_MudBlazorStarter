using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

/// <summary>
/// Service for managing study notes and comments.
/// </summary>
public class StudyNoteService : IStudyNoteService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;

    public StudyNoteService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<StudyNote>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Where(n => n.ReserveStudyId == reserveStudyId && 
                       n.ParentNoteId == null && 
                       n.DateDeleted == null)
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetByVisibilityAsync(Guid reserveStudyId, NoteVisibility visibility, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Where(n => n.ReserveStudyId == reserveStudyId && 
                       n.Visibility == visibility &&
                       n.ParentNoteId == null &&
                       n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetClientVisibleNotesAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        return await GetByVisibilityAsync(reserveStudyId, NoteVisibility.ClientVisible, ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetPinnedNotesAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Where(n => n.ReserveStudyId == reserveStudyId && 
                       n.IsPinned && 
                       n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetUnresolvedNotesAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Where(n => n.ReserveStudyId == reserveStudyId && 
                       !n.IsResolved && 
                       n.ParentNoteId == null &&
                       n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetByTypeAsync(Guid reserveStudyId, NoteType type, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Where(n => n.ReserveStudyId == reserveStudyId && 
                       n.Type == type &&
                       n.ParentNoteId == null &&
                       n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetByAuthorAsync(Guid authorUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.ReserveStudy)
            .Where(n => n.AuthorUserId == authorUserId && n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudyNote>> GetRepliesAsync(Guid parentNoteId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Where(n => n.ParentNoteId == parentNoteId && n.DateDeleted == null)
            .OrderBy(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<StudyNote?> GetByIdWithRepliesAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Include(n => n.Replies!.Where(r => r.DateDeleted == null))
                .ThenInclude(r => r.Author)
            .Include(n => n.ResolvedBy)
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);
    }

    public async Task<StudyNote?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);
    }

    public async Task<StudyNote> CreateAsync(StudyNote note, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        note.TenantId = _tenantContext.TenantId.Value;
        note.DateCreated = DateTime.UtcNow;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.StudyNotes.Add(note);
        await context.SaveChangesAsync(ct);

        return note;
    }

    public async Task<StudyNote> CreateReplyAsync(Guid parentNoteId, StudyNote reply, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var parentNote = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == parentNoteId && n.DateDeleted == null, ct);

        if (parentNote == null)
            throw new InvalidOperationException("Parent note not found");

        reply.TenantId = _tenantContext.TenantId.Value;
        reply.ReserveStudyId = parentNote.ReserveStudyId;
        reply.ParentNoteId = parentNoteId;
        reply.Visibility = parentNote.Visibility; // Inherit visibility from parent
        reply.DateCreated = DateTime.UtcNow;

        context.StudyNotes.Add(reply);
        await context.SaveChangesAsync(ct);

        return reply;
    }

    public async Task<StudyNote> UpdateAsync(StudyNote note, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == note.Id && n.DateDeleted == null, ct);

        if (existing == null)
            throw new InvalidOperationException("Note not found");

        existing.Content = note.Content;
        existing.Type = note.Type;
        existing.Visibility = note.Visibility;
        existing.RelatedToStatus = note.RelatedToStatus;
        existing.MentionedUserIds = note.MentionedUserIds;
        existing.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var note = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);

        if (note == null) return false;

        note.DateDeleted = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetPinnedAsync(Guid id, bool isPinned, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var note = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);

        if (note == null) return false;

        note.IsPinned = isPinned;
        note.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ResolveAsync(Guid id, Guid resolvedByUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var note = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);

        if (note == null) return false;

        note.IsResolved = true;
        note.ResolvedAt = DateTime.UtcNow;
        note.ResolvedByUserId = resolvedByUserId;
        note.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReopenAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var note = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);

        if (note == null) return false;

        note.IsResolved = false;
        note.ResolvedAt = null;
        note.ResolvedByUserId = null;
        note.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetVisibilityAsync(Guid id, NoteVisibility visibility, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var note = await context.StudyNotes
            .FirstOrDefaultAsync(n => n.Id == id && n.DateDeleted == null, ct);

        if (note == null) return false;

        note.Visibility = visibility;
        note.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<StudyNote>> GetMentionsForUserAsync(Guid userId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        var userIdString = userId.ToString();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .AsNoTracking()
            .Include(n => n.Author)
            .Include(n => n.ReserveStudy)
            .Where(n => n.MentionedUserIds != null && 
                       n.MentionedUserIds.Contains(userIdString) && 
                       n.DateDeleted == null)
            .OrderByDescending(n => n.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .CountAsync(n => n.ReserveStudyId == reserveStudyId && n.DateDeleted == null, ct);
    }

    public async Task<int> GetUnresolvedCountAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.StudyNotes
            .CountAsync(n => n.ReserveStudyId == reserveStudyId && 
                           !n.IsResolved && 
                           n.ParentNoteId == null &&
                           n.DateDeleted == null, ct);
    }
}
