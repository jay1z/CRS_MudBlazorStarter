using System.Text;
using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Horizon.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

/// <summary>
/// Service for querying and managing audit logs.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<AuditLogService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task<AuditLogPagedResult> GetAuditLogsAsync(AuditLogFilter filter, int page = 1, int pageSize = 50)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var query = context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        // Apply filters
        query = ApplyFilters(query, filter);

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering and pagination
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new AuditLogPagedResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AuditLog?> GetByIdAsync(Guid id)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<string>> GetDistinctTableNamesAsync()
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .Where(a => a.TableName != null)
            .Select(a => a.TableName!)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }

    public async Task<List<string>> GetDistinctActionsAsync()
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        return await context.AuditLogs
            .Where(a => a.Action != null)
            .Select(a => a.Action!)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();
    }

    public async Task<byte[]> ExportToCsvAsync(AuditLogFilter filter)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();

        var query = context.AuditLogs
            .Include(a => a.User)
            .AsNoTracking()
            .AsQueryable();

        query = ApplyFilters(query, filter);

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(10000) // Limit export to 10k rows
            .ToListAsync();

        var sb = new StringBuilder();
        
        // CSV Header
        sb.AppendLine("Timestamp,User,Action,Table,RecordId,Column,OldValue,NewValue,IP Address,Method,Path");

        // CSV Rows
        foreach (var log in logs)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")),
                EscapeCsv(log.ActorName ?? log.User?.Email ?? "System"),
                EscapeCsv(log.Action ?? ""),
                EscapeCsv(log.TableName ?? ""),
                EscapeCsv(log.RecordId ?? ""),
                EscapeCsv(log.ColumnName ?? ""),
                EscapeCsv(TruncateValue(log.OldValue)),
                EscapeCsv(TruncateValue(log.NewValue)),
                EscapeCsv(log.RemoteIp ?? ""),
                EscapeCsv(log.Method ?? ""),
                EscapeCsv(log.Path ?? "")
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogFilter filter)
    {
        if (filter.UserId.HasValue)
        {
            query = query.Where(a => a.ApplicationUserId == filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.UserSearch))
        {
            var search = filter.UserSearch.ToLower();
            query = query.Where(a => 
                (a.ActorName != null && a.ActorName.ToLower().Contains(search)) ||
                (a.User != null && a.User.Email != null && a.User.Email.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            query = query.Where(a => a.Action == filter.Action);
        }

        if (!string.IsNullOrWhiteSpace(filter.TableName))
        {
            query = query.Where(a => a.TableName == filter.TableName);
        }

        if (!string.IsNullOrWhiteSpace(filter.RecordId))
        {
            query = query.Where(a => a.RecordId == filter.RecordId);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(a => a.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            var endOfDay = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(a => a.CreatedAt < endOfDay);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            query = query.Where(a => a.CorrelationId == filter.CorrelationId);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var search = filter.SearchText.ToLower();
            query = query.Where(a =>
                (a.TableName != null && a.TableName.ToLower().Contains(search)) ||
                (a.Action != null && a.Action.ToLower().Contains(search)) ||
                (a.RecordId != null && a.RecordId.ToLower().Contains(search)) ||
                (a.ColumnName != null && a.ColumnName.ToLower().Contains(search)) ||
                (a.OldValue != null && a.OldValue.ToLower().Contains(search)) ||
                (a.NewValue != null && a.NewValue.ToLower().Contains(search)) ||
                (a.Path != null && a.Path.ToLower().Contains(search)));
        }

        return query;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }

    private static string TruncateValue(string? value, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Length <= maxLength)
            return value;

        return value[..maxLength] + "...";
    }
}
