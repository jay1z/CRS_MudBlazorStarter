using System.ComponentModel.DataAnnotations;
using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Template for the legal terms and conditions shown during click-wrap acceptance.
/// Supports version tracking for legal compliance.
/// </summary>
public class AcceptanceTermsTemplate : ITenantScoped
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    
    /// <summary>
    /// The tenant this template belongs to. Null for platform-wide templates.
    /// </summary>
    public int TenantId { get; set; }
    
    /// <summary>
    /// Version identifier (e.g., "1.0", "2.0", "2024-01").
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Version { get; set; } = "1.0";
    
    /// <summary>
    /// Short name for the template (e.g., "Proposal Acceptance Terms").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The type of terms this template is for.
    /// </summary>
    public TermsType Type { get; set; } = TermsType.ProposalAcceptance;
    
    /// <summary>
    /// The full text of the terms and conditions (supports Markdown).
    /// </summary>
    [Required]
    public string TermsText { get; set; } = string.Empty;
    
    /// <summary>
    /// A shorter summary of key points (optional).
    /// </summary>
    [MaxLength(2000)]
    public string? Summary { get; set; }
    
    /// <summary>
    /// The text displayed next to the checkbox (e.g., "I have read and agree to the terms above").
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string CheckboxText { get; set; } = "I have read and agree to the terms and conditions above.";
    
    /// <summary>
    /// The text displayed on the accept button.
    /// </summary>
    [MaxLength(50)]
    public string AcceptButtonText { get; set; } = "Accept Proposal";
    
    /// <summary>
    /// SHA-256 hash of the TermsText for integrity verification.
    /// </summary>
    [MaxLength(64)]
    public string? ContentHash { get; set; }
    
    /// <summary>
    /// When these terms become effective.
    /// </summary>
    public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When these terms expire (null = no expiration).
    /// </summary>
    public DateTime? ExpirationDate { get; set; }
    
    /// <summary>
    /// Whether this template is currently active and can be used.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether this is the default template for new proposals.
    /// </summary>
    public bool IsDefault { get; set; } = false;
    
    /// <summary>
    /// The user who created this version.
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
    
    /// <summary>
    /// Notes about changes in this version.
    /// </summary>
    [MaxLength(1000)]
    public string? VersionNotes { get; set; }
    
    /// <summary>
    /// Reference to the previous version this supersedes.
    /// </summary>
    public Guid? PreviousVersionId { get; set; }
    
    /// <summary>
    /// Standard audit fields.
    /// </summary>
    public DateTime? DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateModified { get; set; }
    public DateTime? DateDeleted { get; set; }
    
    /// <summary>
    /// Navigation property for acceptances using this template.
    /// </summary>
    public ICollection<ProposalAcceptance>? Acceptances { get; set; }
}

/// <summary>
/// Types of terms templates.
/// </summary>
public enum TermsType
{
    /// <summary>
    /// Terms for accepting a reserve study proposal.
    /// </summary>
    ProposalAcceptance = 0,
    
    /// <summary>
    /// Terms of service for platform usage.
    /// </summary>
    TermsOfService = 1,
    
    /// <summary>
    /// Privacy policy acceptance.
    /// </summary>
    PrivacyPolicy = 2,
    
    /// <summary>
    /// Data processing agreement.
    /// </summary>
    DataProcessingAgreement = 3,
    
    /// <summary>
    /// Service level agreement.
    /// </summary>
    ServiceLevelAgreement = 4,
    
    /// <summary>
    /// Other/custom terms.
    /// </summary>
    Other = 99
}
