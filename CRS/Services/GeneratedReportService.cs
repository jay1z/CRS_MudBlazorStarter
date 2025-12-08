using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Service for managing generated reports for reserve studies.
/// </summary>
public class GeneratedReportService : IGeneratedReportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;

    public GeneratedReportService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Where(r => r.ReserveStudyId == reserveStudyId && r.DateDeleted == null)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetByTypeAsync(Guid reserveStudyId, ReportType type, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Where(r => r.ReserveStudyId == reserveStudyId && 
                       r.Type == type && 
                       r.DateDeleted == null)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetByStatusAsync(Guid reserveStudyId, ReportStatus status, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Where(r => r.ReserveStudyId == reserveStudyId && 
                       r.Status == status && 
                       r.DateDeleted == null)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetPublishedReportsAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Where(r => r.ReserveStudyId == reserveStudyId && 
                       r.IsPublishedToClient && 
                       r.DateDeleted == null)
            .OrderByDescending(r => r.PublishedAt)
            .ToListAsync(ct);
    }

    public async Task<GeneratedReport?> GetLatestByTypeAsync(Guid reserveStudyId, ReportType type, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Where(r => r.ReserveStudyId == reserveStudyId && 
                       r.Type == type && 
                       r.DateDeleted == null)
            .OrderByDescending(r => r.GeneratedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetPendingReviewAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Include(r => r.ReserveStudy)
            .Where(r => r.Status == ReportStatus.PendingReview && r.DateDeleted == null)
            .OrderBy(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task<GeneratedReport?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Include(r => r.ReviewedBy)
            .Include(r => r.PublishedBy)
            .Include(r => r.ReserveStudy)
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);
    }

    public async Task<GeneratedReport> CreateAsync(GeneratedReport report, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        report.TenantId = _tenantContext.TenantId.Value;
        report.DateCreated = DateTime.UtcNow;
        report.GeneratedAt = DateTime.UtcNow;
        report.Status = ReportStatus.Draft;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.GeneratedReports.Add(report);
        await context.SaveChangesAsync(ct);

        return report;
    }

    public async Task<GeneratedReport> UpdateAsync(GeneratedReport report, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existing = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == report.Id && r.DateDeleted == null, ct);

        if (existing == null)
            throw new InvalidOperationException("Report not found");

        existing.Title = report.Title;
        existing.Version = report.Version;
        existing.Notes = report.Notes;
        existing.InternalNotes = report.InternalNotes;
        existing.TemplateUsed = report.TemplateUsed;
        existing.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.DateDeleted = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SubmitForReviewAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.Status = ReportStatus.PendingReview;
        report.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ApproveAsync(Guid id, Guid reviewedByUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.Status = ReportStatus.Approved;
        report.ReviewedByUserId = reviewedByUserId;
        report.ReviewedAt = DateTime.UtcNow;
        report.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RequestRevisionsAsync(Guid id, Guid reviewedByUserId, string? notes = null, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.Status = ReportStatus.RevisionRequired;
        report.ReviewedByUserId = reviewedByUserId;
        report.ReviewedAt = DateTime.UtcNow;
        if (notes != null)
        {
            report.InternalNotes = string.IsNullOrEmpty(report.InternalNotes) 
                ? notes 
                : $"{report.InternalNotes}\n\n[Revision Request]: {notes}";
        }
        report.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> PublishAsync(Guid id, Guid publishedByUserId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.Status = ReportStatus.Published;
        report.IsPublishedToClient = true;
        report.PublishedByUserId = publishedByUserId;
        report.PublishedAt = DateTime.UtcNow;
        report.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MarkSentToClientAsync(Guid id, string email, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.SentToClientAt = DateTime.UtcNow;
        report.SentToEmail = email;
        report.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RecordDownloadAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.DownloadCount++;
        report.LastDownloadedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<GeneratedReport> CreateNewVersionAsync(Guid existingReportId, GeneratedReport newVersion, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var existingReport = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == existingReportId && r.DateDeleted == null, ct);

        if (existingReport == null)
            throw new InvalidOperationException("Existing report not found");

        // Mark existing report as superseded
        existingReport.Status = ReportStatus.Superseded;
        existingReport.DateModified = DateTime.UtcNow;

        // Create new version
        newVersion.TenantId = _tenantContext.TenantId.Value;
        newVersion.ReserveStudyId = existingReport.ReserveStudyId;
        newVersion.Type = existingReport.Type;
        newVersion.SupersedesReportId = existingReportId;
        newVersion.DateCreated = DateTime.UtcNow;
        newVersion.GeneratedAt = DateTime.UtcNow;
        newVersion.Status = ReportStatus.Draft;

        context.GeneratedReports.Add(newVersion);
        await context.SaveChangesAsync(ct);

        return newVersion;
    }

    public async Task<bool> ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        report.Status = ReportStatus.Archived;
        report.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetVersionHistoryAsync(Guid reportId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        // Get the report to find its study
        var report = await context.GeneratedReports
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId, ct);

        if (report == null) return [];

        // Get all versions of this report type for the same study
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Where(r => r.ReserveStudyId == report.ReserveStudyId && 
                       r.Type == report.Type)
            .OrderByDescending(r => r.GeneratedAt)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<GeneratedReport>> GetRecentAsync(int count = 20, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .AsNoTracking()
            .Include(r => r.GeneratedBy)
            .Include(r => r.ReserveStudy)
            .Where(r => r.DateDeleted == null)
            .OrderByDescending(r => r.GeneratedAt)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return 0;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.GeneratedReports
            .CountAsync(r => r.ReserveStudyId == reserveStudyId && r.DateDeleted == null, ct);
    }
}
