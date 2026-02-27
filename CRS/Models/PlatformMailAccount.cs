using System.ComponentModel.DataAnnotations;

namespace CRS.Models;

/// <summary>
/// Stores IMAP/SMTP credentials for platform-level email accounts.
/// Platform admins can view inboxes and send email from these accounts.
/// Not tenant-scoped — these belong to the platform operator.
/// </summary>
public class PlatformMailAccount
{
    [Key]
    public int Id { get; set; }

    /// <summary>Display name for this mailbox (e.g., "Support", "Admin").</summary>
    [Required, MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Full email address (e.g., support@alxreservecloud.com).</summary>
    [Required, MaxLength(256)]
    public string EmailAddress { get; set; } = string.Empty;

    // ── IMAP (incoming) ──────────────────────────────────────────
    [Required, MaxLength(256)]
    public string ImapHost { get; set; } = "mail.alxreservecloud.com";

    public int ImapPort { get; set; } = 993;

    public bool ImapUseSsl { get; set; } = true;

    // ── SMTP (outgoing) ──────────────────────────────────────────
    [Required, MaxLength(256)]
    public string SmtpHost { get; set; } = "mail.alxreservecloud.com";

    public int SmtpPort { get; set; } = 465;

    public bool SmtpUseSsl { get; set; } = true;

    // ── Credentials (shared for IMAP & SMTP unless overridden) ──
    [Required, MaxLength(256)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Stored encrypted in production. For now, plain text in dev.
    /// TODO: Encrypt with Data Protection API before persisting.
    /// </summary>
    [Required, MaxLength(512)]
    public string Password { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
