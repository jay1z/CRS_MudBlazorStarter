using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Data;
using Horizon.Services.Tenant;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Models;

/// <summary>
/// Represents an invoice for a reserve study.
/// Invoices are generated before narrative delivery to request payment.
/// </summary>
public class Invoice : BaseModel, ITenantScoped
{
    // Tenant scope
    public int TenantId { get; set; }

    // Required link to reserve study
    [Required]
    [ForeignKey(nameof(ReserveStudy))]
    public Guid ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    // Invoice identification
    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    // Status tracking
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    // Dates
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;

    public DateTime DueDate { get; set; } = DateTime.UtcNow.AddDays(30);

    public DateTime? SentAt { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? VoidedAt { get; set; }

    // Billing details
    [MaxLength(256)]
    public string? BillToName { get; set; }

    [MaxLength(256)]
    public string? BillToEmail { get; set; }

    [MaxLength(512)]
    public string? BillToAddress { get; set; }

    [MaxLength(100)]
    public string? BillToPhone { get; set; }

    // Amounts
    [Precision(18, 2)]
    public decimal Subtotal { get; set; }

    [Precision(18, 2)]
    public decimal TaxRate { get; set; } = 0;

    [Precision(18, 2)]
    public decimal TaxAmount { get; set; } = 0;

    [Precision(18, 2)]
    public decimal DiscountAmount { get; set; } = 0;

    [MaxLength(200)]
    public string? DiscountDescription { get; set; }

    [Precision(18, 2)]
    public decimal TotalAmount { get; set; }

    [Precision(18, 2)]
    public decimal AmountPaid { get; set; } = 0;

    [NotMapped]
    public decimal BalanceDue => TotalAmount - AmountPaid + LateInterestAccrued;

    // ============================================
    // PAYMENT TERMS
    // ============================================

    /// <summary>
    /// Early payment discount percentage (copied from proposal)
    /// </summary>
    [Precision(5, 2)]
    public decimal EarlyPaymentDiscountPercentage { get; set; } = 0m;

    /// <summary>
    /// Date by which early payment discount applies
    /// </summary>
    public DateTime? EarlyPaymentDiscountDate { get; set; }

    /// <summary>
    /// Early payment discount amount (calculated)
    /// </summary>
    [Precision(18, 2)]
    public decimal EarlyPaymentDiscountAmount { get; set; } = 0m;

    /// <summary>
    /// Monthly interest rate for late payments
    /// </summary>
    [Precision(5, 2)]
    public decimal LatePaymentInterestRate { get; set; } = 0m;

    /// <summary>
    /// Date after which late interest begins accruing (DueDate + GracePeriod)
    /// </summary>
    public DateTime? LateInterestStartDate { get; set; }

    /// <summary>
    /// Total late interest accrued on this invoice
    /// </summary>
    [Precision(18, 2)]
    public decimal LateInterestAccrued { get; set; } = 0m;

    /// <summary>
    /// Date when late interest was last calculated
    /// </summary>
    public DateTime? LastInterestCalculationDate { get; set; }

    /// <summary>
    /// Checks if early payment discount is still available
    /// </summary>
    [NotMapped]
    public bool IsEarlyPaymentDiscountAvailable => 
        EarlyPaymentDiscountPercentage > 0 && 
        EarlyPaymentDiscountDate.HasValue && 
        DateTime.UtcNow.Date <= EarlyPaymentDiscountDate.Value.Date &&
        Status != InvoiceStatus.Paid &&
        Status != InvoiceStatus.Voided;

    /// <summary>
    /// Gets the amount due if paid early (with discount)
    /// </summary>
    [NotMapped]
    public decimal EarlyPaymentAmount => IsEarlyPaymentDiscountAvailable 
        ? TotalAmount - EarlyPaymentDiscountAmount - AmountPaid 
        : BalanceDue;

    // Payment details
    [MaxLength(100)]
    public string? PaymentMethod { get; set; }

    [MaxLength(100)]
    public string? PaymentReference { get; set; }

    // ============================================
    // STRIPE PAYMENT
    // ============================================

    /// <summary>
    /// Stripe Payment Intent ID for online payments.
    /// </summary>
    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Stripe Checkout Session ID for payment tracking.
    /// </summary>
    [MaxLength(100)]
    public string? StripeCheckoutSessionId { get; set; }

    /// <summary>
    /// URL for online payment (Stripe hosted checkout).
    /// </summary>
    [MaxLength(500)]
    public string? PaymentUrl { get; set; }

    /// <summary>
    /// When the payment URL expires (Stripe sessions expire after 24 hours).
    /// </summary>
    public DateTime? PaymentUrlExpires { get; set; }

    // Notes
    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? InternalNotes { get; set; }

    [MaxLength(2000)]
    public string? Terms { get; set; }

    /// <summary>
    /// The payment milestone this invoice represents (e.g., Deposit, SiteVisitComplete, FinalDelivery)
    /// </summary>
    public InvoiceMilestoneType? MilestoneType { get; set; }

    /// <summary>
    /// Description of the milestone for display purposes
    /// </summary>
    [MaxLength(200)]
    public string? MilestoneDescription { get; set; }

    /// <summary>
    /// The percentage of the total proposal this invoice represents
    /// </summary>
    [Precision(5, 2)]
    public decimal? MilestonePercentage { get; set; }

    // User tracking
    [ForeignKey(nameof(CreatedBy))]
    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    [ForeignKey(nameof(SentBy))]
    public Guid? SentByUserId { get; set; }
    public ApplicationUser? SentBy { get; set; }

    // ============================================
    // REMINDER TRACKING
    // ============================================

    /// <summary>
    /// Number of reminders sent for this invoice.
    /// </summary>
    public int ReminderCount { get; set; } = 0;

    /// <summary>
    /// Date of the last reminder sent.
    /// </summary>
    public DateTime? LastReminderSent { get; set; }

    // ============================================
    // CLIENT PORTAL ACCESS
    // ============================================

    /// <summary>
    /// Unique access token for public invoice viewing (client portal).
    /// </summary>
    [MaxLength(64)]
    public string? AccessToken { get; set; }

    /// <summary>
    /// When the access token expires (or null for no expiration).
    /// </summary>
    public DateTime? AccessTokenExpires { get; set; }

    // Line items
    public virtual ICollection<InvoiceLineItem> LineItems { get; set; } = [];

    // Payment records (audit trail)
    public virtual ICollection<PaymentRecord> PaymentRecords { get; set; } = [];

    // Credit memos (refunds/adjustments)
    public virtual ICollection<CreditMemo> CreditMemos { get; set; } = [];

    /// <summary>
    /// Total credits applied to this invoice.
    /// </summary>
    [NotMapped]
    public decimal TotalCredits => CreditMemos?.Where(c => c.Status == CreditMemoStatus.Applied && c.DateDeleted == null).Sum(c => c.Amount) ?? 0;

    /// <summary>
    /// Net balance after credits.
    /// </summary>
    [NotMapped]
    public decimal NetBalanceDue => TotalAmount - AmountPaid - TotalCredits;

    /// <summary>
    /// Recalculates Subtotal, TaxAmount, and TotalAmount from line items.
    /// </summary>
    public void RecalculateTotals()
    {
        Subtotal = LineItems?.Sum(li => li.LineTotal) ?? 0;
        TaxAmount = Subtotal * (TaxRate / 100);
        TotalAmount = Subtotal + TaxAmount - DiscountAmount;
    }
}

/// <summary>
/// Represents a line item on an invoice.
/// </summary>
public class InvoiceLineItem : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }

    [Required]
    [ForeignKey(nameof(Invoice))]
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    [Required]
    [MaxLength(256)]
    public string Description { get; set; } = string.Empty;

    [Precision(18, 2)]
    public decimal Quantity { get; set; } = 1;

    [MaxLength(50)]
    public string? Unit { get; set; }

    [Precision(18, 2)]
    public decimal UnitPrice { get; set; }

    [Precision(18, 2)]
    public decimal LineTotal { get; set; }

    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Recalculates LineTotal from Quantity and UnitPrice.
    /// </summary>
    public void RecalculateTotal()
    {
        LineTotal = Quantity * UnitPrice;
    }
}

/// <summary>
/// Status of an invoice in its lifecycle.
/// </summary>
public enum InvoiceStatus
{
    /// <summary>
    /// Invoice is being created but not yet finalized.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Invoice has been finalized and is ready to send.
    /// </summary>
    Finalized = 1,

    /// <summary>
    /// Invoice has been sent to the client.
    /// </summary>
    Sent = 2,

    /// <summary>
    /// Invoice has been viewed by the client.
    /// </summary>
    Viewed = 3,

    /// <summary>
    /// Partial payment has been received.
    /// </summary>
    PartiallyPaid = 4,

    /// <summary>
    /// Invoice has been fully paid.
    /// </summary>
    Paid = 5,

    /// <summary>
    /// Invoice is past due date and unpaid.
    /// </summary>
    Overdue = 6,

    /// <summary>
    /// Invoice has been cancelled/voided.
    /// </summary>
    Voided = 7
}
