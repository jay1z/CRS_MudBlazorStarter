using CRS.Data;
using CRS.Models;
using CRS.Services.Billing;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace CRS.Services;

/// <summary>
/// Service for managing credit memos (refunds and adjustments).
/// </summary>
public class CreditMemoService : ICreditMemoService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IStripeClientFactory _stripeClientFactory;
    private readonly ILogger<CreditMemoService> _logger;

    public CreditMemoService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        IStripeClientFactory stripeClientFactory,
        ILogger<CreditMemoService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _stripeClientFactory = stripeClientFactory;
        _logger = logger;
    }

    public async Task<CreditMemo?> GetByIdAsync(Guid creditMemoId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.CreditMemos
            .Include(cm => cm.Invoice)
            .Include(cm => cm.CreatedBy)
            .Include(cm => cm.AppliedBy)
            .FirstOrDefaultAsync(cm => cm.Id == creditMemoId && cm.DateDeleted == null, ct);
    }

    public async Task<List<CreditMemo>> GetByInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.CreditMemos
            .Include(cm => cm.CreatedBy)
            .Where(cm => cm.InvoiceId == invoiceId && cm.DateDeleted == null)
            .OrderByDescending(cm => cm.IssueDate)
            .ToListAsync(ct);
    }

    public async Task<List<CreditMemo>> GetAllAsync(CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);
        
        return await context.CreditMemos
            .Include(cm => cm.Invoice)
            .Include(cm => cm.CreatedBy)
            .Where(cm => cm.DateDeleted == null)
            .OrderByDescending(cm => cm.IssueDate)
            .ToListAsync(ct);
    }

    public async Task<CreditMemo> CreateAsync(CreditMemo creditMemo, CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        // Validate invoice exists
        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.Id == creditMemo.InvoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {creditMemo.InvoiceId} not found");

        // Generate credit memo number
        creditMemo.CreditMemoNumber = await GenerateCreditMemoNumberAsync(ct);
        creditMemo.TenantId = _tenantContext.TenantId.Value;
        creditMemo.Status = CreditMemoStatus.Draft;
        creditMemo.IssueDate = DateTime.UtcNow;
        
        // Copy bill-to info from invoice
        creditMemo.BillToName = invoice.BillToName;
        creditMemo.BillToEmail = invoice.BillToEmail;
        creditMemo.ReserveStudyId = invoice.ReserveStudyId;

        context.CreditMemos.Add(creditMemo);
        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created credit memo {CreditMemoNumber} for {Amount:C} on invoice {InvoiceNumber}",
            creditMemo.CreditMemoNumber, creditMemo.Amount, invoice.InvoiceNumber);

        return creditMemo;
    }

    public async Task<CreditMemo> ApplyAsync(Guid creditMemoId, Guid appliedByUserId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var creditMemo = await context.CreditMemos
            .Include(cm => cm.Invoice)
            .FirstOrDefaultAsync(cm => cm.Id == creditMemoId && cm.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Credit memo {creditMemoId} not found");

        if (creditMemo.Status != CreditMemoStatus.Draft)
            throw new InvalidOperationException($"Credit memo is already {creditMemo.Status}");

        var invoice = creditMemo.Invoice
            ?? throw new InvalidOperationException("Invoice not found for credit memo");

        // Apply the credit
        creditMemo.Status = CreditMemoStatus.Applied;
        creditMemo.AppliedAt = DateTime.UtcNow;
        creditMemo.AppliedByUserId = appliedByUserId;
        creditMemo.DateModified = DateTime.UtcNow;

        // Update invoice - credits reduce the effective balance
        // We don't directly reduce TotalAmount, but the NetBalanceDue computed property handles this
        invoice.DateModified = DateTime.UtcNow;

        // If the credit brings balance to zero or below, mark as paid
        var newBalance = invoice.TotalAmount - invoice.AmountPaid - creditMemo.Amount;
        if (newBalance <= 0 && invoice.Status != InvoiceStatus.Paid)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = DateTime.UtcNow;
            _logger.LogInformation("Invoice {InvoiceNumber} marked as paid after credit applied", invoice.InvoiceNumber);
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Applied credit memo {CreditMemoNumber} ({Amount:C}) to invoice {InvoiceNumber}",
            creditMemo.CreditMemoNumber, creditMemo.Amount, invoice.InvoiceNumber);

        return creditMemo;
    }

    public async Task<CreditMemo> VoidAsync(Guid creditMemoId, string? reason, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var creditMemo = await context.CreditMemos
            .Include(cm => cm.Invoice)
            .FirstOrDefaultAsync(cm => cm.Id == creditMemoId && cm.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Credit memo {creditMemoId} not found");

        if (creditMemo.Status == CreditMemoStatus.Voided)
            throw new InvalidOperationException("Credit memo is already voided");

        creditMemo.Status = CreditMemoStatus.Voided;
        creditMemo.VoidedAt = DateTime.UtcNow;
        creditMemo.VoidReason = reason;
        creditMemo.DateModified = DateTime.UtcNow;

        // If it was applied, we may need to adjust the invoice status
        if (creditMemo.Invoice != null && creditMemo.Invoice.Status == InvoiceStatus.Paid)
        {
            // Recalculate if invoice should still be paid
            var totalCredits = await context.CreditMemos
                .Where(cm => cm.InvoiceId == creditMemo.InvoiceId && 
                            cm.Id != creditMemoId &&
                            cm.Status == CreditMemoStatus.Applied && 
                            cm.DateDeleted == null)
                .SumAsync(cm => cm.Amount, ct);

            var balance = creditMemo.Invoice.TotalAmount - creditMemo.Invoice.AmountPaid - totalCredits;
            if (balance > 0)
            {
                creditMemo.Invoice.Status = creditMemo.Invoice.AmountPaid > 0 
                    ? InvoiceStatus.PartiallyPaid 
                    : InvoiceStatus.Sent;
                creditMemo.Invoice.PaidAt = null;
            }
        }

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Voided credit memo {CreditMemoNumber}: {Reason}",
            creditMemo.CreditMemoNumber, reason ?? "No reason provided");

        return creditMemo;
    }

    public async Task<CreditMemo> ProcessRefundAsync(Guid creditMemoId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var creditMemo = await context.CreditMemos
            .Include(cm => cm.Invoice)
            .FirstOrDefaultAsync(cm => cm.Id == creditMemoId && cm.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Credit memo {creditMemoId} not found");

        if (creditMemo.IsRefunded)
            throw new InvalidOperationException("Refund has already been processed");

        // Check if the invoice was paid via Stripe
        if (string.IsNullOrEmpty(creditMemo.Invoice?.StripePaymentIntentId))
        {
            throw new InvalidOperationException("Cannot process Stripe refund - no Stripe payment on invoice");
        }

        try
        {
            // Create Stripe refund
            var client = _stripeClientFactory.CreateClient();
            var refundService = new RefundService(client);

            // Convert amount to cents (Stripe uses smallest currency unit)
            var amountInCents = (long)(creditMemo.Amount * 100);

            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = creditMemo.Invoice.StripePaymentIntentId,
                Amount = amountInCents,
                Reason = creditMemo.Reason switch
                {
                    CreditMemoReason.Duplicate_Payment => "duplicate",
                    CreditMemoReason.Cancelled_Service => "requested_by_customer",
                    _ => "requested_by_customer"
                },
                Metadata = new Dictionary<string, string>
                {
                    ["credit_memo_id"] = creditMemo.Id.ToString(),
                    ["credit_memo_number"] = creditMemo.CreditMemoNumber ?? "",
                    ["invoice_id"] = creditMemo.InvoiceId.ToString(),
                    ["invoice_number"] = creditMemo.Invoice.InvoiceNumber ?? ""
                }
            };

            var refund = await refundService.CreateAsync(refundOptions, cancellationToken: ct);

            // Update credit memo with refund info
            creditMemo.StripeRefundId = refund.Id;
            creditMemo.IsRefunded = true;
            creditMemo.RefundedAt = DateTime.UtcNow;
            creditMemo.DateModified = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Processed Stripe refund {RefundId} for credit memo {CreditMemoNumber}, amount: {Amount}",
                refund.Id, creditMemo.CreditMemoNumber, creditMemo.Amount);

            return creditMemo;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, 
                "Stripe refund failed for credit memo {CreditMemoNumber}: {Message}", 
                creditMemo.CreditMemoNumber, ex.Message);

            throw new InvalidOperationException($"Stripe refund failed: {ex.Message}", ex);
        }
    }

    public async Task<string> GenerateCreditMemoNumberAsync(CancellationToken ct = default)
    {
        if (!_tenantContext.TenantId.HasValue)
            throw new InvalidOperationException("Tenant context is required");

        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var year = DateTime.UtcNow.Year;
        var prefix = $"CM-{year}-";

        var lastNumber = await context.CreditMemos
            .Where(cm => cm.TenantId == _tenantContext.TenantId.Value &&
                        cm.CreditMemoNumber.StartsWith(prefix))
            .Select(cm => cm.CreditMemoNumber)
            .OrderByDescending(n => n)
            .FirstOrDefaultAsync(ct);

        int nextNumber = 1;
        if (!string.IsNullOrEmpty(lastNumber))
        {
            var numberPart = lastNumber.Replace(prefix, "");
            if (int.TryParse(numberPart, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }

        return $"{prefix}{nextNumber:D5}";
    }

    public async Task<decimal> GetTotalCreditsForInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        return await context.CreditMemos
            .Where(cm => cm.InvoiceId == invoiceId && 
                        cm.Status == CreditMemoStatus.Applied && 
                        cm.DateDeleted == null)
            .SumAsync(cm => cm.Amount, ct);
    }
}
