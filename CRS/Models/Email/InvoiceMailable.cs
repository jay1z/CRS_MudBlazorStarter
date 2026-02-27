using Coravel.Mailer.Mail;

using Horizon.Models.Emails;

namespace Horizon.Models.Email;

/// <summary>
/// Mailable for sending invoice notifications to clients.
/// </summary>
public class InvoiceMailable : Mailable<InvoiceEmail>
{
    private readonly InvoiceEmail _invoiceEmail;
    private readonly string _recipientEmail;

    public InvoiceMailable(InvoiceEmail invoiceEmail, string recipientEmail)
    {
        _invoiceEmail = invoiceEmail;
        _recipientEmail = recipientEmail;
    }

    public override void Build()
    {
        var invoice = _invoiceEmail.Invoice;
        var communityName = _invoiceEmail.ReserveStudy?.Community?.Name ?? invoice.BillToName ?? "Client";

        var subject = _invoiceEmail.IsReminder
            ? $"Payment Reminder: Invoice #{invoice.InvoiceNumber} - {communityName}"
            : $"Invoice #{invoice.InvoiceNumber} - {communityName}";

        var fromEmail = _invoiceEmail.TenantInfo?.FromEmail ?? "no-reply@reservecloud.com";

        this.To(_recipientEmail)
            .From(fromEmail)
            .Subject(subject)
            .View("~/Components/EmailTemplates/InvoiceSent.cshtml", _invoiceEmail);
    }
}
