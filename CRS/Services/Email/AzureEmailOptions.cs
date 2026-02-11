namespace CRS.Services.Email;

/// <summary>
/// Configuration options for Azure Communication Services Email.
/// </summary>
public class AzureEmailOptions
{
    public const string SectionName = "AzureEmail";

    /// <summary>
    /// The connection string for Azure Communication Services.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// The default sender email address (e.g., DoNotReply@xxx.azurecomm.net or no-reply@yourdomain.com).
    /// </summary>
    public string DefaultSenderAddress { get; set; } = string.Empty;

    /// <summary>
    /// The default sender display name.
    /// </summary>
    public string DefaultSenderName { get; set; } = "ALX Reserve Cloud";

    /// <summary>
    /// Whether to use Azure Communication Services for email (false = use Coravel SMTP).
    /// </summary>
    public bool Enabled { get; set; } = true;
}
