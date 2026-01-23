using System.Security.Claims;
using CRS.Data;
using CRS.Models;
using CRS.Models.Workflow;
using CRS.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Workflow;

/// <summary>
/// Service for performing workflow actions on reserve studies with role-based access,
/// validation, and notification support.
/// </summary>
public class WorkflowActionService : IWorkflowActionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IStudyWorkflowService _workflowEngine;
    private readonly INotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<WorkflowActionService> _logger;

    public WorkflowActionService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IStudyWorkflowService workflowEngine,
        INotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<WorkflowActionService> logger)
    {
        _dbFactory = dbFactory;
        _workflowEngine = workflowEngine;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    #region Actor Resolution

    private StageActor GetCurrentUserActor()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return StageActor.None;

        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var alxRoles = user.FindAll("alx_role").Select(c => c.Value).ToHashSet();

        StageActor actor = StageActor.None;

        if (roles.Contains("PlatformAdmin") || alxRoles.Contains("PlatformAdmin"))
            actor |= StageActor.Admin;

        if (roles.Contains("TenantOwner"))
            actor |= StageActor.Staff;

        if (roles.Contains("TenantSpecialist"))
            actor |= StageActor.Specialist;

        if (roles.Contains("TenantViewer") || roles.Contains("HOAMember") || 
            (!roles.Contains("TenantOwner") && !roles.Contains("TenantSpecialist") && !roles.Contains("PlatformAdmin")))
            actor |= StageActor.HOA;

        return actor;
    }

    private string? GetCurrentUserId()
    {
        return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string GetActorName()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.Identity?.Name ?? user?.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";
    }

    #endregion

    #region Available Actions

    public async Task<AvailableActionsInfo> GetAvailableActionsAsync(Guid studyId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
        {
            return new AvailableActionsInfo
            {
                CurrentStatus = StudyStatus.RequestCreated,
                ActionMessage = "Study not found."
            };
        }

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;
        var config = StageConfiguration.GetConfig(currentStatus);
        var actor = GetCurrentUserActor();
        
        // Validate current stage
        var (isValid, validationErrors) = await ValidateStageInternalAsync(db, study, currentStatus);
        
        // Determine if it's the user's turn
        var isUsersTurn = config.AdvancedBy.HasFlag(actor) || 
                          (actor.HasFlag(StageActor.Admin) && config.AdvancedBy != StageActor.None);

        // Calculate available actions
        var canAdvance = isUsersTurn && isValid && config.AvailableActions.HasFlag(StageAction.Advance);
        var canReject = config.AvailableActions.HasFlag(StageAction.Reject) && 
                        (config.AdvancedBy.HasFlag(actor) || actor.HasFlag(StageActor.Admin));
        var canRequestChanges = config.AvailableActions.HasFlag(StageAction.RequestChanges) && 
                                 (config.AdvancedBy.HasFlag(actor) || actor.HasFlag(StageActor.Admin));
        var canCancel = config.AvailableActions.HasFlag(StageAction.Cancel) && 
                        (actor.HasFlag(StageActor.Staff) || actor.HasFlag(StageActor.Admin));
        
        // Admin-only actions
        var isAdmin = actor.HasFlag(StageActor.Admin);
        
        return new AvailableActionsInfo
        {
            CurrentStatus = currentStatus,
            StageConfig = config,
            IsUsersTurn = isUsersTurn,
            AvailableActions = config.AvailableActions,
            ActionMessage = isUsersTurn ? config.ActionRequiredMessage : string.Empty,
            WaitingMessage = !isUsersTurn ? config.WaitingMessage : string.Empty,
            PendingValidations = validationErrors,
            CanAdvance = canAdvance,
            CanReject = canReject,
            CanRequestChanges = canRequestChanges,
            CanCancel = canCancel,
            CanSkip = isAdmin,
            CanMoveBack = isAdmin,
            CanForceComplete = isAdmin
        };
    }

    #endregion

    #region Actions

    public async Task<WorkflowActionResult> AdvanceAsync(Guid studyId, string? notes = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;
        var config = StageConfiguration.GetConfig(currentStatus);
        var actor = GetCurrentUserActor();

        // Check permission
        if (!config.AdvancedBy.HasFlag(actor) && !actor.HasFlag(StageActor.Admin))
            return WorkflowActionResult.Failed("You don't have permission to advance this stage.");

        // Validate
        var (isValid, errors) = await ValidateStageInternalAsync(db, study, currentStatus);
        if (!isValid)
            return WorkflowActionResult.Blocked(errors);

        // Get next stage
        var nextStatus = StageConfiguration.GetNextStage(currentStatus, StageAction.Advance);
        if (!nextStatus.HasValue)
            return WorkflowActionResult.Failed("No next stage defined for this status.");

        // Perform transition
        return await PerformTransitionAsync(db, study, currentStatus, nextStatus.Value, notes, "Advanced");
    }

    public async Task<WorkflowActionResult> RejectAsync(Guid studyId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return WorkflowActionResult.Failed("Rejection reason is required.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;
        var config = StageConfiguration.GetConfig(currentStatus);

        if (!config.AvailableActions.HasFlag(StageAction.Reject))
            return WorkflowActionResult.Failed("Rejection is not allowed at this stage.");

        var nextStatus = StageConfiguration.GetNextStage(currentStatus, StageAction.Reject);
        if (!nextStatus.HasValue)
            nextStatus = StudyStatus.RequestCancelled;

        return await PerformTransitionAsync(db, study, currentStatus, nextStatus.Value, reason, "Rejected");
    }

    public async Task<WorkflowActionResult> RequestChangesAsync(Guid studyId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return WorkflowActionResult.Failed("Reason for changes is required.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;
        var config = StageConfiguration.GetConfig(currentStatus);

        if (!config.AvailableActions.HasFlag(StageAction.RequestChanges))
            return WorkflowActionResult.Failed("Requesting changes is not allowed at this stage.");

        var nextStatus = StageConfiguration.GetNextStage(currentStatus, StageAction.RequestChanges);
        if (!nextStatus.HasValue)
            return WorkflowActionResult.Failed("No rollback stage defined for this action.");

        return await PerformTransitionAsync(db, study, currentStatus, nextStatus.Value, reason, "Changes Requested");
    }

    public async Task<WorkflowActionResult> CancelAsync(Guid studyId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return WorkflowActionResult.Failed("Cancellation reason is required.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var actor = GetCurrentUserActor();
        if (!actor.HasFlag(StageActor.Staff) && !actor.HasFlag(StageActor.Admin))
            return WorkflowActionResult.Failed("Only staff or admin can cancel a study.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;

        // Store the previous status for potential reversal
        if (study.StudyRequest != null)
        {
            study.StudyRequest.PreviousStatus = currentStatus;
        }

        return await PerformTransitionAsync(db, study, currentStatus, StudyStatus.RequestCancelled, reason, "Cancelled");
    }

    public async Task<WorkflowActionResult> ReverseCancellationAsync(Guid studyId, StudyStatus restoreToStatus)
    {
        var actor = GetCurrentUserActor();
        if (!actor.HasFlag(StageActor.Admin))
            return WorkflowActionResult.Failed("Only admins can reverse a cancellation.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;
        if (currentStatus != StudyStatus.RequestCancelled)
            return WorkflowActionResult.Failed("Study is not cancelled.");

        // Use the stored previous status if no specific status provided
        var targetStatus = restoreToStatus;
        if (study.StudyRequest?.PreviousStatus.HasValue == true && restoreToStatus == default)
        {
            targetStatus = study.StudyRequest.PreviousStatus.Value;
        }

        return await PerformTransitionAsync(db, study, currentStatus, targetStatus, "Cancellation reversed by admin", "Cancellation Reversed");
    }

    #endregion

    #region Admin Actions

    public async Task<WorkflowActionResult> SkipToStageAsync(Guid studyId, StudyStatus targetStatus, string reason)
    {
        var actor = GetCurrentUserActor();
        if (!actor.HasFlag(StageActor.Admin))
            return WorkflowActionResult.Failed("Only admins can skip stages.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;

        return await PerformTransitionAsync(db, study, currentStatus, targetStatus, $"Admin skip: {reason}", "Skipped");
    }

    public async Task<WorkflowActionResult> MoveBackAsync(Guid studyId, StudyStatus targetStatus, string reason)
    {
        var actor = GetCurrentUserActor();
        if (!actor.HasFlag(StageActor.Admin))
            return WorkflowActionResult.Failed("Only admins can move back stages.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;

        // Validate that target is before current
        if ((int)targetStatus >= (int)currentStatus)
            return WorkflowActionResult.Failed("Target status must be before the current status.");

        return await PerformTransitionAsync(db, study, currentStatus, targetStatus, $"Admin rollback: {reason}", "Moved Back");
    }

    public async Task<WorkflowActionResult> ForceCompleteAsync(Guid studyId, string reason)
    {
        var actor = GetCurrentUserActor();
        if (!actor.HasFlag(StageActor.Admin))
            return WorkflowActionResult.Failed("Only admins can force complete a study.");

        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.Community)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return WorkflowActionResult.Failed("Study not found.");

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;

        // Mark study as complete
        study.IsComplete = true;
        study.DateModified = DateTime.UtcNow;

        return await PerformTransitionAsync(db, study, currentStatus, StudyStatus.RequestCompleted, $"Force completed by admin: {reason}", "Force Completed");
    }

    #endregion

    #region Validation

    public async Task<(bool IsValid, string[] Errors)> ValidateStageAsync(Guid studyId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var study = await db.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.CurrentProposal)
            .Include(s => s.FinancialInfo)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
            return (false, new[] { "Study not found." });

        var currentStatus = study.StudyRequest?.CurrentStatus ?? study.CurrentStatus;
        return await ValidateStageInternalAsync(db, study, currentStatus);
    }

    private async Task<(bool IsValid, string[] Errors)> ValidateStageInternalAsync(
        ApplicationDbContext db, ReserveStudy study, StudyStatus status)
    {
        var config = StageConfiguration.GetConfig(status);
        var errors = new List<string>();

        foreach (var validation in config.RequiredValidations)
        {
            var (isValid, error) = await CheckValidationAsync(db, study, validation);
            if (!isValid && error != null)
                errors.Add(error);
        }

        return (errors.Count == 0, errors.ToArray());
    }

    private async Task<(bool IsValid, string? Error)> CheckValidationAsync(
        ApplicationDbContext db, ReserveStudy study, StageValidation validation)
    {
        return validation switch
        {
            StageValidation.None => (true, null),
            
            // Proposal validations - use existing properties
            StageValidation.ProposalDocumentAttached => 
                (study.CurrentProposal != null, 
                 "Proposal must be created."),
                 
            StageValidation.ProposalReviewed => 
                (study.CurrentProposal != null, 
                 "Proposal must exist."),
                 
            StageValidation.ESignEnvelopeCreated => 
                (study.CurrentProposal?.DateSent != null, 
                 "Proposal must be sent."),
                 
            StageValidation.SignedAcceptanceReceived => 
                (await db.ProposalAcceptances.AnyAsync(a => a.ReserveStudyId == study.Id && a.AcceptedAt != null), 
                 "Signed acceptance must be received."),
                 
            StageValidation.ServiceContactsProvided => 
                await ValidateServiceContactsAsync(db, study),
                
            StageValidation.FinancialInfoFormSent => 
                (study.FinancialInfo != null, 
                 "Financial info request must be sent."),
                 
            StageValidation.FinancialInfoComplete => 
                (study.FinancialInfo?.IsComplete == true, 
                 "Financial information must be complete."),
                 
            StageValidation.FinancialInfoReviewed => 
                (study.FinancialInfo?.DateReviewed != null, 
                 "Financial information must be reviewed."),
                 
            StageValidation.SiteVisitDateSelected => 
                (true, null), // TODO: Implement site visit date check
                 
            StageValidation.SiteVisitPhotosUploaded => 
                (true, null), // TODO: Implement photo check
                
            StageValidation.SiteVisitDataComplete => 
                (true, null), // TODO: Implement data completeness check
                
            StageValidation.FundingPlanCreated => 
                (true, null), // TODO: Implement funding plan check
                
            StageValidation.FundingPlanComplete => 
                (true, null), // TODO: Implement funding plan completion check
                
            StageValidation.NarrativeCreated => 
                (await db.Narratives.AnyAsync(n => n.ReserveStudyId == study.Id && n.DateDeleted == null), 
                 "Narrative must be created."),

            StageValidation.NarrativeComplete => 
                (await db.Narratives.AnyAsync(n => n.ReserveStudyId == study.Id && 
                                                   n.DateDeleted == null && 
                                                   (n.Status == NarrativeStatus.Approved || n.Status == NarrativeStatus.Published)), 
                 "Narrative must be completed and approved."),
                
            StageValidation.ReportAttached => 
                (study.IsComplete, 
                 "Study must be marked complete."),
                 
            StageValidation.ReportApproved => 
                (study.DateApproved != null || study.IsApproved, 
                 "Report must be approved."),
                 
            StageValidation.CancellationReasonProvided => 
                (true, null), // Checked at action level
                
                
            _ => (true, null)
        };
    }
    
    private async Task<(bool IsValid, string? Error)> ValidateServiceContactsAsync(ApplicationDbContext db, ReserveStudy study)
    {
        // Check if tenant requires service contacts
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == study.TenantId);
        if (tenant?.RequireServiceContacts != true)
        {
            return (true, null); // Setting not enabled, skip validation
        }
        
        // Check if any common elements in this study have service contacts assigned
        var hasAssignedContacts = await db.ReserveStudyCommonElements
            .AnyAsync(rsce => rsce.ReserveStudyId == study.Id && rsce.ServiceContact != null);
        
        // Alternatively, check if tenant has any active service contacts that could be used
        if (!hasAssignedContacts)
        {
            var tenantHasContacts = await db.ServiceContacts
                .AnyAsync(sc => sc.TenantId == study.TenantId && sc.IsActive);
            
            if (!tenantHasContacts)
            {
                return (false, "At least one service contact is required. Please add vendor/contractor contacts before proceeding.");
            }
        }
        
        return (true, null);
    }

    #endregion

    #region Transition Helper

    private async Task<WorkflowActionResult> PerformTransitionAsync(
        ApplicationDbContext db,
        ReserveStudy study,
        StudyStatus fromStatus,
        StudyStatus toStatus,
        string? notes,
        string actionType)
    {
        try
        {
            var actorName = GetActorName();
            
            // Ensure StudyRequest exists
            var studyRequest = study.StudyRequest;
            if (studyRequest == null)
            {
                studyRequest = new StudyRequest
                {
                    Id = study.Id,
                    TenantId = study.TenantId,
                    CommunityId = study.CommunityId ?? Guid.Empty,
                    CurrentStatus = fromStatus,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow,
                    StateChangedAt = DateTimeOffset.UtcNow
                };
                db.StudyRequests.Add(studyRequest);
                study.StudyRequest = studyRequest;
            }

            // Update the status
            studyRequest.CurrentStatus = toStatus;
            studyRequest.UpdatedAt = DateTimeOffset.UtcNow;
            studyRequest.StateChangedAt = studyRequest.UpdatedAt;
            studyRequest.StatusChangedBy = actorName;

            // Update study
            study.DateModified = DateTime.UtcNow;
            if (toStatus == StudyStatus.RequestCompleted)
                study.IsComplete = true;

            // Log history
            var history = new StudyStatusHistory
            {
                TenantId = study.TenantId,
                RequestId = study.Id,
                FromStatus = fromStatus,
                ToStatus = toStatus,
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedBy = actorName,
                Notes = notes,
                Source = GetSource(),
                CorrelationId = GetCorrelationId()
            };
            db.StudyStatusHistories.Add(history);

            await db.SaveChangesAsync();

            // Send notifications
            await SendNotificationsAsync(study, fromStatus, toStatus);

            _logger.LogInformation(
                "Workflow {ActionType}: Study {StudyId} transitioned from {FromStatus} to {ToStatus} by {Actor}",
                actionType, study.Id, fromStatus, toStatus, actorName);

            return WorkflowActionResult.Succeeded(toStatus, $"{actionType} successfully. Status: {toStatus.ToDisplayTitle()}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform workflow transition for study {StudyId}", study.Id);
            return WorkflowActionResult.Failed($"Failed to complete action: {ex.Message}");
        }
    }

    private async Task SendNotificationsAsync(ReserveStudy study, StudyStatus fromStatus, StudyStatus toStatus)
    {
        try
        {
            var config = StageConfiguration.GetConfig(toStatus);
            if (config.NotifyOnEnter != StageActor.None)
            {
                await _notificationService.OnStateChangedAsync(
                    study.StudyRequest!, 
                    fromStatus, 
                    toStatus);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notifications for study {StudyId}", study.Id);
            // Don't fail the transition if notifications fail
        }
    }

    private string GetSource()
    {
        var path = _httpContextAccessor.HttpContext?.Request?.Path.ToString() ?? string.Empty;
        return path.Contains("/api/", StringComparison.OrdinalIgnoreCase) ? "API" : "UI";
    }

    private string? GetCorrelationId()
    {
        var headers = _httpContextAccessor.HttpContext?.Request?.Headers;
        if (headers != null && headers.TryGetValue("X-Correlation-Id", out var values))
            return values.FirstOrDefault();
        return null;
    }

    #endregion
}
