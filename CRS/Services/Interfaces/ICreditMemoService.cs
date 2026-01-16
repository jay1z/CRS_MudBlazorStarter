using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service for managing credit memos (refunds and adjustments).
/// </summary>
public interface ICreditMemoService
{
    /// <summary>
    /// Gets a credit memo by ID.
    /// </summary>
    Task<CreditMemo?> GetByIdAsync(Guid creditMemoId, CancellationToken ct = default);

    /// <summary>
    /// Gets all credit memos for an invoice.
    /// </summary>
    Task<List<CreditMemo>> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Gets all credit memos for the current tenant.
    /// </summary>
    Task<List<CreditMemo>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Creates a new credit memo.
    /// </summary>
    Task<CreditMemo> CreateAsync(CreditMemo creditMemo, CancellationToken ct = default);

    /// <summary>
    /// Applies a credit memo to its invoice (reduces invoice balance).
    /// </summary>
    Task<CreditMemo> ApplyAsync(Guid creditMemoId, Guid appliedByUserId, CancellationToken ct = default);

    /// <summary>
    /// Voids a credit memo.
    /// </summary>
    Task<CreditMemo> VoidAsync(Guid creditMemoId, string? reason, CancellationToken ct = default);

    /// <summary>
    /// Processes a refund via Stripe for a credit memo.
    /// </summary>
    Task<CreditMemo> ProcessRefundAsync(Guid creditMemoId, CancellationToken ct = default);

    /// <summary>
    /// Generates the next credit memo number for the tenant.
    /// </summary>
    Task<string> GenerateCreditMemoNumberAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the total credits applied to an invoice.
    /// </summary>
    Task<decimal> GetTotalCreditsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default);
}
