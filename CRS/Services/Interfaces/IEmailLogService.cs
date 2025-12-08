using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service for logging and tracking emails sent by the system.
/// </summary>
public interface IEmailLogService
{
    /// <summary>
    /// Logs a new email being sent.
    /// </summary>
    Task<EmailLog> LogEmailAsync(EmailLog emailLog, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an email.
    /// </summary>
    Task<bool> UpdateStatusAsync(Guid id, EmailStatus status, string? errorMessage = null, CancellationToken ct = default);

    /// <summary>
    /// Updates the status using the external message ID (e.g., from SendGrid webhook).
    /// </summary>
    Task<bool> UpdateStatusByExternalIdAsync(string externalMessageId, EmailStatus status, CancellationToken ct = default);

    /// <summary>
    /// Marks an email as delivered.
    /// </summary>
    Task<bool> MarkDeliveredAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Marks an email as opened.
    /// </summary>
    Task<bool> MarkOpenedAsync(string externalMessageId, CancellationToken ct = default);

    /// <summary>
    /// Marks an email as bounced.
    /// </summary>
    Task<bool> MarkBouncedAsync(string externalMessageId, string? errorMessage = null, CancellationToken ct = default);

    /// <summary>
    /// Gets email logs for a reserve study.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets email logs by recipient email address.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetByRecipientAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets email logs by status.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetByStatusAsync(EmailStatus status, CancellationToken ct = default);

    /// <summary>
    /// Gets email logs by template type.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetByTemplateTypeAsync(string templateType, CancellationToken ct = default);

    /// <summary>
    /// Gets an email log by ID.
    /// </summary>
    Task<EmailLog?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets recent email logs for the current tenant.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetRecentAsync(int count = 50, CancellationToken ct = default);

    /// <summary>
    /// Gets emails that failed and should be retried.
    /// </summary>
    Task<IReadOnlyList<EmailLog>> GetPendingRetriesAsync(CancellationToken ct = default);

    /// <summary>
    /// Increments the retry count for an email.
    /// </summary>
    Task<bool> IncrementRetryCountAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets email statistics for the current tenant.
    /// </summary>
    Task<EmailStats> GetStatsAsync(DateTime? since = null, CancellationToken ct = default);
}

/// <summary>
/// Email statistics for reporting.
/// </summary>
public record EmailStats(
    int TotalSent,
    int Delivered,
    int Opened,
    int Bounced,
    int Failed,
    int Pending
);
