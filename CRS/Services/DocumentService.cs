using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Service for managing documents associated with reserve studies and communities.
/// </summary>
public class DocumentService : IDocumentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;

    public DocumentService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<Document>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Where(d => d.ReserveStudyId == reserveStudyId && d.DateDeleted == null)
            .OrderByDescending(d => d.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByCommunityAsync(Guid communityId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Where(d => d.CommunityId == communityId && d.DateDeleted == null)
            .OrderByDescending(d => d.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetByTypeAsync(DocumentType type, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Where(d => d.Type == type && d.DateDeleted == null)
            .OrderByDescending(d => d.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetPublicDocumentsAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Where(d => d.ReserveStudyId == reserveStudyId && 
                       d.IsPublic && 
                       d.DateDeleted == null)
            .OrderByDescending(d => d.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> GetPublicDocumentsAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Include(d => d.UploadedBy)
            .Include(d => d.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Include(d => d.Community)
            .Where(d => d.IsPublic && d.DateDeleted == null)
            .OrderByDescending(d => d.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Include(d => d.UploadedBy)
            .FirstOrDefaultAsync(d => d.Id == id && d.DateDeleted == null, ct);
    }

    public async Task<Document> CreateAsync(Document document, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        document.TenantId = _tenantContext.TenantId.Value;
        document.DateCreated = DateTime.UtcNow;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.Documents.Add(document);
        await context.SaveChangesAsync(ct);

        return document;
    }

    public async Task<Document> UpdateAsync(Document document, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == document.Id && d.DateDeleted == null, ct);

        if (existing == null)
            throw new InvalidOperationException("Document not found");

        existing.FileName = document.FileName;
        existing.OriginalFileName = document.OriginalFileName;
        existing.Description = document.Description;
        existing.Type = document.Type;
        existing.IsPublic = document.IsPublic;
        existing.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.DateDeleted == null, ct);

        if (document == null) return false;

        document.DateDeleted = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetPublicAsync(Guid id, bool isPublic, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var document = await context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.DateDeleted == null, ct);

        if (document == null) return false;

        document.IsPublic = isPublic;
        document.DateModified = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetCountAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .CountAsync(d => d.DateDeleted == null, ct);
    }

    public async Task<IReadOnlyList<Document>> GetRecentAsync(int count = 10, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Include(d => d.UploadedBy)
            .Include(d => d.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Include(d => d.Community)
            .Where(d => d.DateDeleted == null)
            .OrderByDescending(d => d.DateCreated)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Document>> SearchAsync(string searchTerm, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue || string.IsNullOrWhiteSpace(searchTerm)) 
            return [];

        var term = searchTerm.ToLower();

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.Documents
            .AsNoTracking()
            .Where(d => d.DateDeleted == null &&
                       (d.FileName.ToLower().Contains(term) ||
                        d.OriginalFileName != null && d.OriginalFileName.ToLower().Contains(term) ||
                        d.Description != null && d.Description.ToLower().Contains(term)))
            .OrderByDescending(d => d.DateCreated)
            .Take(50)
            .ToListAsync(ct);
    }
}
