using CRS.Data;
using CRS.Models;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace CRS.Services.Mail;

public class MailboxService : IMailboxService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<MailboxService> _logger;

    public MailboxService(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<MailboxService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<List<MailMessageSummary>> GetMessagesAsync(int accountId, string folder = "INBOX", int page = 0, int pageSize = 50, CancellationToken ct = default)
    {
        var account = await GetAccountAsync(accountId, ct);
        using var client = await ConnectImapAsync(account, ct);

        var mailFolder = await OpenFolderAsync(client, folder, FolderAccess.ReadOnly, ct);
        var count = mailFolder.Count;
        if (count == 0) return [];

        // Fetch newest first
        var start = Math.Max(0, count - ((page + 1) * pageSize));
        var end = Math.Max(0, count - (page * pageSize) - 1);
        if (start > end) return [];

        var summaries = await mailFolder.FetchAsync(start, end,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags | MessageSummaryItems.BodyStructure, ct);

        var result = new List<MailMessageSummary>(summaries.Count);
        foreach (var s in summaries.OrderByDescending(x => x.Date))
        {
            result.Add(new MailMessageSummary(
                s.UniqueId.ToString(),
                s.Envelope?.Subject ?? "(no subject)",
                s.Envelope?.From?.ToString() ?? "",
                s.Envelope?.To?.Select(a => a.ToString()).ToArray() ?? [],
                s.Date,
                s.Flags?.HasFlag(MessageFlags.Seen) ?? false,
                s.Body is BodyPartMultipart mp && HasAttachments(mp)));
        }

        await client.DisconnectAsync(true, ct);
        return result;
    }

    public async Task<MailMessageDetail?> GetMessageAsync(int accountId, string uniqueId, string folder = "INBOX", CancellationToken ct = default)
    {
        var account = await GetAccountAsync(accountId, ct);
        using var client = await ConnectImapAsync(account, ct);

        var mailFolder = await OpenFolderAsync(client, folder, FolderAccess.ReadWrite, ct);

        if (!UniqueId.TryParse(uniqueId, out var uid)) return null;

        var message = await mailFolder.GetMessageAsync(uid, ct);
        if (message == null) return null;

        // Mark as seen
        await mailFolder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);

        var attachments = new List<MailAttachmentInfo>();
        foreach (var attachment in message.Attachments)
        {
            var fileName = attachment.ContentDisposition?.FileName ?? attachment.ContentType.Name ?? "attachment";
            attachments.Add(new MailAttachmentInfo(fileName, attachment.ContentType.MimeType, 0));
        }

        var detail = new MailMessageDetail(
            uniqueId,
            message.Subject ?? "(no subject)",
            message.From?.ToString() ?? "",
            message.To?.Select(a => a.ToString()).ToArray() ?? [],
            message.Cc?.Select(a => a.ToString()).ToArray() ?? [],
            message.Date,
            message.HtmlBody,
            message.TextBody,
            true,
            attachments);

        await client.DisconnectAsync(true, ct);
        return detail;
    }

    public async Task<List<string>> GetFoldersAsync(int accountId, CancellationToken ct = default)
    {
        var account = await GetAccountAsync(accountId, ct);
        using var client = await ConnectImapAsync(account, ct);

        var personal = client.GetFolder(client.PersonalNamespaces[0]);
        var folders = await personal.GetSubfoldersAsync(false, ct);

        var result = new List<string> { "INBOX" };
        foreach (var f in folders)
        {
            if (!f.Name.Equals("INBOX", StringComparison.OrdinalIgnoreCase))
                result.Add(f.FullName);
        }

        await client.DisconnectAsync(true, ct);
        return result;
    }

    public async Task SendAsync(int accountId, ComposeMailRequest request, CancellationToken ct = default)
    {
        var account = await GetAccountAsync(accountId, ct);

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(account.EmailAddress));
        foreach (var to in request.To)
            message.To.Add(MailboxAddress.Parse(to));
        foreach (var cc in request.Cc)
            message.Cc.Add(MailboxAddress.Parse(cc));
        message.Subject = request.Subject;

        var builder = new BodyBuilder { HtmlBody = request.HtmlBody };
        message.Body = builder.ToMessageBody();

        using var smtp = new MailKit.Net.Smtp.SmtpClient();
        // Accept certificate hostname mismatches (shared hosting issues server cert for its own hostname)
        smtp.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        var secureSocketOptions = account.SmtpUseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await smtp.ConnectAsync(account.SmtpHost, account.SmtpPort, secureSocketOptions, ct);
        await smtp.AuthenticateAsync(account.Username, account.Password, ct);
        await smtp.SendAsync(message, ct);
        await smtp.DisconnectAsync(true, ct);

        _logger.LogInformation("Sent email from {From} to {To}, subject: {Subject}", account.EmailAddress, string.Join(", ", request.To), request.Subject);
    }

    public async Task DeleteMessageAsync(int accountId, string uniqueId, string folder = "INBOX", CancellationToken ct = default)
    {
        var account = await GetAccountAsync(accountId, ct);
        using var client = await ConnectImapAsync(account, ct);

        var mailFolder = await OpenFolderAsync(client, folder, FolderAccess.ReadWrite, ct);

        if (!UniqueId.TryParse(uniqueId, out var uid)) return;

        await mailFolder.AddFlagsAsync(uid, MessageFlags.Deleted, true, ct);
        await mailFolder.ExpungeAsync(ct);
        await client.DisconnectAsync(true, ct);
    }

    public async Task SetReadAsync(int accountId, string uniqueId, bool isRead, string folder = "INBOX", CancellationToken ct = default)
    {
        var account = await GetAccountAsync(accountId, ct);
        using var client = await ConnectImapAsync(account, ct);

        var mailFolder = await OpenFolderAsync(client, folder, FolderAccess.ReadWrite, ct);

        if (!UniqueId.TryParse(uniqueId, out var uid)) return;

        if (isRead)
            await mailFolder.AddFlagsAsync(uid, MessageFlags.Seen, true, ct);
        else
            await mailFolder.RemoveFlagsAsync(uid, MessageFlags.Seen, true, ct);

        await client.DisconnectAsync(true, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private async Task<PlatformMailAccount> GetAccountAsync(int accountId, CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var account = await db.PlatformMailAccounts.AsNoTracking().FirstOrDefaultAsync(a => a.Id == accountId && a.IsActive, ct);
        return account ?? throw new InvalidOperationException($"Mail account {accountId} not found or inactive.");
    }

    private static async Task<ImapClient> ConnectImapAsync(PlatformMailAccount account, CancellationToken ct)
    {
        var client = new ImapClient();
        // Accept certificate hostname mismatches (shared hosting issues server cert for its own hostname)
        client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        var secureSocketOptions = account.ImapUseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(account.ImapHost, account.ImapPort, secureSocketOptions, ct);
        await client.AuthenticateAsync(account.Username, account.Password, ct);
        return client;
    }

    private static async Task<IMailFolder> OpenFolderAsync(ImapClient client, string folderName, FolderAccess access, CancellationToken ct)
    {
        IMailFolder folder;
        if (folderName.Equals("INBOX", StringComparison.OrdinalIgnoreCase))
        {
            folder = client.Inbox;
        }
        else
        {
            folder = await client.GetFolderAsync(folderName, ct);
        }
        await folder.OpenAsync(access, ct);
        return folder;
    }

    private static bool HasAttachments(BodyPartMultipart mp)
    {
        foreach (var part in mp.BodyParts)
        {
            if (part is BodyPartBasic basic && basic.ContentDisposition?.Disposition?.Equals("attachment", StringComparison.OrdinalIgnoreCase) == true)
                return true;
            if (part is BodyPartMultipart sub && HasAttachments(sub))
                return true;
        }
        return false;
    }
}
