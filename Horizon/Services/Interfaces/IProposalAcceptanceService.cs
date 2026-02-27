using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for managing click-wrap proposal acceptances.
/// </summary>
public interface IProposalAcceptanceService
{
    /// <summary>
    /// Gets the acceptance record for a reserve study, if any.
    /// </summary>
    Task<ProposalAcceptance?> GetAcceptanceByStudyIdAsync(Guid reserveStudyId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all acceptances for a reserve study (including revoked).
    /// </summary>
    Task<IReadOnlyList<ProposalAcceptance>> GetAcceptanceHistoryAsync(Guid reserveStudyId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets an acceptance by ID.
    /// </summary>
    Task<ProposalAcceptance?> GetByIdAsync(Guid id, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a reserve study has a valid (non-revoked) acceptance.
    /// </summary>
    Task<bool> HasValidAcceptanceAsync(Guid reserveStudyId, CancellationToken ct = default);
    
    /// <summary>
    /// Records a new click-wrap acceptance.
    /// </summary>
    Task<ProposalAcceptance> RecordAcceptanceAsync(ProposalAcceptance acceptance, CancellationToken ct = default);
    
    /// <summary>
    /// Revokes an acceptance (rare, for legal/dispute reasons).
    /// </summary>
    Task<bool> RevokeAcceptanceAsync(Guid acceptanceId, string reason, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the currently active terms template for a tenant and type.
    /// </summary>
    Task<AcceptanceTermsTemplate?> GetActiveTermsTemplateAsync(TermsType type = TermsType.ProposalAcceptance, CancellationToken ct = default);
    
    /// <summary>
    /// Gets the default terms template for a tenant and type.
    /// </summary>
    Task<AcceptanceTermsTemplate?> GetDefaultTermsTemplateAsync(TermsType type = TermsType.ProposalAcceptance, CancellationToken ct = default);
    
    /// <summary>
    /// Gets all terms templates for a tenant.
    /// </summary>
    Task<IReadOnlyList<AcceptanceTermsTemplate>> GetAllTermsTemplatesAsync(TermsType? type = null, CancellationToken ct = default);
    
    /// <summary>
    /// Creates a new terms template version.
    /// </summary>
    Task<AcceptanceTermsTemplate> CreateTermsTemplateAsync(AcceptanceTermsTemplate template, CancellationToken ct = default);
    
    /// <summary>
    /// Updates a terms template.
    /// </summary>
    Task<AcceptanceTermsTemplate> UpdateTermsTemplateAsync(AcceptanceTermsTemplate template, CancellationToken ct = default);
    
    /// <summary>
    /// Sets a template as the default for its type.
    /// </summary>
    Task<bool> SetDefaultTemplateAsync(Guid templateId, CancellationToken ct = default);
    
    /// <summary>
    /// Computes a SHA-256 hash of the terms text for verification.
    /// </summary>
    string ComputeTermsHash(string termsText);
    
    /// <summary>
    /// Seeds default proposal acceptance terms if none exist.
    /// </summary>
    Task SeedDefaultTermsIfNeededAsync(CancellationToken ct = default);
}
