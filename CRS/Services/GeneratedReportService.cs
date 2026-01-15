using CRS.Data;
using CRS.Models;
using CRS.Models.Workflow;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRS.Services;

/// <summary>
/// Service for managing generated reports for reserve studies.
/// Includes workflow automation for advancing study status based on report lifecycle.
/// </summary>
public class GeneratedReportService : IGeneratedReportService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IReserveStudyWorkflowService _workflowService;
    private readonly ILogger<GeneratedReportService> _logger;

    public GeneratedReportService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        IReserveStudyWorkflowService workflowService,
        ILogger<GeneratedReportService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _workflowService = workflowService;
        _logger = logger;
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

        // Workflow automation based on report type
        await AdvanceWorkflowOnApprovalAsync(report);

        return true;
    }

    /// <summary>
    /// Advances workflow when a report is approved.
    /// </summary>
    private async Task AdvanceWorkflowOnApprovalAsync(GeneratedReport report)
    {
        try
        {
            if (report.Type == ReportType.FundingPlan)
            {
                // FundingPlan approval → FundingPlanComplete
                await _workflowService.TryTransitionStudyAsync(report.ReserveStudyId, StudyStatus.FundingPlanComplete);
                _logger.LogInformation("Advanced study {StudyId} to FundingPlanComplete on FundingPlan approval", report.ReserveStudyId);
            }
            else if (report.Type == ReportType.FullReport)
            {
                // FullReport (Narrative) approval → NarrativePackaged
                // First ensure we're at NarrativePrintReady, then advance to NarrativePackaged
                await TryAdvanceToStatusAsync(report.ReserveStudyId, StudyStatus.NarrativePackaged);
                _logger.LogInformation("Advanced study {StudyId} to NarrativePackaged on FullReport approval", report.ReserveStudyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to advance workflow on report approval for study {StudyId}", report.ReserveStudyId);
        }
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

        // Workflow automation: Published → NarrativeSent
        await AdvanceWorkflowOnPublishAsync(report);

        return true;
    }

    /// <summary>
    /// Advances workflow when a report is published to the client.
    /// </summary>
    private async Task AdvanceWorkflowOnPublishAsync(GeneratedReport report)
    {
        try
        {
            if (report.Type == ReportType.FullReport)
            {
                // Publishing the full narrative report → NarrativeSent
                await TryAdvanceToStatusAsync(report.ReserveStudyId, StudyStatus.NarrativeSent);
                _logger.LogInformation("Advanced study {StudyId} to NarrativeSent on FullReport publish", report.ReserveStudyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to advance workflow on report publish for study {StudyId}", report.ReserveStudyId);
        }
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

        // Workflow automation: Sent to client → NarrativeSent (if not already)
        if (report.Type == ReportType.FullReport)
        {
            try
            {
                await TryAdvanceToStatusAsync(report.ReserveStudyId, StudyStatus.NarrativeSent);
                _logger.LogInformation("Advanced study {StudyId} to NarrativeSent on report sent to client", report.ReserveStudyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to advance workflow on report sent for study {StudyId}", report.ReserveStudyId);
            }
        }

        return true;
    }

    public async Task<bool> RecordDownloadAsync(Guid id, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue) return false;

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var report = await context.GeneratedReports
            .FirstOrDefaultAsync(r => r.Id == id && r.DateDeleted == null, ct);

        if (report == null) return false;

        var isFirstDownload = report.DownloadCount == 0;

        report.DownloadCount++;
        report.LastDownloadedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        // Workflow automation: First client download → ReportReady (client has received the report)
        if (isFirstDownload && report.IsPublishedToClient && report.Type == ReportType.FullReport)
        {
            try
            {
                await TryAdvanceToStatusAsync(report.ReserveStudyId, StudyStatus.ReportReady);
                _logger.LogInformation("Advanced study {StudyId} to ReportReady on first client download", report.ReserveStudyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to advance workflow on first download for study {StudyId}", report.ReserveStudyId);
            }
        }

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

        /// <inheritdoc />
        public async Task<bool> MarkClientFeedbackReceivedAsync(Guid reserveStudyId, string? feedbackNotes = null, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return false;

            try
            {
                // Record feedback on the latest published report
                await using var context = await _dbFactory.CreateDbContextAsync(ct);
                var report = await context.GeneratedReports
                    .Where(r => r.ReserveStudyId == reserveStudyId && 
                               r.Type == ReportType.FullReport && 
                               r.IsPublishedToClient &&
                               r.DateDeleted == null)
                    .OrderByDescending(r => r.PublishedAt)
                    .FirstOrDefaultAsync(ct);

                if (report != null && !string.IsNullOrEmpty(feedbackNotes))
                {
                    report.InternalNotes = string.IsNullOrEmpty(report.InternalNotes)
                        ? $"[Client Feedback]: {feedbackNotes}"
                        : $"{report.InternalNotes}\n\n[Client Feedback]: {feedbackNotes}";
                    report.DateModified = DateTime.UtcNow;
                    await context.SaveChangesAsync(ct);
                }

                // Advance workflow to ReportInProcess (handling client feedback/questions)
                await TryAdvanceToStatusAsync(reserveStudyId, StudyStatus.ReportInProcess);
                _logger.LogInformation("Advanced study {StudyId} to ReportInProcess on client feedback", reserveStudyId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process client feedback for study {StudyId}", reserveStudyId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> MarkRevisionsCompleteAsync(Guid reserveStudyId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return false;

            try
            {
                // Advance workflow to ReportComplete (all revisions done, client accepted)
                await TryAdvanceToStatusAsync(reserveStudyId, StudyStatus.ReportComplete);
                _logger.LogInformation("Advanced study {StudyId} to ReportComplete", reserveStudyId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark revisions complete for study {StudyId}", reserveStudyId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CompleteStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return false;

            try
            {
                // Advance workflow to RequestCompleted (study finished successfully)
                await TryAdvanceToStatusAsync(reserveStudyId, StudyStatus.RequestCompleted);
                _logger.LogInformation("Study {StudyId} marked as completed", reserveStudyId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete study {StudyId}", reserveStudyId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ArchiveStudyAsync(Guid reserveStudyId, CancellationToken ct = default)
        {
            if (!_tenantContext.TenantId.HasValue) return false;

            try
            {
                // Archive all reports for this study
                await using var context = await _dbFactory.CreateDbContextAsync(ct);
                var reports = await context.GeneratedReports
                    .Where(r => r.ReserveStudyId == reserveStudyId && r.DateDeleted == null)
                    .ToListAsync(ct);

                foreach (var report in reports)
                {
                    report.Status = ReportStatus.Archived;
                    report.DateModified = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(ct);

                // Advance workflow to RequestArchived
                await _workflowService.TryTransitionStudyAsync(reserveStudyId, StudyStatus.RequestArchived);
                _logger.LogInformation("Study {StudyId} archived with {Count} reports", reserveStudyId, reports.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive study {StudyId}", reserveStudyId);
                return false;
            }
        }

        #region Workflow Helpers

        /// <summary>
        /// Attempts to advance the workflow to a target status by executing
        /// the necessary intermediate transitions.
        /// </summary>
        private async Task TryAdvanceToStatusAsync(Guid studyId, StudyStatus targetStatus)
        {
            // Get current status
            await using var context = await _dbFactory.CreateDbContextAsync();
            var studyRequest = await context.StudyRequests.FirstOrDefaultAsync(r => r.Id == studyId);

            if (studyRequest == null)
            {
                _logger.LogWarning("No StudyRequest found for study {StudyId}", studyId);
                return;
            }

            var currentStatus = studyRequest.CurrentStatus;

            // If already at or past target, nothing to do
            if ((int)currentStatus >= (int)targetStatus)
            {
                _logger.LogDebug("Study {StudyId} already at status {Status}, target was {Target}", 
                    studyId, currentStatus, targetStatus);
                return;
            }

            // Get the transition path
            var path = GetTransitionPath(currentStatus, targetStatus);

            if (path.Count == 0)
            {
                _logger.LogWarning("No valid transition path from {From} to {To} for study {StudyId}", 
                    currentStatus, targetStatus, studyId);
                return;
            }

            // Execute each transition
            foreach (var nextStatus in path)
            {
                var success = await _workflowService.TryTransitionStudyAsync(studyId, nextStatus);
                if (!success)
                {
                    _logger.LogWarning("Failed to transition study {StudyId} to {Status}", studyId, nextStatus);
                    break;
                }
                _logger.LogDebug("Transitioned study {StudyId} to {Status}", studyId, nextStatus);
            }
        }

        /// <summary>
        /// Gets the sequence of transitions needed to move from current to target status.
        /// </summary>
        private static List<StudyStatus> GetTransitionPath(StudyStatus current, StudyStatus target)
        {
            var path = new List<StudyStatus>();

            // Define the ordered workflow for narrative/report phase
            var workflowOrder = new[]
            {
                StudyStatus.FundingPlanReady,
                StudyStatus.FundingPlanInProcess,
                StudyStatus.FundingPlanComplete,
                StudyStatus.NarrativeReady,
                StudyStatus.NarrativeInProcess,
                StudyStatus.NarrativeComplete,
                StudyStatus.NarrativePrintReady,
                StudyStatus.NarrativePackaged,
                StudyStatus.NarrativeSent,
                StudyStatus.ReportReady,
                StudyStatus.ReportInProcess,
                StudyStatus.ReportComplete,
                StudyStatus.RequestCompleted,
                StudyStatus.RequestArchived
            };

            var currentIndex = Array.IndexOf(workflowOrder, current);
            var targetIndex = Array.IndexOf(workflowOrder, target);

            // If statuses aren't in our workflow order, can't compute path
            if (currentIndex < 0 || targetIndex < 0 || currentIndex >= targetIndex)
            {
                return path;
            }

            // Add each intermediate status
            for (int i = currentIndex + 1; i <= targetIndex; i++)
            {
                path.Add(workflowOrder[i]);
            }

            return path;
        }

        #endregion
    }
