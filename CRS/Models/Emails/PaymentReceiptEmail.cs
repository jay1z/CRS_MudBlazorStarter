namespace CRS.Models.Emails;

/// <summary>
/// Email model for payment receipt sent after successful invoice payment.
/// </summary>
public class PaymentReceiptEmail
{
    /// <summary>
    /// Recipient email address.
    /// </summary>
    public string RecipientEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Recipient name for personalization.
    /// </summary>
    public string? RecipientName { get; set; }
    
    /// <summary>
    /// Name of the tenant (reserve study company).
    /// </summary>
    public string TenantName { get; set; } = string.Empty;
    
    /// <summary>
    /// Invoice number (e.g., "INV-2024-001").
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Invoice ID for linking.
    /// </summary>
    public Guid InvoiceId { get; set; }
    
    /// <summary>
    /// Community or client name being billed.
    /// </summary>
    public string? ClientName { get; set; }
    
    /// <summary>
    /// Total invoice amount.
    /// </summary>
    public decimal TotalAmount { get; set; }
    
    /// <summary>
    /// Amount paid in this transaction.
    /// </summary>
    public decimal AmountPaid { get; set; }
    
    /// <summary>
    /// Remaining balance (if partial payment).
    /// </summary>
    public decimal BalanceRemaining { get; set; }
    
    /// <summary>
    /// Whether the invoice is now fully paid.
    /// </summary>
    public bool IsFullyPaid => BalanceRemaining <= 0;
    
    /// <summary>
    /// Payment method used (e.g., "Credit Card", "ACH Bank Transfer").
    /// </summary>
    public string? PaymentMethod { get; set; }
    
    /// <summary>
    /// Stripe payment reference ID (for records).
    /// </summary>
    public string? PaymentReference { get; set; }
    
    /// <summary>
    /// Date and time of payment.
    /// </summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Description of what was paid for.
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// URL to view the invoice details online.
    /// </summary>
    public string? InvoiceUrl { get; set; }
    
    /// <summary>
    /// URL to download the invoice PDF.
    /// </summary>
    public string? DownloadPdfUrl { get; set; }
    
    /// <summary>
    /// Support email address.
    /// </summary>
    public string? SupportEmail { get; set; }
}
