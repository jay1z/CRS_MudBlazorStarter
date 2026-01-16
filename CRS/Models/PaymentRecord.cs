using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Data;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Models;

/// <summary>
/// Represents a payment record for an invoice, providing audit trail for all payments.
/// </summary>
public class PaymentRecord : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }

    [Required]
    [ForeignKey(nameof(Invoice))]
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    /// <summary>
    /// The amount of this payment.
    /// </summary>
    [Precision(18, 2)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Date and time the payment was received.
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Payment method (e.g., "Check", "ACH", "Credit Card", "Stripe", "Cash").
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Reference number for the payment (check number, transaction ID, etc.).
    /// </summary>
    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Stripe Payment Intent ID if paid via Stripe.
    /// </summary>
    [MaxLength(100)]
    public string? StripePaymentIntentId { get; set; }

    /// <summary>
    /// Notes about this payment.
    /// </summary>
    [MaxLength(500)]
    public string? Notes { get; set; }

    /// <summary>
    /// User who recorded this payment.
    /// </summary>
    [ForeignKey(nameof(RecordedBy))]
    public Guid? RecordedByUserId { get; set; }
    public ApplicationUser? RecordedBy { get; set; }

    /// <summary>
    /// Whether this payment was recorded automatically (via webhook) or manually.
    /// </summary>
    public bool IsAutomatic { get; set; } = false;
}
