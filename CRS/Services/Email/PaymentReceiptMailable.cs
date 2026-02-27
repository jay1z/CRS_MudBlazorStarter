using Coravel.Mailer.Mail;
using Horizon.Models.Emails;

namespace Horizon.Services.Email;

/// <summary>
/// Mailable for sending payment receipt emails after successful invoice payments.
/// </summary>
public class PaymentReceiptMailable : Mailable<PaymentReceiptEmail>
{
    private readonly PaymentReceiptEmail _model;

    public PaymentReceiptMailable(PaymentReceiptEmail model)
    {
        _model = model;
    }

    public override void Build()
    {
        var subject = _model.IsFullyPaid
            ? $"Payment Receipt - Invoice #{_model.InvoiceNumber} Paid in Full"
            : $"Payment Receipt - {_model.AmountPaid:C} received for Invoice #{_model.InvoiceNumber}";

        To(_model.RecipientEmail)
            .Subject(subject)
            .View("~/Components/EmailTemplates/Invoices/PaymentReceipt.cshtml", _model);
    }
}
