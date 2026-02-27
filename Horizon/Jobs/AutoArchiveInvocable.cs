using Coravel.Invocable;
using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Workflow;
using Horizon.Services.Interfaces;
using Horizon.Services.Workflow;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Jobs;

/// <summary>
/// Scheduled job to auto-archive completed studies based on tenant settings.
/// Runs daily via Coravel scheduler.
/// </summary>
public class AutoArchiveInvocable : IInvocable
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IStudyWorkflowService _workflowService;
    private readonly ILogger<AutoArchiveInvocable> _logger;

    public AutoArchiveInvocable(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IStudyWorkflowService workflowService,
        ILogger<AutoArchiveInvocable> logger)
    {
        _dbFactory = dbFactory;
        _workflowService = workflowService;
        _logger = logger;
    }

    public async Task Invoke()
    {
        _logger.LogInformation("Starting auto-archive job at {Time}", DateTime.UtcNow);

        try
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Get all tenants with auto-archive enabled (AutoArchiveAfterDays > 0)
            var tenantsWithAutoArchive = await context.Tenants
                .Where(t => t.ProvisioningStatus == TenantProvisioningStatus.Active &&
                            t.AutoArchiveAfterDays > 0)
                .ToDictionaryAsync(t => t.Id, t => t.AutoArchiveAfterDays);

            if (tenantsWithAutoArchive.Count == 0)
            {
                _logger.LogInformation("No tenants have auto-archive enabled. Skipping.");
                return;
            }

            _logger.LogInformation("Found {Count} tenants with auto-archive enabled", tenantsWithAutoArchive.Count);

            // Get all completed studies that are not yet archived
            var completedStudies = await context.StudyRequests
                .Include(sr => sr.ReserveStudy)
                .Where(sr => sr.CurrentStatus == StudyStatus.RequestCompleted &&
                             tenantsWithAutoArchive.Keys.Contains(sr.TenantId))
                .ToListAsync();

            _logger.LogInformation("Found {Count} completed studies to check for auto-archive", completedStudies.Count);

            var archivedCount = 0;
            var today = DateTime.UtcNow.Date;

            foreach (var studyRequest in completedStudies)
            {
                try
                {
                    if (!tenantsWithAutoArchive.TryGetValue(studyRequest.TenantId, out var archiveAfterDays))
                        continue;

                    // Calculate days since completion
                    var completedDate = studyRequest.StateChangedAt.Date;
                    var daysSinceCompletion = (today - completedDate).Days;

                    if (daysSinceCompletion >= archiveAfterDays)
                    {
                        // Archive the study
                        var success = await _workflowService.TryTransitionAsync(
                            studyRequest, 
                            StudyStatus.RequestArchived, 
                            "System (Auto-Archive)");

                        if (success)
                        {
                            studyRequest.UpdatedAt = DateTimeOffset.UtcNow;
                            studyRequest.StateChangedAt = DateTimeOffset.UtcNow;
                            studyRequest.StatusChangedBy = "System (Auto-Archive)";
                            
                            archivedCount++;
                            _logger.LogInformation(
                                "Auto-archived study {StudyId} for tenant {TenantId} ({Days} days after completion)",
                                studyRequest.Id, studyRequest.TenantId, daysSinceCompletion);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Failed to auto-archive study {StudyId} - workflow transition failed",
                                studyRequest.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing auto-archive for study {StudyId}", studyRequest.Id);
                }
            }

            // Save all changes
            if (archivedCount > 0)
            {
                await context.SaveChangesAsync();
            }

            _logger.LogInformation("Auto-archive job completed. Archived {Count} studies.", archivedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running auto-archive job");
            throw;
        }
    }
}
