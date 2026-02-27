using System.ComponentModel.DataAnnotations;

namespace Horizon.Models;

/// <summary>
/// Stores contact form submissions from the public /contact page.
/// Not tenant-scoped — these are platform-level inquiries.
/// </summary>
public class ContactSubmission : BaseModel
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the email notification was sent successfully.
    /// </summary>
    public bool EmailSent { get; set; }

    /// <summary>
    /// Whether an admin has read/acknowledged this submission.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Optional admin notes or reply reference.
    /// </summary>
    public string? AdminNotes { get; set; }
}
