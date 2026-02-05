using System.ComponentModel.DataAnnotations;
using CRS.Data;

namespace CRS.Models;

/// <summary>
/// Join entity for many-to-many relationship between CustomerAccount and ApplicationUser.
/// Allows multiple users to belong to one customer account (e.g., multiple HOA board members).
/// </summary>
public class CustomerAccountUser
{
    /// <summary>
    /// The customer account
    /// </summary>
    public Guid CustomerAccountId { get; set; }
    public CustomerAccount CustomerAccount { get; set; } = null!;

    /// <summary>
    /// The user linked to this customer account
    /// </summary>
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Role of the user within the customer account
    /// </summary>
    public CustomerAccountRole Role { get; set; } = CustomerAccountRole.Member;

    /// <summary>
    /// When the user was added to the account
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who invited this user (null if original owner)
    /// </summary>
    public Guid? InvitedByUserId { get; set; }

    /// <summary>
    /// Is this user currently active on the account
    /// </summary>
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Role of a user within a customer account
/// </summary>
public enum CustomerAccountRole
{
    /// <summary>
    /// Owner has full control - can manage team, billing, delete account
    /// </summary>
    Owner = 0,

    /// <summary>
    /// Admin can manage team members and settings
    /// </summary>
    Admin = 1,

    /// <summary>
    /// Member can view and request studies, but cannot manage team
    /// </summary>
    Member = 2,

    /// <summary>
    /// Viewer has read-only access to studies and reports
    /// </summary>
    Viewer = 3
}
