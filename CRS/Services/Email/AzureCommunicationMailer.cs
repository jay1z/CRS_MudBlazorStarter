using Azure;
using Azure.Communication.Email;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;
using Microsoft.Extensions.Options;

namespace CRS.Services.Email;

/// <summary>
/// IMailer implementation that sends emails via Azure Communication Services.
/// This replaces Coravel's SmtpMailer while keeping all existing Mailables working.
/// </summary>
public class AzureCommunicationMailer : IMailer
{
    private readonly EmailClient _emailClient;
    private readonly AzureEmailOptions _options;
    private readonly ILogger<AzureCommunicationMailer> _logger;
    private readonly IServiceProvider _serviceProvider;

    public AzureCommunicationMailer(
        IOptions<AzureEmailOptions> options,
        ILogger<AzureCommunicationMailer> logger,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _emailClient = new EmailClient(_options.ConnectionString);
    }

    public async Task SendAsync<T>(Mailable<T> mailable)
    {
        // Build the mailable to populate its properties
        mailable.Build();

        // Extract email details using reflection (Coravel keeps these private)
        var (to, cc, bcc, from, replyTo, subject, attachments, messageBody) = ExtractMailableDetails(mailable);

        // Get the rendered content - either from a view template or inline HTML/Text
        string? htmlContent = null;
        string? textContent = null;

        // First check for inline HTML/Text content (used by BasicMailable and Html()/Text() methods)
        if (messageBody != null)
        {
            htmlContent = messageBody.Html;
            textContent = messageBody.Text;
        }

        // If no inline content, try rendering a view template
        if (string.IsNullOrEmpty(htmlContent) && string.IsNullOrEmpty(textContent))
        {
            var renderedContent = await RenderAsync(mailable);
            if (!string.IsNullOrEmpty(renderedContent))
            {
                htmlContent = renderedContent;
            }
        }

        // Validate we have some content to send
        if (string.IsNullOrEmpty(htmlContent) && string.IsNullOrEmpty(textContent))
        {
            var mailableTypeName = mailable.GetType().Name;
            _logger.LogError("Cannot send email: No content found for mailable {MailableType}. " +
                "Ensure the mailable either uses a view template or sets Html()/Text() content.",
                mailableTypeName);
            throw new ArgumentException($"Email content must have either HTML or PlainText. Mailable type: {mailableTypeName}");
        }

        // Validate recipients - fail early with helpful error
        if (to == null || to.Count == 0 || to.All(r => string.IsNullOrWhiteSpace(r.Email)))
        {
            var mailableTypeName = mailable.GetType().Name;
            _logger.LogError("Cannot send email: No valid recipients found for mailable {MailableType}. " +
                "This may indicate a reflection issue extracting the _to field from Coravel's Mailable base class.",
                mailableTypeName);
            throw new ArgumentException($"ToRecipients cannot be empty. Mailable type: {mailableTypeName}");
        }

        // Filter out any empty/null recipients
        var validTo = to.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();

        // Determine sender - always use the configured DefaultSenderAddress for Azure ACS
        // This ensures we use a verified domain. Log if we override a different sender.
        var senderAddress = _options.DefaultSenderAddress;
        if (!string.IsNullOrEmpty(from?.Email) && from.Email != senderAddress)
        {
            // Extract domain from the from email
            var fromDomain = from.Email.Split('@').LastOrDefault();
            var defaultDomain = senderAddress.Split('@').LastOrDefault();

            // Only use the mailable's from address if it matches the configured ACS domain
            if (fromDomain?.Equals(defaultDomain, StringComparison.OrdinalIgnoreCase) == true)
            {
                senderAddress = from.Email;
            }
            else
            {
                _logger.LogDebug("Overriding sender from '{OriginalFrom}' to '{ConfiguredFrom}' (Azure ACS requires verified domain)",
                    from.Email, senderAddress);
            }
        }

        // Build recipients
        var toRecipients = validTo.Select(r => new EmailAddress(r.Email, r.Name)).ToList();
        var ccRecipients = cc?.Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .Select(r => new EmailAddress(r.Email, r.Name)).ToList();
        var bccRecipients = bcc?.Where(r => !string.IsNullOrWhiteSpace(r.Email))
            .Select(r => new EmailAddress(r.Email, r.Name)).ToList();

        var emailRecipients = new EmailRecipients(toRecipients, ccRecipients, bccRecipients);

        // Build content
        var emailContent = new EmailContent(subject ?? "No Subject")
        {
            Html = htmlContent,
            PlainText = textContent
        };

        // Create message
        var emailMessage = new EmailMessage(senderAddress, emailRecipients, emailContent);

        // Add reply-to if specified
        if (replyTo != null && !string.IsNullOrEmpty(replyTo.Email))
        {
            emailMessage.ReplyTo.Add(new EmailAddress(replyTo.Email, replyTo.Name));
        }

        // Add attachments if any
        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                emailMessage.Attachments.Add(new EmailAttachment(
                    attachment.Name,
                    GetContentType(attachment.Name),
                    BinaryData.FromBytes(attachment.Bytes)));
            }
        }

        _logger.LogDebug("Sending email via Azure Communication Services to {To}, subject: {Subject}", 
            string.Join(", ", toRecipients.Select(r => r.Address)), subject);

        try
        {
            // Send with WaitUntil.Started for faster response - email will be delivered asynchronously
            // This returns as soon as Azure accepts the email, rather than waiting for full delivery
            var operation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);

            _logger.LogInformation("Email queued successfully via ACS. OperationId: {OperationId}, Status: {Status}",
                operation.Id, operation.Value.Status);

            // Optionally poll for completion in background (fire-and-forget for non-critical emails)
            // For critical emails where you need delivery confirmation, you could await this
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await operation.WaitForCompletionAsync();
                    _logger.LogDebug("Email delivery completed. OperationId: {OperationId}, FinalStatus: {Status}",
                        operation.Id, result.Value.Status);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Email delivery status check failed for OperationId: {OperationId}", operation.Id);
                }
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services email send failed. Status: {Status}, ErrorCode: {ErrorCode}",
                ex.Status, ex.ErrorCode);
            throw;
        }
    }

    public async Task SendAsync(
        MessageBody message,
        string subject,
        IEnumerable<MailRecipient> to,
        MailRecipient? from = null,
        MailRecipient? replyTo = null,
        IEnumerable<MailRecipient>? cc = null,
        IEnumerable<MailRecipient>? bcc = null,
        IEnumerable<Attachment>? attachments = null,
        MailRecipient? sender = null)
    {
        var senderAddress = from?.Email ?? _options.DefaultSenderAddress;

        var toRecipients = to.Select(r => new EmailAddress(r.Email, r.Name)).ToList();
        var ccRecipients = cc?.Select(r => new EmailAddress(r.Email, r.Name)).ToList();
        var bccRecipients = bcc?.Select(r => new EmailAddress(r.Email, r.Name)).ToList();

        var emailRecipients = new EmailRecipients(toRecipients, ccRecipients, bccRecipients);

        var emailContent = new EmailContent(subject)
        {
            Html = message.Html,
            PlainText = message.Text
        };

        var emailMessage = new EmailMessage(senderAddress, emailRecipients, emailContent);

        if (replyTo != null && !string.IsNullOrEmpty(replyTo.Email))
        {
            emailMessage.ReplyTo.Add(new EmailAddress(replyTo.Email, replyTo.Name));
        }

        if (attachments != null)
        {
            foreach (var attachment in attachments)
            {
                emailMessage.Attachments.Add(new EmailAttachment(
                    attachment.Name,
                    GetContentType(attachment.Name),
                    BinaryData.FromBytes(attachment.Bytes)));
            }
        }

        _logger.LogDebug("Sending direct email via Azure Communication Services to {To}, subject: {Subject}",
            string.Join(", ", toRecipients.Select(r => r.Address)), subject);

        try
        {
            // Send with WaitUntil.Started for faster response
            var operation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);

            _logger.LogInformation("Direct email queued successfully via ACS. OperationId: {OperationId}, Status: {Status}",
                operation.Id, operation.Value.Status);

            // Poll for completion in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var result = await operation.WaitForCompletionAsync();
                    _logger.LogDebug("Direct email delivery completed. OperationId: {OperationId}, FinalStatus: {Status}",
                        operation.Id, result.Value.Status);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Direct email delivery status check failed for OperationId: {OperationId}", operation.Id);
                }
            });
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Communication Services direct email send failed. Status: {Status}, ErrorCode: {ErrorCode}",
                ex.Status, ex.ErrorCode);
            throw;
        }
    }

    public async Task<string> RenderAsync<T>(Mailable<T> mailable)
    {
        // Use Coravel's internal rendering by calling the mailable's render method via reflection
        // The mailable should already be built at this point
        var mailableType = mailable.GetType().BaseType;
        if (mailableType == null) return string.Empty;

        // Get the view path and model
        var viewField = mailableType.GetField("_viewPath", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var modelField = mailableType.GetField("_viewModel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var viewPath = viewField?.GetValue(mailable) as string;
        var model = modelField?.GetValue(mailable);

        if (string.IsNullOrEmpty(viewPath))
        {
            // This is expected for mailables that use Html()/Text() instead of a view template
            _logger.LogDebug("No view path found for mailable {Type} - will use inline content if available", mailable.GetType().Name);
            return string.Empty;
        }

        // Use the RazorViewToStringRenderer to render the view
        using var scope = _serviceProvider.CreateScope();
        var razorRenderer = scope.ServiceProvider.GetService<IRazorViewToStringRenderer>();

        if (razorRenderer != null && model != null)
        {
            return await razorRenderer.RenderViewToStringAsync(viewPath, model);
        }

        _logger.LogWarning("Could not render email template {ViewPath}", viewPath);
        return string.Empty;
    }

    private (
        List<MailRecipient> to,
        List<MailRecipient>? cc,
        List<MailRecipient>? bcc,
        MailRecipient? from,
        MailRecipient? replyTo,
        string? subject,
        List<Attachment>? attachments,
        MessageBody? messageBody
    ) ExtractMailableDetails<T>(Mailable<T> mailable)
    {
        // Search through the entire type hierarchy for the fields
        var bindingFlags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;

        // Extract recipients - Coravel uses different types depending on how mailable is created:
        // - Array (MailRecipient[]) when using custom Mailable class
        // - IEnumerable<MailRecipient> when using Mailable.AsInline<T>()
        var to = ExtractRecipients(mailable, "_to", bindingFlags);
        var cc = ExtractRecipients(mailable, "_cc", bindingFlags);
        var bcc = ExtractRecipients(mailable, "_bcc", bindingFlags);

        var from = GetFieldValueFromHierarchy<MailRecipient>(mailable, "_from", bindingFlags);
        var replyTo = GetFieldValueFromHierarchy<MailRecipient>(mailable, "_replyTo", bindingFlags);
        var subject = GetFieldValueFromHierarchy<string>(mailable, "_subject", bindingFlags);

        var attachmentsArray = GetFieldValueFromHierarchy<Attachment[]>(mailable, "_attachments", bindingFlags);
        var attachments = attachmentsArray?.ToList() 
            ?? GetFieldValueFromHierarchy<List<Attachment>>(mailable, "_attachments", bindingFlags);

        // Extract inline HTML/Text content (set by Html() and Text() methods)
        var messageBody = GetFieldValueFromHierarchy<MessageBody>(mailable, "_messageBody", bindingFlags);

        // Debug: Log if recipients are still empty after extraction
        if (to.Count == 0)
        {
            _logger.LogWarning("No recipients found via reflection for {MailableType}. Dumping available fields:",
                mailable.GetType().Name);

            var type = mailable.GetType();
            while (type != null)
            {
                var fields = type.GetFields(bindingFlags);
                foreach (var field in fields)
                {
                    var value = field.GetValue(mailable);
                    var valueType = value?.GetType().Name ?? "null";
                    _logger.LogWarning("  Field: {TypeName}.{FieldName} ({FieldType}) = {Value}",
                        type.Name, field.Name, valueType, value?.ToString() ?? "(null)");
                }
                type = type.BaseType;
            }
        }

        return (to, cc, bcc, from, replyTo, subject, attachments, messageBody);
    }

    /// <summary>
    /// Extracts recipients from a mailable field, handling various types Coravel uses.
    /// </summary>
    private List<MailRecipient> ExtractRecipients<T>(Mailable<T> mailable, string fieldName, System.Reflection.BindingFlags flags)
    {
        var type = mailable.GetType();
        var results = new List<MailRecipient>();

        // Walk up the type hierarchy checking ALL fields at each level
        // This is important because InlineMailable might shadow base class fields
        while (type != null)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                var value = field.GetValue(mailable);
                if (value != null)
                {
                    // Try different types Coravel might use
                    if (value is MailRecipient[] array && array.Length > 0)
                    {
                        results.AddRange(array);
                    }
                    else if (value is List<MailRecipient> list && list.Count > 0)
                    {
                        results.AddRange(list);
                    }
                    else if (value is IEnumerable<MailRecipient> enumerable)
                    {
                        var items = enumerable.ToList();
                        if (items.Count > 0) results.AddRange(items);
                    }
                    else if (value is System.Collections.IEnumerable nonGenericEnumerable)
                    {
                        // Try to enumerate if it implements IEnumerable (non-generic)
                        foreach (var item in nonGenericEnumerable)
                        {
                            if (item is MailRecipient recipient)
                            {
                                results.Add(recipient);
                            }
                        }
                    }
                    else
                    {
                        // Log unexpected type for debugging
                        _logger.LogWarning("Field {FieldName} at type {TypeName} has unexpected type {ValueType} with value {Value}", 
                            fieldName, type.Name, value.GetType().FullName, value);
                    }

                    // If we found recipients, we can stop searching
                    if (results.Count > 0) break;
                }
            }
            type = type.BaseType;
        }

        return results;
    }

    private static TField? GetFieldValueFromHierarchy<TField>(object obj, string fieldName, System.Reflection.BindingFlags flags)
    {
        var type = obj.GetType();

        // Walk up the type hierarchy to find the field
        while (type != null)
        {
            var field = type.GetField(fieldName, flags);
            if (field != null)
            {
                return field.GetValue(obj) is TField value ? value : default;
            }
            type = type.BaseType;
        }

        return default;
    }

    private static TField? GetFieldValue<TField>(object obj, Type type, string fieldName, System.Reflection.BindingFlags flags)
    {
        var field = type.GetField(fieldName, flags);
        return field?.GetValue(obj) is TField value ? value : default;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".csv" => "text/csv",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}

/// <summary>
/// Interface for rendering Razor views to string.
/// </summary>
public interface IRazorViewToStringRenderer
{
    Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
}
