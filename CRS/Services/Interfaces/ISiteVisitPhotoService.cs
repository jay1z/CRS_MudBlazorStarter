using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service for managing site visit photos for reserve studies.
/// </summary>
public interface ISiteVisitPhotoService
{
    /// <summary>
    /// Gets all photos for a reserve study.
    /// </summary>
    Task<IReadOnlyList<SiteVisitPhoto>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets photos by category for a reserve study.
    /// </summary>
    Task<IReadOnlyList<SiteVisitPhoto>> GetByCategoryAsync(Guid reserveStudyId, PhotoCategory category, CancellationToken ct = default);

    /// <summary>
    /// Gets photos associated with a specific element.
    /// </summary>
    Task<IReadOnlyList<SiteVisitPhoto>> GetByElementAsync(Guid elementId, string elementType, CancellationToken ct = default);

    /// <summary>
    /// Gets photos marked for inclusion in the report.
    /// </summary>
    Task<IReadOnlyList<SiteVisitPhoto>> GetForReportAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets the primary photo for a reserve study.
    /// </summary>
    Task<SiteVisitPhoto?> GetPrimaryPhotoAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets a photo by ID.
    /// </summary>
    Task<SiteVisitPhoto?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new photo record.
    /// </summary>
    Task<SiteVisitPhoto> CreateAsync(SiteVisitPhoto photo, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing photo.
    /// </summary>
    Task<SiteVisitPhoto> UpdateAsync(SiteVisitPhoto photo, CancellationToken ct = default);

    /// <summary>
    /// Deletes a photo (soft delete).
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets a photo as the primary photo for a study.
    /// </summary>
    Task<bool> SetAsPrimaryAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets whether a photo should be included in the report.
    /// </summary>
    Task<bool> SetIncludeInReportAsync(Guid id, bool include, CancellationToken ct = default);

    /// <summary>
    /// Updates the condition assessment for a photo.
    /// </summary>
    Task<bool> UpdateConditionAsync(Guid id, ElementCondition condition, CancellationToken ct = default);

    /// <summary>
    /// Reorders photos for a reserve study.
    /// </summary>
    Task<bool> ReorderPhotosAsync(Guid reserveStudyId, IEnumerable<Guid> photoIdsInOrder, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of photos for a reserve study.
    /// </summary>
    Task<int> GetCountAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Associates a photo with an element.
    /// </summary>
    Task<bool> AssociateWithElementAsync(Guid photoId, Guid elementId, string elementType, CancellationToken ct = default);
}
