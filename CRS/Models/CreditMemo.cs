using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Horizon.Data;
using Horizon.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Models;

/// <summary>
/// Status of a credit memo.
/// </summary>
public enum CreditMemoStatus
{
    Draft,
    Applied,
    Voided
}

/// <summary>
/// Reason for issuing a credit memo.
/// </summary>
public enum CreditMemoReason
{
    Refund,
    Pricing_Adjustment,
    Service_Credit,
    Duplicate_Payment,
    Cancelled_Service,
    Other
}

/// <summary>
/// Represents a credit memo that reduces an invoice balance or provides a refund.
/// </summary>
public class CreditMemo : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }

    /// <summary>
    /// Credit memo number (e.g., CM-2025-00001).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CreditMemoNumber { get; set; } = string.Empty;

    /// <summary>
    /// The invoice this credit memo applies to.
    /// </summary>
    [Required]
    [ForeignKey(nameof(Invoice))]
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    /// <summary>
    /// Reserve study associated with this credit (optional).
    /// </summary>
    [ForeignKey(nameof(ReserveStudy))]
    public Guid? ReserveStudyId { get; set; }
    public ReserveStudy? ReserveStudy { get; set; }

    /// <summary>
    /// Date the credit memo was issued.
    /// </summary>
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Amount of the credit.
    /// </summary>
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Reason for the credit memo.
    /// </summary>
    public CreditMemoReason Reason { get; set; } = CreditMemoReason.Other;

    /// <summary>
    /// Detailed description of why the credit was issued.
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Current status of the credit memo.
    /// </summary>
    public CreditMemoStatus Status { get; set; } = CreditMemoStatus.Draft;

    /// <summary>
    /// Date the credit was applied to the invoice.
    /// </summary>
    public DateTime? AppliedAt { get; set; }

    /// <summary>
    /// Date the credit memo was voided.
    /// </summary>
    public DateTime? VoidedAt { get; set; }

    /// <summary>
    /// Reason for voiding (if voided).
    /// </summary>
    [MaxLength(500)]
    public string? VoidReason { get; set; }

    /// <summary>
    /// If a refund was issued, the Stripe refund ID.
    /// </summary>
    [MaxLength(100)]
    public string? StripeRefundId { get; set; }

    /// <summary>
    /// Whether a refund was processed for this credit memo.
    /// </summary>
    public bool IsRefunded { get; set; } = false;

    /// <summary>
    /// Date the refund was processed.
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Internal notes about this credit memo.
    /// </summary>
    [MaxLength(1000)]
    public string? InternalNotes { get; set; }

    // ============================================
    // BILL TO (copied from invoice)
    // ============================================

    [MaxLength(200)]
    public string? BillToName { get; set; }

    [MaxLength(200)]
    public string? BillToEmail { get; set; }

    // ============================================
    // USER TRACKING
    // ============================================

    [ForeignKey(nameof(CreatedBy))]
    public Guid? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }

    [ForeignKey(nameof(AppliedBy))]
    public Guid? AppliedByUserId { get; set; }
    public ApplicationUser? AppliedBy { get; set; }
}
