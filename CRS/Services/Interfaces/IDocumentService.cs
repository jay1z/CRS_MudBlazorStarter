using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for managing documents associated with reserve studies and communities.
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Gets all documents for a reserve study.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets all documents for a community.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByCommunityAsync(Guid communityId, CancellationToken ct = default);

    /// <summary>
    /// Gets documents by type for the current tenant.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByTypeAsync(DocumentType type, CancellationToken ct = default);

    /// <summary>
    /// Gets public documents for a reserve study (visible to HOA users).
    /// </summary>
    Task<IReadOnlyList<Document>> GetPublicDocumentsAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets all public documents for the current tenant (visible to HOA users).
    /// </summary>
    Task<IReadOnlyList<Document>> GetPublicDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a document by ID.
    /// </summary>
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new document record.
    /// </summary>
    Task<Document> CreateAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing document.
    /// </summary>
    Task<Document> UpdateAsync(Document document, CancellationToken ct = default);

    /// <summary>
    /// Soft deletes a document.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Sets the public visibility of a document.
    /// </summary>
    Task<bool> SetPublicAsync(Guid id, bool isPublic, CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of documents for the current tenant.
    /// </summary>
    Task<int> GetCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets recent documents for the current tenant.
    /// </summary>
    Task<IReadOnlyList<Document>> GetRecentAsync(int count = 10, CancellationToken ct = default);

    /// <summary>
    /// Searches documents by filename or description.
    /// </summary>
    Task<IReadOnlyList<Document>> SearchAsync(string searchTerm, CancellationToken ct = default);
}
