namespace Horizon.Models.Emails;

/// <summary>
/// Model for team invitation email
/// </summary>
public class TeamInvitationEmail
{
    public required string CustomerName { get; set; }
    public required string InviterName { get; set; }
    public required string Email { get; set; }
    public required string Role { get; set; }
    public required string TenantName { get; set; }
    public required string AcceptUrl { get; set; }
    public required DateTime ExpiresAt { get; set; }
}
