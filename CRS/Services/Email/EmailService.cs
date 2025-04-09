using CRS.Models;
using CRS.Models.Emails;

using MailKit.Security;

using MimeKit;

namespace CRS.Services.Email {
    public class EmailService : IEmailService {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IRazorComponentRenderer _componentRenderer;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IRazorComponentRenderer componentRenderer) {
            _configuration = configuration;
            _logger = logger;
            _componentRenderer = componentRenderer;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage) {
            await SendEmailAsync(to, subject, htmlMessage, new List<EmailAttachment>());
        }

        public async Task SendEmailAsync(string to, string subject, string htmlMessage, List<EmailAttachment> attachments) {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(emailSettings["SenderName"], emailSettings["SenderEmail"]));

            to = "emailme@jasonzurowski.com";
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var builder = new BodyBuilder { HtmlBody = htmlMessage };

            // Add attachments if any
            foreach (var attachment in attachments) {
                builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            message.Body = builder.ToMessageBody();

            try {
                using var client = new MailKit.Net.Smtp.SmtpClient();
                _logger.LogInformation($"Sending email to {to}");

                await client.ConnectAsync(emailSettings["SmtpServer"], int.Parse(emailSettings["SmtpPort"]), SecureSocketOptions.Auto);
                await client.AuthenticateAsync(emailSettings["SmtpUsername"], emailSettings["SmtpPassword"]);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex) {
                _logger.LogError($"Failed to send email to {to}: {ex.Message}");
                throw;
            }
        }

        public async Task SendReserveStudyEmailAsync(ReserveStudy reserveStudy, string to, string subject, string additionalMessage = "") {
            var model = new ReserveStudyEmail {
                ReserveStudy = reserveStudy,
                AdditionalMessage = additionalMessage,
                BaseUrl = _configuration["BaseUrl"]
            };

            var parameters = new Dictionary<string, object> { { "Model", model } };
            var htmlContent = await _componentRenderer.RenderComponentAsync<Components.EmailTemplates.ReserveStudyEmailTemplate>(parameters);

            await SendEmailAsync(to, subject, htmlContent);
        }

        public async Task SendWelcomeEmailAsync(string to, string subject, string userName) {
            var model = new WelcomeEmail { UserName = userName };

            var parameters = new Dictionary<string, object> { { "Model", model } };
            var htmlContent = await _componentRenderer.RenderComponentAsync<Components.EmailTemplates.WelcomeEmailTemplate>(parameters);

            await SendEmailAsync(to, subject, htmlContent);
        }

        public async Task SendReserveStudyToContactsAsync(ReserveStudy reserveStudy, string subject, string additionalMessage = "") {
            var emailRecipients = new List<string>();
            emailRecipients.Add("emailme@jasonzurowski.com");
            // Add contact emails if available
            if (reserveStudy.Contact?.Email != null) {
                //emailRecipients.Add(reserveStudy.Contact.Email);
            }

            // Add property manager email if available
            if (reserveStudy.PropertyManager?.Email != null) {
                //emailRecipients.Add(reserveStudy.PropertyManager.Email);
            }

            // Add specialist email if available
            if (reserveStudy.Specialist?.Email != null) {
                //emailRecipients.Add(reserveStudy.Specialist.Email);
            }

            // Send emails to each recipient
            foreach (var recipient in emailRecipients.Distinct()) {
                await SendReserveStudyEmailAsync(reserveStudy, recipient, subject, additionalMessage);
            }
        }
    }
}
