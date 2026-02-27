using Horizon.Models;

namespace Horizon.Services.Interfaces;

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
    /// Marks a report as superseded by a newer version.
    /// </summary>
    /// <param name="id">The report ID to mark as superseded.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> MarkAsSupersededAsync(Guid id, CancellationToken ct = default);

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

        // ═══════════════════════════════════════════════════════════════
        // WORKFLOW AUTOMATION METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Records that client feedback/questions have been received.
        /// Advances workflow to ReportInProcess.
        /// </summary>
        /// <param name="reserveStudyId">The reserve study ID.</param>
        /// <param name="feedbackNotes">Optional notes about the feedback.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<bool> MarkClientFeedbackReceivedAsync(Guid reserveStudyId, string? feedbackNotes = null, CancellationToken ct = default);

        /// <summary>
        /// Marks that all client revisions have been addressed and the client has accepted.
        /// Advances workflow to ReportComplete.
        /// </summary>
        /// <param name="reserveStudyId">The reserve study ID.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<bool> MarkRevisionsCompleteAsync(Guid reserveStudyId, CancellationToken ct = default);

        /// <summary>
        /// Completes the study (marks as successfully finished).
        /// Advances workflow to RequestCompleted.
        /// </summary>
        /// <param name="reserveStudyId">The reserve study ID.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<bool> CompleteStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

        /// <summary>
        /// Archives a completed study and all its reports.
        /// Advances workflow to RequestArchived.
        /// </summary>
        /// <param name="reserveStudyId">The reserve study ID.</param>
        /// <param name="ct">Cancellation token.</param>
        Task<bool> ArchiveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

        // ═══════════════════════════════════════════════════════════════
        // EMAIL DELIVERY METHODS
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Sends a report to the client via email.
        /// </summary>
        /// <param name="reportId">The report ID to send.</param>
        /// <param name="recipientEmail">Primary recipient email address.</param>
        /// <param name="subject">Email subject line.</param>
        /// <param name="personalMessage">Optional personal message to include.</param>
        /// <param name="ccEmail">Optional CC email address.</param>
        /// <param name="includeDownloadLink">Whether to include a secure download link.</param>
        /// <param name="attachReport">Whether to attach the report PDF to the email.</param>
        /// <param name="sentByUserId">The user ID of the person sending the email.</param>
        /// <param name="baseUrl">Base URL for generating download links.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if email was sent successfully.</returns>
        Task<bool> SendToClientAsync(
            Guid reportId,
            string recipientEmail,
            string subject,
            string? personalMessage,
            string? ccEmail,
            bool includeDownloadLink,
            bool attachReport,
            Guid sentByUserId,
            string baseUrl,
            CancellationToken ct = default);
    }
