using Horizon.Models;

namespace Horizon.Services.Mail;

/// <summary>
/// DTOs for the platform webmail feature.
/// </summary>
public record MailMessageSummary(
    string UniqueId,
    string Subject,
    string From,
    string[] To,
    DateTimeOffset Date,
    bool IsRead,
    bool HasAttachments);

public record MailMessageDetail(
    string UniqueId,
    string Subject,
    string From,
    string[] To,
    string[] Cc,
    DateTimeOffset Date,
    string? HtmlBody,
    string? TextBody,
    bool IsRead,
    List<MailAttachmentInfo> Attachments);

public record MailAttachmentInfo(string FileName, string ContentType, int SizeBytes);

public record ComposeMailRequest(
    string From,
    string[] To,
    string[] Cc,
    string Subject,
    string HtmlBody);

/// <summary>
/// Platform webmail service: read via IMAP, send via SMTP.
/// </summary>
public interface IMailboxService
{
    /// <summary>List messages in a folder (defaults to INBOX).</summary>
    Task<List<MailMessageSummary>> GetMessagesAsync(int accountId, string folder = "INBOX", int page = 0, int pageSize = 50, CancellationToken ct = default);

    /// <summary>Get full message details by UID.</summary>
    Task<MailMessageDetail?> GetMessageAsync(int accountId, string uniqueId, string folder = "INBOX", CancellationToken ct = default);

    /// <summary>List available folders/mailboxes.</summary>
    Task<List<string>> GetFoldersAsync(int accountId, CancellationToken ct = default);

    /// <summary>Send an email via SMTP using the specified account.</summary>
    Task SendAsync(int accountId, ComposeMailRequest request, CancellationToken ct = default);

    /// <summary>Delete a message by UID.</summary>
    Task DeleteMessageAsync(int accountId, string uniqueId, string folder = "INBOX", CancellationToken ct = default);

    /// <summary>Mark a message as read/unread.</summary>
    Task SetReadAsync(int accountId, string uniqueId, bool isRead, string folder = "INBOX", CancellationToken ct = default);
}
