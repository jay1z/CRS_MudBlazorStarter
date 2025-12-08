using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;

namespace CRS.Services.Email;

/// <summary>
/// Decorator for IMailer that logs all sent emails to the EmailLog table.
/// </summary>
public class LoggingMailer : IMailer
{
    private readonly IMailer _innerMailer;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LoggingMailer> _logger;

    public LoggingMailer(
        IMailer innerMailer,
        IServiceProvider serviceProvider,
        ILogger<LoggingMailer> logger)
    {
        _innerMailer = innerMailer;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SendAsync<T>(Mailable<T> mailable)
    {
        // Extract email details before sending
        var emailInfo = ExtractEmailInfo(mailable);
        
        // Create scope for scoped services
        using var scope = _serviceProvider.CreateScope();
        var emailLogService = scope.ServiceProvider.GetRequiredService<IEmailLogService>();
        var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();

        // Log the email before sending
        var emailLog = new EmailLog
        {
            TenantId = tenantContext?.TenantId ?? 0,
            ToEmail = emailInfo.ToEmail,
            Subject = emailInfo.Subject,
            TemplateType = emailInfo.TemplateType,
            Status = EmailStatus.Sending,
            SentAt = DateTime.UtcNow
        };

        try
        {
            await emailLogService.LogEmailAsync(emailLog);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create email log entry before sending");
        }

        try
        {
            // Send the actual email
            await _innerMailer.SendAsync(mailable);

            // Update status to sent
            try
            {
                await emailLogService.UpdateStatusAsync(emailLog.Id, EmailStatus.Sent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update email log status to Sent");
            }

            _logger.LogInformation("Email sent successfully to {To}: {Subject}", emailInfo.ToEmail, emailInfo.Subject);
        }
        catch (Exception ex)
        {
            // Update status to failed
            try
            {
                await emailLogService.UpdateStatusAsync(emailLog.Id, EmailStatus.Failed, ex.Message);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to update email log status to Failed");
            }

            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", emailInfo.ToEmail, emailInfo.Subject);
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
        // Create scope for scoped services
        using var scope = _serviceProvider.CreateScope();
        var emailLogService = scope.ServiceProvider.GetRequiredService<IEmailLogService>();
        var tenantContext = scope.ServiceProvider.GetService<ITenantContext>();

        var toEmail = to.FirstOrDefault()?.Email ?? "";

        // Log the email before sending
        var emailLog = new EmailLog
        {
            TenantId = tenantContext?.TenantId ?? 0,
            ToEmail = toEmail,
            Subject = subject,
            TemplateType = "DirectSend",
            CcEmails = cc != null ? string.Join(";", cc.Select(r => r.Email)) : null,
            BccEmails = bcc != null ? string.Join(";", bcc.Select(r => r.Email)) : null,
            Status = EmailStatus.Sending,
            SentAt = DateTime.UtcNow
        };

        try
        {
            await emailLogService.LogEmailAsync(emailLog);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create email log entry before sending");
        }

        try
        {
            // Send the actual email
            await _innerMailer.SendAsync(message, subject, to, from, replyTo, cc, bcc, attachments, sender);

            // Update status to sent
            try
            {
                await emailLogService.UpdateStatusAsync(emailLog.Id, EmailStatus.Sent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update email log status to Sent");
            }

            _logger.LogInformation("Email sent successfully to {To}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            // Update status to failed
            try
            {
                await emailLogService.UpdateStatusAsync(emailLog.Id, EmailStatus.Failed, ex.Message);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Failed to update email log status to Failed");
            }

            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", toEmail, subject);
            throw;
        }
    }

    public async Task<string> RenderAsync<T>(Mailable<T> mailable)
    {
        return await _innerMailer.RenderAsync(mailable);
    }

    private static EmailInfo ExtractEmailInfo<T>(Mailable<T> mailable)
    {
        var info = new EmailInfo
        {
            TemplateType = mailable.GetType().Name
        };

        // Use reflection to get protected/private fields from Mailable
        var mailableType = typeof(Mailable<T>);
        
        // Try to get To addresses
        var toField = mailableType.GetField("_to", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (toField?.GetValue(mailable) is IEnumerable<MailRecipient> toList)
        {
            var first = toList.FirstOrDefault();
            if (first != null)
            {
                info.ToEmail = first.Email;
            }
        }

        // Try to get Subject
        var subjectField = mailableType.GetField("_subject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (subjectField?.GetValue(mailable) is string subject)
        {
            info.Subject = subject;
        }

        // Handle BasicMailable specifically
        if (mailable is BasicMailable basicMailable)
        {
            info.Subject = basicMailable.SubjectText;
            info.ToEmail = basicMailable.ToAddresses.FirstOrDefault() ?? "";
            info.TemplateType = "BasicMailable";
        }

        return info;
    }

    private class EmailInfo
    {
        public string ToEmail { get; set; } = "";
        public string Subject { get; set; } = "";
        public string TemplateType { get; set; } = "";
    }
}

/// <summary>
/// Extension methods for registering the logging mailer.
/// </summary>
public static class LoggingMailerExtensions
{
    /// <summary>
    /// Wraps the existing IMailer with logging functionality.
    /// Call this AFTER AddMailer() in Program.cs.
    /// </summary>
    public static IServiceCollection AddEmailLogging(this IServiceCollection services)
    {
        // Find the existing IMailer registration
        var mailerDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMailer));
        if (mailerDescriptor == null)
        {
            throw new InvalidOperationException("IMailer is not registered. Call AddMailer() before AddEmailLogging().");
        }

        // Remove the existing registration
        services.Remove(mailerDescriptor);

        // Re-register with decoration
        if (mailerDescriptor.ImplementationFactory != null)
        {
            services.AddSingleton<IMailer>(sp =>
            {
                var innerMailer = (IMailer)mailerDescriptor.ImplementationFactory(sp);
                var logger = sp.GetRequiredService<ILogger<LoggingMailer>>();
                return new LoggingMailer(innerMailer, sp, logger);
            });
        }
        else if (mailerDescriptor.ImplementationInstance != null)
        {
            services.AddSingleton<IMailer>(sp =>
            {
                var innerMailer = (IMailer)mailerDescriptor.ImplementationInstance;
                var logger = sp.GetRequiredService<ILogger<LoggingMailer>>();
                return new LoggingMailer(innerMailer, sp, logger);
            });
        }
        else if (mailerDescriptor.ImplementationType != null)
        {
            // Store the implementation type
            var implType = mailerDescriptor.ImplementationType;
            
            services.AddSingleton<IMailer>(sp =>
            {
                var innerMailer = (IMailer)ActivatorUtilities.CreateInstance(sp, implType);
                var logger = sp.GetRequiredService<ILogger<LoggingMailer>>();
                return new LoggingMailer(innerMailer, sp, logger);
            });
        }

        return services;
    }
}
