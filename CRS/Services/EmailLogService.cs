using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

/// <summary>
/// Service for logging and tracking emails sent by the system.
/// </summary>
public class EmailLogService : IEmailLogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;

    public EmailLogService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
    }

    public async Task<EmailLog> LogEmailAsync(EmailLog emailLog, CancellationToken ct = default)
    {
        // Use tenant from context if available and not already set on the log
        // TenantId of 0 indicates a platform-level email (no specific tenant)
        if (emailLog.TenantId == 0 && _tenantContext.TenantId.HasValue)
        {
            emailLog.TenantId = _tenantContext.TenantId.Value;
        }

        emailLog.DateCreated = DateTime.UtcNow;
        emailLog.QueuedAt = DateTime.UtcNow;
        if (emailLog.Status == EmailStatus.Queued || emailLog.Status == 0)
            emailLog.Status = EmailStatus.Queued;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        context.EmailLogs.Add(emailLog);
        await context.SaveChangesAsync(ct);

        return emailLog;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, EmailStatus status, string? errorMessage = null, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var log = await context.EmailLogs.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (log == null) return false;

        log.Status = status;
        log.DateModified = DateTime.UtcNow;

        switch (status)
        {
            case EmailStatus.Sending:
                break;
            case EmailStatus.Sent:
                log.SentAt = DateTime.UtcNow;
                break;
            case EmailStatus.Delivered:
                log.DeliveredAt = DateTime.UtcNow;
                break;
            case EmailStatus.Opened:
                log.OpenedAt = DateTime.UtcNow;
                break;
            case EmailStatus.Clicked:
                log.ClickedAt = DateTime.UtcNow;
                break;
            case EmailStatus.Bounced:
                log.BouncedAt = DateTime.UtcNow;
                log.ErrorMessage = errorMessage;
                break;
            case EmailStatus.Failed:
                log.FailedAt = DateTime.UtcNow;
                log.ErrorMessage = errorMessage;
                break;
        }

        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateStatusByExternalIdAsync(string externalMessageId, EmailStatus status, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalMessageId)) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var log = await context.EmailLogs
            .FirstOrDefaultAsync(e => e.ExternalMessageId == externalMessageId, ct);
        
        if (log == null) return false;

        return await UpdateStatusAsync(log.Id, status, ct: ct);
    }

    public async Task<bool> MarkDeliveredAsync(Guid id, CancellationToken ct = default)
    {
        return await UpdateStatusAsync(id, EmailStatus.Delivered, ct: ct);
    }

    public async Task<bool> MarkOpenedAsync(string externalMessageId, CancellationToken ct = default)
    {
        return await UpdateStatusByExternalIdAsync(externalMessageId, EmailStatus.Opened, ct);
    }

    public async Task<bool> MarkBouncedAsync(string externalMessageId, string? errorMessage = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(externalMessageId)) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var log = await context.EmailLogs
            .FirstOrDefaultAsync(e => e.ExternalMessageId == externalMessageId, ct);
        
        if (log == null) return false;

        return await UpdateStatusAsync(log.Id, EmailStatus.Bounced, errorMessage, ct);
    }

    public async Task<IReadOnlyList<EmailLog>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .Where(e => e.ReserveStudyId == reserveStudyId)
            .OrderByDescending(e => e.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmailLog>> GetByRecipientAsync(string email, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue || string.IsNullOrWhiteSpace(email)) 
            return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .Where(e => e.ToEmail.ToLower() == email.ToLower())
            .OrderByDescending(e => e.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmailLog>> GetByStatusAsync(EmailStatus status, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmailLog>> GetByTemplateTypeAsync(string templateType, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue || string.IsNullOrWhiteSpace(templateType)) 
            return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .Where(e => e.TemplateType == templateType)
            .OrderByDescending(e => e.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return null;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IReadOnlyList<EmailLog>> GetRecentAsync(int count = 50, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .OrderByDescending(e => e.DateCreated)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EmailLog>> GetPendingRetriesAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return [];

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        return await context.EmailLogs
            .AsNoTracking()
            .Where(e => e.Status == EmailStatus.Failed && 
                       e.RetryCount < e.MaxRetries)
            .OrderBy(e => e.DateCreated)
            .ToListAsync(ct);
    }

    public async Task<bool> IncrementRetryCountAsync(Guid id, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var log = await context.EmailLogs.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (log == null) return false;

        log.RetryCount++;
        log.Status = EmailStatus.Queued;
        log.DateModified = DateTime.UtcNow;
        
        await context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EmailStats> GetStatsAsync(DateTime? since = null, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            return new EmailStats(0, 0, 0, 0, 0, 0);

        var sinceDate = since ?? DateTime.UtcNow.AddDays(-30);

        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        var logs = await context.EmailLogs
            .AsNoTracking()
            .Where(e => e.DateCreated >= sinceDate)
            .ToListAsync(ct);

        return new EmailStats(
            TotalSent: logs.Count(e => e.SentAt.HasValue),
            Delivered: logs.Count(e => e.Status == EmailStatus.Delivered),
            Opened: logs.Count(e => e.Status == EmailStatus.Opened || e.OpenedAt.HasValue),
            Bounced: logs.Count(e => e.Status == EmailStatus.Bounced),
            Failed: logs.Count(e => e.Status == EmailStatus.Failed),
            Pending: logs.Count(e => e.Status == EmailStatus.Queued || e.Status == EmailStatus.Sending)
        );
    }
}
