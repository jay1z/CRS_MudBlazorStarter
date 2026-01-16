using System.ComponentModel.DataAnnotations;
using CRS.Data;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Models;

/// <summary>
/// Tenant-specific settings for invoice generation and management.
/// </summary>
public class TenantInvoiceSettings : BaseModel, ITenantScoped
{
    public int TenantId { get; set; }

    // ============================================
    // INVOICE NUMBERING
    // ============================================

    /// <summary>
    /// Prefix for invoice numbers (e.g., "INV", "INVOICE", or company abbreviation).
    /// Default: "INV"
    /// </summary>
    [MaxLength(20)]
    public string InvoicePrefix { get; set; } = "INV";

    /// <summary>
    /// Format pattern for invoice numbers.
    /// Available tokens: {PREFIX}, {YEAR}, {MONTH}, {NUMBER}
    /// Default: "{PREFIX}-{YEAR}-{NUMBER}"
    /// Examples: 
    ///   - "{PREFIX}-{YEAR}-{NUMBER}" => INV-2025-00001
    ///   - "{PREFIX}{YEAR}{MONTH}{NUMBER}" => INV20250100001
    ///   - "{NUMBER}" => 00001
    /// </summary>
    [MaxLength(100)]
    public string InvoiceNumberFormat { get; set; } = "{PREFIX}-{YEAR}-{NUMBER}";

    /// <summary>
    /// Number of digits for the sequential number portion.
    /// Default: 5 (e.g., 00001)
    /// </summary>
    [Range(3, 10)]
    public int NumberPadding { get; set; } = 5;

    /// <summary>
    /// The next sequential number to use. Resets based on ResetFrequency.
    /// </summary>
    public int NextInvoiceNumber { get; set; } = 1;

    /// <summary>
    /// When to reset the sequential number.
    /// </summary>
    public NumberResetFrequency ResetFrequency { get; set; } = NumberResetFrequency.Yearly;

    /// <summary>
    /// Year of the last invoice number used (for yearly reset).
    /// </summary>
    public int LastInvoiceYear { get; set; } = DateTime.UtcNow.Year;

    /// <summary>
    /// Month of the last invoice number used (for monthly reset).
    /// </summary>
    public int LastInvoiceMonth { get; set; } = DateTime.UtcNow.Month;

    // ============================================
    // CREDIT MEMO NUMBERING
    // ============================================

    /// <summary>
    /// Prefix for credit memo numbers.
    /// </summary>
    [MaxLength(20)]
    public string CreditMemoPrefix { get; set; } = "CM";

    /// <summary>
    /// The next sequential number for credit memos.
    /// </summary>
    public int NextCreditMemoNumber { get; set; } = 1;

    // ============================================
    // PAYMENT TERMS DEFAULTS
    // ============================================

    /// <summary>
    /// Default Net Days for new invoices.
    /// </summary>
    public int DefaultNetDays { get; set; } = 30;

    /// <summary>
    /// Default early payment discount percentage (e.g., 2 for 2%).
    /// </summary>
    [Precision(5, 2)]
    public decimal DefaultEarlyPaymentDiscount { get; set; } = 0;

    /// <summary>
    /// Default days within which early payment discount applies.
    /// </summary>
    public int DefaultEarlyPaymentDays { get; set; } = 10;

    /// <summary>
    /// Default monthly late payment interest rate (e.g., 1.5 for 1.5%).
    /// </summary>
    [Precision(5, 2)]
    public decimal DefaultLateInterestRate { get; set; } = 0;

    /// <summary>
    /// Default grace period in days before late interest accrues.
    /// </summary>
    public int DefaultGracePeriodDays { get; set; } = 0;

    // ============================================
    // AUTOMATION SETTINGS
    // ============================================

    /// <summary>
    /// Automatically generate the next milestone invoice when current is paid.
    /// </summary>
    public bool AutoGenerateNextMilestone { get; set; } = false;

    /// <summary>
    /// Send email notification when invoice is auto-generated.
    /// </summary>
    public bool NotifyOnAutoGenerate { get; set; } = true;

    /// <summary>
    /// Automatically send invoice to client when generated from proposal.
    /// </summary>
    public bool AutoSendOnCreate { get; set; } = false;

