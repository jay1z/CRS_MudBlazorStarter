using Horizon.Models.Workflow;

namespace Horizon.Services.Workflow;

/// <summary>
/// Result of a workflow action attempt.
/// </summary>
public class WorkflowActionResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public StudyStatus? NewStatus { get; init; }
    public string[] ValidationErrors { get; init; } = Array.Empty<string>();
    
    public static WorkflowActionResult Succeeded(StudyStatus newStatus, string message = "Action completed successfully.")
        => new() { Success = true, NewStatus = newStatus, Message = message };
        
    public static WorkflowActionResult Failed(string message, params string[] validationErrors)
        => new() { Success = false, Message = message, ValidationErrors = validationErrors };
        
    public static WorkflowActionResult Blocked(string[] validationErrors)
        => new() { Success = false, Message = "Cannot advance: validation requirements not met.", ValidationErrors = validationErrors };
}

/// <summary>
/// Information about what actions are available for the current user on a study.
/// </summary>
public class AvailableActionsInfo
{
    public StudyStatus CurrentStatus { get; init; }
    public StageConfig StageConfig { get; init; } = null!;
    public bool IsUsersTurn { get; init; }
    public StageAction AvailableActions { get; init; }
    public string ActionMessage { get; init; } = string.Empty;
    public string WaitingMessage { get; init; } = string.Empty;
    public string[] PendingValidations { get; init; } = Array.Empty<string>();
    public bool CanAdvance { get; init; }
    public bool CanReject { get; init; }
    public bool CanRequestChanges { get; init; }
    public bool CanCancel { get; init; }
    
    // Admin-only actions
    public bool CanSkip { get; init; }
    public bool CanMoveBack { get; init; }
    public bool CanForceComplete { get; init; }
}

/// <summary>
/// Service for performing workflow actions on reserve studies.
/// </summary>
public interface IWorkflowActionService
{
    /// <summary>
    /// Gets the available actions for the current user on a study.
    /// </summary>
    Task<AvailableActionsInfo> GetAvailableActionsAsync(Guid studyId);
    
    /// <summary>
    /// Advances the study to the next stage.
    /// </summary>
    Task<WorkflowActionResult> AdvanceAsync(Guid studyId, string? notes = null);
    
    /// <summary>
    /// Rejects the study (e.g., proposal rejection by HOA).
    /// </summary>
    Task<WorkflowActionResult> RejectAsync(Guid studyId, string reason);
    
    /// <summary>
    /// Requests changes and moves back to an earlier stage.
    /// </summary>
    Task<WorkflowActionResult> RequestChangesAsync(Guid studyId, string reason);
    
    /// <summary>
    /// Cancels the study request.
    /// </summary>
    Task<WorkflowActionResult> CancelAsync(Guid studyId, string reason);
    
    /// <summary>
    /// Reverses a cancellation (admin only).
    /// </summary>
    Task<WorkflowActionResult> ReverseCancellationAsync(Guid studyId, StudyStatus restoreToStatus);
    
    /// <summary>
    /// Skips to a specific stage (admin only).
    /// </summary>
    Task<WorkflowActionResult> SkipToStageAsync(Guid studyId, StudyStatus targetStatus, string reason);
    
    /// <summary>
    /// Moves back to an earlier stage (admin only).
    /// </summary>
    Task<WorkflowActionResult> MoveBackAsync(Guid studyId, StudyStatus targetStatus, string reason);
    
    /// <summary>
    /// Forces the study to complete (admin only).
    /// </summary>
    Task<WorkflowActionResult> ForceCompleteAsync(Guid studyId, string reason);
    
    /// <summary>
    /// Validates whether the current stage requirements are met.
    /// </summary>
    Task<(bool IsValid, string[] Errors)> ValidateStageAsync(Guid studyId);
}
