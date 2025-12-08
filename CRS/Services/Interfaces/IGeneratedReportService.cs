using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service for managing generated reports for reserve studies.
/// </summary>
public interface IGeneratedReportService
{
    /// <summary>
    /// Gets all reports for a reserve study.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets reports by type for a reserve study.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetByTypeAsync(Guid reserveStudyId, ReportType type, CancellationToken ct = default);

    /// <summary>
    /// Gets reports by status for a reserve study.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetByStatusAsync(Guid reserveStudyId, ReportStatus status, CancellationToken ct = default);

    /// <summary>
    /// Gets published reports for a reserve study.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetPublishedReportsAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets the latest report of a specific type for a reserve study.
    /// </summary>
    Task<GeneratedReport?> GetLatestByTypeAsync(Guid reserveStudyId, ReportType type, CancellationToken ct = default);

    /// <summary>
    /// Gets reports pending review.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetPendingReviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a report by ID.
    /// </summary>
    Task<GeneratedReport?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new report record.
    /// </summary>
    Task<GeneratedReport> CreateAsync(GeneratedReport report, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing report.
    /// </summary>
    Task<GeneratedReport> UpdateAsync(GeneratedReport report, CancellationToken ct = default);

    /// <summary>
    /// Deletes a report (soft delete).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Submits a report for review.
    /// </summary>
    Task<bool> SubmitForReviewAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Approves a report after review.
    /// </summary>
    Task<bool> ApproveAsync(Guid id, Guid reviewedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Requests revisions for a report.
    /// </summary>
    Task<bool> RequestRevisionsAsync(Guid id, Guid reviewedByUserId, string? notes = null, CancellationToken ct = default);

    /// <summary>
    /// Publishes a report to the client.
    /// </summary>
    Task<bool> PublishAsync(Guid id, Guid publishedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Marks a report as sent to the client.
    /// </summary>
    Task<bool> MarkSentToClientAsync(Guid id, string email, CancellationToken ct = default);

    /// <summary>
    /// Records a download of the report.
    /// </summary>
    Task<bool> RecordDownloadAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new version of an existing report.
    /// </summary>
    Task<GeneratedReport> CreateNewVersionAsync(Guid existingReportId, GeneratedReport newVersion, CancellationToken ct = default);

    /// <summary>
    /// Archives a report.
    /// </summary>
    Task<bool> ArchiveAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets the version history for a report.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetVersionHistoryAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets recent reports for the current tenant.
    /// </summary>
    Task<IReadOnlyList<GeneratedReport>> GetRecentAsync(int count = 20, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of reports for a reserve study.
    /// </summary>
    Task<int> GetCountAsync(Guid reserveStudyId, CancellationToken ct = default);
}