    /// <summary>
    /// Email address to notify for auto-generated invoices (if empty, uses creator).
    /// </summary>
    [MaxLength(200)]
    public string? NotificationEmail { get; set; }

    // ============================================
    // REMINDER SETTINGS
    // ============================================

    /// <summary>
    /// Enable automated invoice reminders.
    /// </summary>
    public bool EnableAutoReminders { get; set; } = true;

    /// <summary>
    /// Maximum number of automated reminders per invoice.
    /// </summary>
    public int MaxAutoReminders { get; set; } = 5;

    /// <summary>
    /// Days before due date to send first reminder (negative = before due).
    /// </summary>
    public int FirstReminderDays { get; set; } = -3;

    // ============================================
    // TAX DEFAULTS
    // ============================================

        /// <summary>
        /// Default tax rate for new invoices (percentage).
        /// </summary>
        [Precision(5, 2)]
        public decimal DefaultTaxRate { get; set; } = 0;

        /// <summary>
        /// Tax label (e.g., "Sales Tax", "VAT", "GST").
        /// </summary>
        [MaxLength(50)]
        public string TaxLabel { get; set; } = "Tax";

        // ============================================
        // BRANDING / TEMPLATE SETTINGS
        // ============================================

        /// <summary>
        /// Whether to use tenant branding on invoices.
        /// If false, uses default/minimal branding.
        /// </summary>
        public bool UseTenantBranding { get; set; } = true;

        /// <summary>
        /// Primary brand color (hex format, e.g., "#667eea").
        /// Used for headers, table headers, and accents.
        /// </summary>
        [MaxLength(20)]
        public string PrimaryColor { get; set; } = "#667eea";

        /// <summary>
        /// Secondary brand color (hex format).
        /// Used for secondary accents.
        /// </summary>
        [MaxLength(20)]
        public string SecondaryColor { get; set; } = "#764ba2";

        /// <summary>
        /// URL to the company logo for invoice headers.
        /// </summary>
        [MaxLength(500)]
        public string? LogoUrl { get; set; }

        /// <summary>
        /// Company name to display on invoices.
        /// If null, uses tenant name.
        /// </summary>
        [MaxLength(200)]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Company address line for invoice header.
        /// </summary>
        [MaxLength(500)]
        public string? CompanyAddress { get; set; }

        /// <summary>
        /// Company phone for invoice header.
        /// </summary>
        [MaxLength(50)]
        public string? CompanyPhone { get; set; }

        /// <summary>
        /// Company email for invoice header.
        /// </summary>
        [MaxLength(200)]
        public string? CompanyEmail { get; set; }

        /// <summary>
        /// Company website for invoice header.
        /// </summary>
        [MaxLength(200)]
        public string? CompanyWebsite { get; set; }

        /// <summary>
        /// Tagline/slogan shown under company name.
        /// </summary>
        [MaxLength(200)]
        public string? Tagline { get; set; }

        /// <summary>
        /// Custom footer text for invoices.
        /// </summary>
        [MaxLength(500)]
        public string FooterText { get; set; } = "Thank you for your business!";

        /// <summary>
        /// Default terms and conditions for invoices.
        /// </summary>
        [MaxLength(2000)]
        public string? DefaultTerms { get; set; }

        /// <summary>
        /// Default notes for invoices.
        /// </summary>
        [MaxLength(2000)]
        public string? DefaultNotes { get; set; }

        /// <summary>
        /// Show "PAID" watermark on paid invoices in PDF.
        /// </summary>
        public bool ShowPaidWatermark { get; set; } = true;

        /// <summary>
        /// Include payment instructions in PDF.
        /// </summary>
        [MaxLength(1000)]
        public string? PaymentInstructions { get; set; }
    }

/// <summary>
/// Frequency for resetting invoice sequential numbers.
/// </summary>
public enum NumberResetFrequency
{
    /// <summary>
    /// Never reset - continuous numbering.
    /// </summary>
    Never,

    /// <summary>
    /// Reset at the start of each year.
    /// </summary>
    Yearly,

    /// <summary>
    /// Reset at the start of each month.
    /// </summary>
    Monthly
}
