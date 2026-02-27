namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for archiving tenant data to cold storage before permanent deletion.
/// Used for GDPR/legal compliance to retain financial records.
/// </summary>
public interface ITenantArchiveService
{
    /// <summary>
    /// Archives all financial and critical records for a tenant to cold storage.
    /// </summary>
    /// <param name="tenantId">Tenant ID to archive</param>
    /// <param name="reason">Reason for archiving (e.g., "payment_failed", "user_requested")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Archive result with location and statistics</returns>
    Task<TenantArchiveResult> ArchiveTenantDataAsync(int tenantId, string reason, CancellationToken ct = default);

    /// <summary>
    /// Gets the archive status for a tenant.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    Task<TenantArchiveInfo?> GetArchiveInfoAsync(int tenantId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a tenant has been archived.
    /// </summary>
    /// <param name="tenantId">Tenant ID</param>
    /// <param name="ct">Cancellation token</param>
    Task<bool> IsArchivedAsync(int tenantId, CancellationToken ct = default);
}

/// <summary>
/// Result of archiving tenant data.
/// </summary>
public record TenantArchiveResult(
    bool Success,
    string? ArchiveUrl,
    DateTime ArchivedAt,
    int InvoicesArchived,
    int PaymentsArchived,
    int ReportsArchived,
    long TotalSizeBytes,
    string? ErrorMessage = null
);

/// <summary>
/// Information about a tenant's archive.
/// </summary>
public record TenantArchiveInfo(
    int TenantId,
    string TenantName,
    string ArchiveUrl,
    DateTime ArchivedAt,
    string Reason,
    long SizeBytes
);
