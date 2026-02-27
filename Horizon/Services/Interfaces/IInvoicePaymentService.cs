using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service for handling Stripe payment processing for invoices.
/// </summary>
public interface IInvoicePaymentService
{
    /// <summary>
    /// Creates a Stripe Checkout Session for an invoice and returns the payment URL.
    /// </summary>
    /// <param name="invoiceId">The invoice ID</param>
    /// <param name="successUrl">URL to redirect after successful payment</param>
    /// <param name="cancelUrl">URL to redirect if payment is cancelled</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>The Stripe checkout URL</returns>
    Task<string> CreateCheckoutSessionAsync(Guid invoiceId, string successUrl, string cancelUrl, CancellationToken ct = default);

    /// <summary>
    /// Handles a successful payment from Stripe webhook.
    /// </summary>
    /// <param name="checkoutSessionId">The Stripe Checkout Session ID</param>
    /// <param name="paymentIntentId">The Stripe Payment Intent ID</param>
    /// <param name="amountPaid">Amount paid in cents</param>
    /// <param name="ct">Cancellation token</param>
    Task HandlePaymentSucceededAsync(string checkoutSessionId, string? paymentIntentId, long amountPaid, CancellationToken ct = default);

    /// <summary>
    /// Gets a valid payment URL for an invoice, creating a new checkout session if needed.
    /// </summary>
    Task<string> GetOrCreatePaymentUrlAsync(Guid invoiceId, string baseUrl, CancellationToken ct = default);

    /// <summary>
    /// Checks if an invoice has a valid (non-expired) payment URL.
    /// </summary>
    bool HasValidPaymentUrl(Invoice invoice);
}
