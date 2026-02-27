using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service interface for querying and managing audit logs.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Gets paginated audit logs with optional filtering.
    /// </summary>
    Task<AuditLogPagedResult> GetAuditLogsAsync(AuditLogFilter filter, int page = 1, int pageSize = 50);

    /// <summary>
    /// Gets a single audit log entry by ID.
    /// </summary>
    Task<AuditLog?> GetByIdAsync(Guid id);

    /// <summary>
    /// Gets all distinct table names that have been audited.
    /// </summary>
    Task<List<string>> GetDistinctTableNamesAsync();

    /// <summary>
    /// Gets all distinct action types that have been logged.
    /// </summary>
    Task<List<string>> GetDistinctActionsAsync();

    /// <summary>
    /// Exports audit logs matching the filter to CSV format.
    /// </summary>
    Task<byte[]> ExportToCsvAsync(AuditLogFilter filter);
}

/// <summary>
/// Filter criteria for querying audit logs.
/// </summary>
public class AuditLogFilter
{
    /// <summary>
    /// Filter by specific user ID.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Filter by user name or email (partial match).
    /// </summary>
    public string? UserSearch { get; set; }

    /// <summary>
    /// Filter by action type (Created, Modified, Deleted).
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Filter by table/entity name.
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// Filter by specific record ID.
    /// </summary>
    public string? RecordId { get; set; }

    /// <summary>
    /// Filter logs from this date onwards.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter logs up to this date.
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Free text search across multiple fields.
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Filter by correlation ID (groups related changes).
    /// </summary>
    public string? CorrelationId { get; set; }
}

/// <summary>
/// Paged result for audit log queries.
/// </summary>
public class AuditLogPagedResult
{
    public List<AuditLog> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
