using Coravel.Mailer.Mail.Interfaces;
using Horizon.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Horizon.Services.Email;

public class IdentityEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IMailer _mailer;
    private readonly ILogger<IdentityEmailSender> _logger;

    public IdentityEmailSender(IMailer mailer, ILogger<IdentityEmailSender> logger)
    {
        _mailer = mailer;
        _logger = logger;
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var html = EmailTemplateBuilder.CreateConfirmationEmail(confirmationLink);
        await SendAsync(email, "Confirm Your Email - ALX Reserve Cloud", html);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var html = EmailTemplateBuilder.CreatePasswordResetEmail(resetLink);
        await SendAsync(email, "Reset Your Password - ALX Reserve Cloud", html);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var html = EmailTemplateBuilder.CreatePasswordResetCodeEmail(resetCode);
        await SendAsync(email, "Your Password Reset Code - ALX Reserve Cloud", html);
    }

    private async Task SendAsync(string to, string subject, string html)
    {
        try
        {
            var mail = new BasicMailable
            {
                SubjectText = subject,
                HtmlBody = html,
                TextBody = StripTags(html),
                ToAddresses = new[] { to }
            };

            await _mailer.SendAsync(mail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed sending email {Subject} to {To}", subject, to);
            throw;
        }
    }

    private static string StripTags(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var array = new char[input.Length];
        var arrayIndex = 0;
        var inside = false;

        foreach (var @let in input)
        {
            if (@let == '<') { inside = true; continue; }
            if (@let == '>') { inside = false; continue; }
            if (!inside) { array[arrayIndex] = @let; arrayIndex++; }
        }
        return new string(array, 0, arrayIndex);
    }
}
