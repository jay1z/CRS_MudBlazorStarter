using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

/// <summary>
/// Service for managing site visit photos for reserve studies.
/// </summary>
public class SiteVisitPhotoService : ISiteVisitPhotoService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;

    public SiteVisitPhotoService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<SiteVisitPhoto>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .AsNoTracking()
            .Where(p => p.ReserveStudyId == reserveStudyId && p.DateDeleted == null)
            .OrderBy(p => p.SortOrder)
            .ThenByDescending(p => p.PhotoTakenAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SiteVisitPhoto>> GetByCategoryAsync(Guid reserveStudyId, PhotoCategory category, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .AsNoTracking()
            .Where(p => p.ReserveStudyId == reserveStudyId && 
                       p.Category == category && 
                       p.DateDeleted == null)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SiteVisitPhoto>> GetByElementAsync(Guid elementId, string elementType, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .AsNoTracking()
            .Where(p => p.ElementId == elementId && 
                       p.ElementType == elementType && 
                       p.DateDeleted == null)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SiteVisitPhoto>> GetForReportAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .AsNoTracking()
            .Where(p => p.ReserveStudyId == reserveStudyId && 
                       p.IncludeInReport && 
                       p.DateDeleted == null)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<SiteVisitPhoto?> GetPrimaryPhotoAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ReserveStudyId == reserveStudyId && 
                                     p.IsPrimary && 
                                     p.DateDeleted == null, ct);
    }

    public async Task<SiteVisitPhoto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .AsNoTracking()
            .Include(p => p.TakenBy)
            .FirstOrDefaultAsync(p => p.Id == id && p.DateDeleted == null, ct);
    }

    public async Task<SiteVisitPhoto> CreateAsync(SiteVisitPhoto photo, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        photo.TenantId = _tenantContext.TenantId.Value;
        photo.DateCreated = DateTime.UtcNow;

        // Get the next sort order
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        var maxOrder = await context.SiteVisitPhotos
            .Where(p => p.ReserveStudyId == photo.ReserveStudyId && p.DateDeleted == null)
            .MaxAsync(p => (int?)p.SortOrder, ct) ?? -1;
        photo.SortOrder = maxOrder + 1;

        context.SiteVisitPhotos.Add(photo);
        await context.SaveChangesAsync(ct);

        return photo;
    }

    public async Task<SiteVisitPhoto> UpdateAsync(SiteVisitPhoto photo, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.SiteVisitPhotos
            .FirstOrDefaultAsync(p => p.Id == photo.Id && p.DateDeleted == null, ct);

        if (existing == null)
            throw new InvalidOperationException("Photo not found");

        existing.Caption = photo.Caption;
        existing.Notes = photo.Notes;
        existing.Condition = photo.Condition;
        existing.Category = photo.Category;
        existing.IncludeInReport = photo.IncludeInReport;
        existing.ElementId = photo.ElementId;
        existing.ElementType = photo.ElementType;
        existing.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var photo = await context.SiteVisitPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.DateDeleted == null, ct);

        if (photo == null) return false;

        photo.DateDeleted = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetAsPrimaryAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var photo = await context.SiteVisitPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.DateDeleted == null, ct);

        if (photo == null) return false;

        // Clear any existing primary photo for this study
        var existingPrimary = await context.SiteVisitPhotos
            .Where(p => p.ReserveStudyId == photo.ReserveStudyId && p.IsPrimary && p.Id != id)
            .ToListAsync(ct);

        foreach (var existing in existingPrimary)
        {
            existing.IsPrimary = false;
        }

        photo.IsPrimary = true;
        photo.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SetIncludeInReportAsync(Guid id, bool include, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var photo = await context.SiteVisitPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.DateDeleted == null, ct);

        if (photo == null) return false;

        photo.IncludeInReport = include;
        photo.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateConditionAsync(Guid id, ElementCondition condition, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var photo = await context.SiteVisitPhotos
            .FirstOrDefaultAsync(p => p.Id == id && p.DateDeleted == null, ct);

        if (photo == null) return false;

        photo.Condition = condition;
        photo.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ReorderPhotosAsync(Guid reserveStudyId, IEnumerable<Guid> photoIdsInOrder, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var photos = await context.SiteVisitPhotos
            .Where(p => p.ReserveStudyId == reserveStudyId && p.DateDeleted == null)
            .ToListAsync(ct);

        var order = 0;
        foreach (var photoId in photoIdsInOrder)
        {
            var photo = photos.FirstOrDefault(p => p.Id == photoId);
            if (photo != null)
            {
                photo.SortOrder = order++;
                photo.DateModified = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> GetCountAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.SiteVisitPhotos
            .CountAsync(p => p.ReserveStudyId == reserveStudyId && p.DateDeleted == null, ct);
    }

    public async Task<bool> AssociateWithElementAsync(Guid photoId, Guid elementId, string elementType, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var photo = await context.SiteVisitPhotos
            .FirstOrDefaultAsync(p => p.Id == photoId && p.DateDeleted == null, ct);

        if (photo == null) return false;

        photo.ElementId = elementId;
        photo.ElementType = elementType;
        photo.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }
}
