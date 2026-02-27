namespace Horizon.Services.Interfaces;

/// <summary>
/// Scoped service for tracking narrative editor state across components.
/// Allows the Reports panel to know if there are unsaved changes in the Narrative editor.
/// Persists dirty state even when the editor component is disposed (e.g., tab switches).
/// </summary>
public interface INarrativeStateService
{
    /// <summary>
    /// Gets the reserve study ID that currently has an active narrative editor.
    /// </summary>
    Guid? ActiveStudyId { get; }

    /// <summary>
    /// Gets the study ID that has unsaved changes (persists even after editor is disposed).
    /// </summary>
    Guid? DirtyStudyId { get; }

    /// <summary>
    /// Gets whether any narrative has unsaved changes.
    /// </summary>
    bool HasUnsavedChanges { get; }

    /// <summary>
    /// Gets the callback to save all sections. Set by NarrativeEditorPanel.
    /// </summary>
    Func<Task>? SaveCallback { get; }

    /// <summary>
    /// Registers the narrative editor as active for a study.
    /// Called when NarrativeEditorPanel initializes.
    /// </summary>
    void RegisterEditor(Guid studyId, Func<Task> saveCallback);

    /// <summary>
    /// Unregisters the narrative editor.
    /// Called when NarrativeEditorPanel disposes.
    /// Note: Does NOT clear dirty state - user may have unsaved changes and just switched tabs.
    /// </summary>
    void UnregisterEditor(Guid studyId);

    /// <summary>
    /// Marks the narrative as having unsaved changes.
    /// Called when content is modified in the editor.
    /// </summary>
    void MarkDirty();

    /// <summary>
    /// Marks the narrative as saved (no unsaved changes).
    /// Called after successful save.
    /// </summary>
    void MarkClean();

    /// <summary>
    /// Checks if a specific study has unsaved changes.
    /// </summary>
    bool HasUnsavedChangesForStudy(Guid studyId);

    /// <summary>
    /// Ensures any unsaved changes are saved before proceeding.
    /// Returns true if save was successful or not needed.
    /// Returns false if there are unsaved changes but the editor is not active (user must save manually).
    /// </summary>
    Task<bool> EnsureSavedAsync(Guid studyId);
}
