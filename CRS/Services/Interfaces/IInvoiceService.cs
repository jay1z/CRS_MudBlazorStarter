using Horizon.Models;

namespace Horizon.Services.Interfaces;

/// <summary>
/// Service interface for managing invoices for reserve studies.
/// </summary>
public interface IInvoiceService
{
    /// <summary>
    /// Gets all invoices for a reserve study.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetByReserveStudyAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets an invoice by its ID.
    /// </summary>
    Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets invoices by status across all studies for the current tenant.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetByStatusAsync(InvoiceStatus status, CancellationToken ct = default);

    /// <summary>
    /// Gets overdue invoices for the current tenant.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetOverdueAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets unpaid invoices (Draft, Finalized, Sent, Viewed, PartiallyPaid, Overdue) for the current tenant.
    /// </summary>
    Task<IReadOnlyList<Invoice>> GetUnpaidAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new invoice for a reserve study.
    /// </summary>
    Task<Invoice> CreateAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Creates an invoice pre-populated from the current proposal.
    /// </summary>
    Task<Invoice> CreateFromProposalAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Creates an invoice for a specific payment milestone from the proposal.
    /// </summary>
    /// <param name="reserveStudyId">The reserve study ID</param>
    /// <param name="milestoneType">The milestone type to invoice for</param>
    /// <param name="applyPrepaymentDiscount">Whether to apply the prepayment discount</param>
    /// <param name="ct">Cancellation token</param>
    Task<Invoice> CreateFromProposalMilestoneAsync(
        Guid reserveStudyId, 
        InvoiceMilestoneType milestoneType, 
        bool applyPrepaymentDiscount = false,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the payment schedule milestones for a reserve study, including which have been invoiced.
    /// </summary>
    Task<List<PaymentMilestone>> GetPaymentMilestonesAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets the total amount already invoiced for a reserve study (excluding voided invoices).
    /// </summary>
    Task<decimal> GetInvoicedTotalAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Gets the total amount paid for a reserve study.
    /// </summary>
    Task<decimal> GetPaidTotalAsync(Guid reserveStudyId, CancellationToken ct = default);

    /// <summary>
    /// Updates an invoice (only if in Draft status).
    /// </summary>
    Task<Invoice> UpdateAsync(Invoice invoice, CancellationToken ct = default);

    /// <summary>
    /// Adds a line item to an invoice.
    /// </summary>
    Task<InvoiceLineItem> AddLineItemAsync(Guid invoiceId, InvoiceLineItem lineItem, CancellationToken ct = default);

    /// <summary>
    /// Updates a line item on an invoice.
    /// </summary>
    Task<InvoiceLineItem> UpdateLineItemAsync(InvoiceLineItem lineItem, CancellationToken ct = default);

    /// <summary>
    /// Removes a line item from an invoice.
    /// </summary>
    Task RemoveLineItemAsync(Guid lineItemId, CancellationToken ct = default);

    /// <summary>
    /// Finalizes an invoice (transitions from Draft to Finalized).
    /// </summary>
    Task<Invoice> FinalizeAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Marks an invoice as sent to the client.
    /// </summary>
    Task<Invoice> MarkSentAsync(Guid invoiceId, Guid sentByUserId, CancellationToken ct = default);

    /// <summary>
    /// Marks an invoice as viewed by the client.
    /// </summary>
    Task<Invoice> MarkViewedAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Records a payment on an invoice.
    /// </summary>
    Task<Invoice> RecordPaymentAsync(Guid invoiceId, decimal amount, string? paymentMethod = null, string? paymentReference = null, CancellationToken ct = default);

    /// <summary>
    /// Voids an invoice.
    /// </summary>
    Task<Invoice> VoidAsync(Guid invoiceId, string? reason = null, CancellationToken ct = default);

    /// <summary>
    /// Generates the next invoice number for the tenant.
    /// </summary>
    Task<string> GenerateInvoiceNumberAsync(CancellationToken ct = default);

        /// <summary>
        /// Checks and updates overdue status for invoices past their due date.
        /// </summary>
        Task UpdateOverdueStatusesAsync(CancellationToken ct = default);

        /// <summary>
        /// Calculates and applies late payment interest for overdue invoices.
        /// </summary>
        Task<int> CalculateLateInterestAsync(CancellationToken ct = default);

        /// <summary>
        /// Calculates late interest for a specific invoice.
        /// </summary>
        Task<decimal> CalculateLateInterestForInvoiceAsync(Guid invoiceId, CancellationToken ct = default);

        /// <summary>
        /// Applies early payment discount if payment is within discount period.
        /// </summary>
        Task<Invoice> ApplyEarlyPaymentDiscountAsync(Guid invoiceId, CancellationToken ct = default);

                /// <summary>
                /// Soft deletes an invoice (only if in Draft status).
                            /// </summary>
                                Task DeleteAsync(Guid invoiceId, CancellationToken ct = default);

                            /// <summary>
                            /// Creates a duplicate of an existing invoice as a new draft.
                                                        /// </summary>
                                                        /// <param name="invoiceId">The invoice to duplicate</param>
                                                        /// <param name="ct">Cancellation token</param>
                                                        /// <returns>The new draft invoice</returns>
                                                        Task<Invoice> DuplicateAsync(Guid invoiceId, CancellationToken ct = default);

                                /// <summary>
                                /// Records that a reminder was sent for the invoice.
                                /// </summary>
                                Task<Invoice> RecordReminderSentAsync(Guid invoiceId, CancellationToken ct = default);

                                                                /// <summary>
                                                                /// Gets all payment records for an invoice.
                                                                /// </summary>
                                                                Task<List<PaymentRecord>> GetPaymentRecordsAsync(Guid invoiceId, CancellationToken ct = default);

                                                                /// <summary>
                                                                /// Generates an access token for public invoice viewing.
                                                                /// </summary>
                                                                Task<string> GenerateAccessTokenAsync(Guid invoiceId, CancellationToken ct = default);

                                                                /// <summary>
                                                                /// Gets an invoice by its public access token.
                                                                /// </summary>
                                                                Task<Invoice?> GetByAccessTokenAsync(string accessToken, CancellationToken ct = default);

                                                                        /// <summary>
                                                                        /// Gets or creates the tenant invoice settings.
                                                                        /// </summary>
                                                                        Task<TenantInvoiceSettings> GetOrCreateInvoiceSettingsAsync(CancellationToken ct = default);

                                                                        /// <summary>
                                                                        /// Updates the tenant invoice settings.
                                                                        /// </summary>
                                                                        Task<TenantInvoiceSettings> UpdateInvoiceSettingsAsync(TenantInvoiceSettings settings, CancellationToken ct = default);

                                                                        /// <summary>
                                                                        /// Generates a preview of what the next invoice number will look like.
                                                                        /// </summary>
                                                                        Task<string> PreviewInvoiceNumberAsync(TenantInvoiceSettings settings, CancellationToken ct = default);

                                                                                /// <summary>
                                                                                /// Creates the next milestone invoice based on a paid invoice.
                                                                                /// </summary>
                                                                                Task<Invoice?> CreateNextMilestoneInvoiceAsync(Guid paidInvoiceId, InvoiceMilestoneType nextMilestone, CancellationToken ct = default);

                                            /// <summary>
                                            /// Sends an invoice to the client via email, marks it as sent, and generates access token.
                                            /// </summary>
                                            /// <param name="invoiceId">The invoice to send</param>
                                            /// <param name="sentByUserId">The user sending the invoice</param>
                                            /// <param name="baseUrl">The base URL for the invoice link</param>
                                            /// <param name="ct">Cancellation token</param>
                                            /// <returns>The updated invoice</returns>
                                            Task<Invoice> SendInvoiceAsync(Guid invoiceId, Guid sentByUserId, string baseUrl, CancellationToken ct = default);
                                        }
