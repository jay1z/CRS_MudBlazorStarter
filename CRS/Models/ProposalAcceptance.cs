using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.Models;

/// <summary>
/// Records a click-wrap agreement acceptance for a proposal.
/// Captures legal acceptance details including typed signature, IP, and terms version.
/// </summary>
public class ProposalAcceptance : ITenantScoped
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    
    /// <summary>
    /// The tenant this acceptance belongs to.
    /// </summary>
    public int TenantId { get; set; }
    
    /// <summary>
    /// The reserve study this acceptance is for.
    /// </summary>
    public Guid ReserveStudyId { get; set; }
    
    [ForeignKey(nameof(ReserveStudyId))]
    public ReserveStudy? ReserveStudy { get; set; }
    
    /// <summary>
    /// Optional link to a specific proposal if tracking multiple proposals.
    /// </summary>
    public Guid? ProposalId { get; set; }
    
    [ForeignKey(nameof(ProposalId))]
    public Proposal? Proposal { get; set; }
    
    /// <summary>
    /// The user who accepted the proposal.
    /// </summary>
    public Guid AcceptedByUserId { get; set; }
    
    [ForeignKey(nameof(AcceptedByUserId))]
    public ApplicationUser? AcceptedByUser { get; set; }
    
    /// <summary>
    /// The typed signature (full legal name) entered by the user.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string TypedSignature { get; set; } = string.Empty;
    
    /// <summary>
    /// The title/role of the person accepting (e.g., "Board President", "Property Manager").
    /// </summary>
    [MaxLength(100)]
    public string? AcceptorTitle { get; set; }
    
    /// <summary>
    /// The company/organization name on behalf of which the user is accepting.
    /// </summary>
    [MaxLength(200)]
    public string? AcceptorOrganization { get; set; }
    
    /// <summary>
    /// The IP address of the user at the time of acceptance.
    /// </summary>
    [MaxLength(45)] // Supports IPv6
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// The user agent string of the browser used for acceptance.
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }
    
    /// <summary>
    /// The version of the terms that were accepted.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TermsVersion { get; set; } = "1.0";
    
    /// <summary>
    /// Reference to the terms template that was displayed.
    /// </summary>
    public Guid? AcceptanceTermsTemplateId { get; set; }
    
    [ForeignKey(nameof(AcceptanceTermsTemplateId))]
    public AcceptanceTermsTemplate? AcceptanceTermsTemplate { get; set; }
    
    /// <summary>
    /// A hash of the terms content at the time of acceptance for verification.
    /// </summary>
    [MaxLength(64)]
    public string? TermsContentHash { get; set; }
    
    /// <summary>
    /// The exact UTC timestamp when the user clicked "I Accept".
    /// </summary>
    public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether the user explicitly checked the "I have read and agree" checkbox.
    /// </summary>
    public bool CheckboxConfirmed { get; set; }
    
    /// <summary>
    /// The email address of the user at the time of acceptance (for record-keeping).
    /// </summary>
    [MaxLength(256)]
    public string? AcceptorEmail { get; set; }
    
    /// <summary>
    /// Optional additional notes or context.
    /// </summary>
    [MaxLength(1000)]
    public string? Notes { get; set; }
    
    /// <summary>
    /// Whether this acceptance is still valid (can be revoked in certain cases).
    /// </summary>
    public bool IsValid { get; set; } = true;
    
    /// <summary>
    /// If revoked, the reason for revocation.
    /// </summary>
    [MaxLength(500)]
    public string? RevocationReason { get; set; }
    
    /// <summary>
    /// If revoked, when it was revoked.
    /// </summary>
    public DateTime? RevokedAt { get; set; }
    
    /// <summary>
    /// Standard audit fields.
    /// </summary>
    public DateTime? DateCreated { get; set; } = DateTime.UtcNow;
    public DateTime? DateModified { get; set; }
    public DateTime? DateDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
