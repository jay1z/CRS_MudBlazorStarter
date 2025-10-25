using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CRS.Services.Email;

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
        => await SendAsync(email, "Confirm your email", $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
        => await SendAsync(email, "Reset your password", $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
        => await SendAsync(email, "Reset your password", $"Please reset your password using the following code: {resetCode}");

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
