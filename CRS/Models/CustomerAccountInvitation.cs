using System.ComponentModel.DataAnnotations;

namespace CRS.Models;

/// <summary>
/// Represents a pending invitation to join a customer account.
/// </summary>
public class CustomerAccountInvitation
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    /// <summary>
    /// The customer account being invited to
    /// </summary>
    public Guid CustomerAccountId { get; set; }
    public CustomerAccount CustomerAccount { get; set; } = null!;

    /// <summary>
    /// Email address of the person being invited
    /// </summary>
    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Role the user will have when they accept
    /// </summary>
    public CustomerAccountRole Role { get; set; } = CustomerAccountRole.Member;

    /// <summary>
    /// Secure token for accepting the invitation
    /// </summary>
    public string Token { get; set; } = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

    /// <summary>
    /// When the invitation was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the invitation expires
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(7);

    /// <summary>
    /// Who sent the invitation
    /// </summary>
    public Guid InvitedByUserId { get; set; }

    /// <summary>
    /// Status of the invitation
    /// </summary>
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    /// <summary>
    /// When the invitation was accepted (if accepted)
    /// </summary>
    public DateTime? AcceptedAt { get; set; }

    /// <summary>
    /// The user who accepted (if accepted)
    /// </summary>
    public Guid? AcceptedByUserId { get; set; }
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Expired = 2,
    Revoked = 3
}
