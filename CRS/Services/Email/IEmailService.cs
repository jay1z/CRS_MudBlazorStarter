using CRS.Models;

namespace CRS.Services.Email {
    public interface IEmailService {
        Task SendEmailAsync(string to, string subject, string htmlMessage);
        Task SendEmailAsync(string to, string subject, string htmlMessage, List<EmailAttachment> attachments);
        Task SendReserveStudyEmailAsync(ReserveStudy reserveStudy, string to, string subject, string additionalMessage = "");
        Task SendReserveStudyToContactsAsync(ReserveStudy reserveStudy, string subject, string additionalMessage = "");
    }

    public class EmailAttachment {
        public string FileName { get; set; }
        public byte[] Content { get; set; }
        public string ContentType { get; set; }
    }
}
