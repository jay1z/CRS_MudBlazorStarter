using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service for managing study notes and comments.
/// </summary>
public interface IStudyNoteService
{
    /// <summary>
    /// Gets all notes for a reserve study.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets notes by visibility level for a reserve study.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetByVisibilityAsync(Guid reserveStudyId, NoteVisibility visibility, CancellationToken ct = default);

    /// <summary>
    /// Gets client-visible notes for a reserve study.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetClientVisibleNotesAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets pinned notes for a reserve study.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetPinnedNotesAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets unresolved notes for a reserve study.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetUnresolvedNotesAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets notes by type for a reserve study.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetByTypeAsync(Guid reserveStudyId, NoteType type, CancellationToken ct = default);

    /// <summary>
    /// Gets notes by author.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetByAuthorAsync(Guid authorUserId, CancellationToken ct = default);

    /// <summary>
    /// Gets replies to a note.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetRepliesAsync(Guid parentNoteId, CancellationToken ct = default);

    /// <summary>
    /// Gets a note by ID with its replies.
    /// </summary>
    Task<StudyNote?> GetByIdWithRepliesAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a note by ID.
    /// </summary>
    Task<StudyNote?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new note.
    /// </summary>
    Task<StudyNote> CreateAsync(StudyNote note, CancellationToken ct = default);

    /// <summary>
    /// Creates a reply to an existing note.
    /// </summary>
    Task<StudyNote> CreateReplyAsync(Guid parentNoteId, StudyNote reply, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    Task<StudyNote> UpdateAsync(StudyNote note, CancellationToken ct = default);

    /// <summary>
    /// Deletes a note (soft delete).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Pins or unpins a note.
    /// </summary>
    Task<bool> SetPinnedAsync(Guid id, bool isPinned, CancellationToken ct = default);

    /// <summary>
    /// Resolves a note.
    /// </summary>
    Task<bool> ResolveAsync(Guid id, Guid resolvedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Reopens a resolved note.
    /// </summary>
    Task<bool> ReopenAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Changes the visibility of a note.
    /// </summary>
    Task<bool> SetVisibilityAsync(Guid id, NoteVisibility visibility, CancellationToken ct = default);

    /// <summary>
    /// Gets notes that mention a specific user.
    /// </summary>
    Task<IReadOnlyList<StudyNote>> GetMentionsForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of notes for a reserve study.
    /// </summary>
    Task<int> GetCountAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of unresolved notes for a reserve study.
    /// </summary>
    Task<int> GetUnresolvedCountAsync(Guid reserveStudyId, CancellationToken ct = default);
}
