using CRS.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CRS.Services;

/// <summary>
/// Scoped service for tracking narrative editor state across components within a circuit.
/// Persists dirty state even when the editor component is disposed (tab switches).
/// </summary>
public class NarrativeStateService : INarrativeStateService
{
    private readonly ILogger<NarrativeStateService> _logger;

    /// <summary>
    /// The study ID that has pending changes (persists even after editor is disposed).
    /// </summary>
    private Guid? _dirtyStudyId;

    public Guid? ActiveStudyId { get; private set; }
    public bool HasUnsavedChanges => _dirtyStudyId.HasValue;
    public Func<Task>? SaveCallback { get; private set; }

    public NarrativeStateService(ILogger<NarrativeStateService> logger)
    {
        _logger = logger;
    }

    public void RegisterEditor(Guid studyId, Func<Task> saveCallback)
    {
        // If registering for a different study, clear any previous dirty state
        if (ActiveStudyId.HasValue && ActiveStudyId.Value != studyId)
        {
            _dirtyStudyId = null;
        }

        ActiveStudyId = studyId;
        SaveCallback = saveCallback;
        _logger.LogDebug("Narrative editor registered for study {StudyId}, dirty: {IsDirty}", studyId, _dirtyStudyId.HasValue);
    }

    public void UnregisterEditor(Guid studyId)
    {
        if (ActiveStudyId == studyId)
        {
            // Keep _dirtyStudyId - don't clear it! User might have unsaved changes and just switched tabs.
            ActiveStudyId = null;
            SaveCallback = null;
            _logger.LogDebug("Narrative editor unregistered for study {StudyId}, dirty state preserved: {IsDirty}", studyId, _dirtyStudyId.HasValue);
        }
    }

    public void MarkDirty()
    {
        if (ActiveStudyId.HasValue && _dirtyStudyId != ActiveStudyId)
        {
            _dirtyStudyId = ActiveStudyId;
            _logger.LogDebug("Narrative marked as dirty for study {StudyId}", ActiveStudyId);
        }
    }

    public void MarkClean()
    {
        if (_dirtyStudyId.HasValue)
        {
            _logger.LogDebug("Narrative marked as clean for study {StudyId}", _dirtyStudyId);
            _dirtyStudyId = null;
        }
    }

    /// <summary>
    /// Gets the study ID that has unsaved changes.
    /// </summary>
    public Guid? DirtyStudyId => _dirtyStudyId;

    public async Task<bool> EnsureSavedAsync(Guid studyId)
    {
        // Check if this study has unsaved changes
        if (_dirtyStudyId != studyId)
        {
            return true; // No unsaved changes for this study
        }

        // If the editor is currently active and we have a save callback, use it
        if (ActiveStudyId == studyId && SaveCallback != null)
        {
            try
            {
                _logger.LogInformation("Auto-saving narrative for study {StudyId} before report generation", studyId);
                await SaveCallback();
                MarkClean();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-save narrative for study {StudyId}", studyId);
                return false;
            }
        }

        // Editor is not active (user switched tabs) but we have dirty state
        // We can't auto-save without the editor, so return false to alert the user
        _logger.LogWarning("Cannot auto-save narrative for study {StudyId} - editor not active. User must save manually.", studyId);
        return false;
    }

    /// <summary>
    /// Checks if a specific study has unsaved changes.
    /// </summary>
    public bool HasUnsavedChangesForStudy(Guid studyId) => _dirtyStudyId == studyId;
}
