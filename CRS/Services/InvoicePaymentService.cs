using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Email;
using CRS.Models.Emails;
using CRS.Services.Billing;
using CRS.Services.Email;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Stripe.Checkout;

namespace CRS.Services;

/// <summary>
/// Service for handling Stripe payment processing for invoices.
/// </summary>
public class InvoicePaymentService : IInvoicePaymentService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IStripeClientFactory _stripeClientFactory;
    private readonly ITenantContext _tenantContext;
    private readonly IInvoiceService _invoiceService;
    private readonly IMailer _mailer;
    private readonly ILogger<InvoicePaymentService> _logger;
    private readonly IConfiguration _configuration;

    public InvoicePaymentService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IStripeClientFactory stripeClientFactory,
        ITenantContext tenantContext,
        IInvoiceService invoiceService,
        IMailer mailer,
        ILogger<InvoicePaymentService> logger,
        IConfiguration configuration)
    {
        _dbFactory = dbFactory;
        _stripeClientFactory = stripeClientFactory;
        _tenantContext = tenantContext;
        _invoiceService = invoiceService;
        _mailer = mailer;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        Guid invoiceId, 
        string successUrl, 
        string cancelUrl, 
        CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices
            .Include(i => i.LineItems.Where(li => li.DateDeleted == null))
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        // Don't allow payment for voided or paid invoices
        if (invoice.Status == InvoiceStatus.Voided)
            throw new InvalidOperationException("Cannot create payment for voided invoice");
        if (invoice.Status == InvoiceStatus.Paid)
            throw new InvalidOperationException("Invoice is already paid");

        var balanceDue = invoice.TotalAmount - invoice.AmountPaid;
        if (balanceDue <= 0)
            throw new InvalidOperationException("No balance due on this invoice");

        // Build line items for Stripe
        var stripeLineItems = new List<SessionLineItemOptions>();
        
        // Use invoice line items or create a single line item
        if (invoice.LineItems.Any())
        {
            foreach (var item in invoice.LineItems.OrderBy(li => li.SortOrder))
            {
                stripeLineItems.Add(new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmountDecimal = item.UnitPrice * 100, // Convert to cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Description ?? "Service",
                            Description = $"Invoice #{invoice.InvoiceNumber}"
                        }
                    },
                    Quantity = (long)Math.Ceiling(item.Quantity)
                });
            }
        }
        else
        {
            // Fallback to single line item
            stripeLineItems.Add(new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "usd",
                    UnitAmountDecimal = balanceDue * 100,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Invoice #{invoice.InvoiceNumber}",
                        Description = invoice.ReserveStudy?.Community?.Name ?? "Reserve Study Services"
                    }
                },
                Quantity = 1
            });
        }

        // Create Stripe checkout session
        var client = _stripeClientFactory.CreateClient();
        var sessionService = new SessionService(client);

        // Get tenant for Stripe Connect account and platform fee calculation
        var tenant = await context.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == invoice.TenantId, ct);

        var hasConnectAccount = !string.IsNullOrEmpty(tenant?.StripeConnectAccountId) 
            && tenant.StripeConnectOnboardingComplete 
            && tenant.StripeConnectCardPaymentsEnabled;

        // Calculate platform fee (application_fee) based on tenant's subscription tier
        long? applicationFeeAmount = null;
        if (hasConnectAccount && tenant != null)
        {
            var feeRate = SubscriptionTierDefaults.GetPlatformFeeRate(tenant);
            applicationFeeAmount = (long)Math.Ceiling(balanceDue * feeRate * 100); // Convert to cents

            _logger.LogInformation(
                "Calculated application fee for invoice {InvoiceNumber}: {Amount:C} × {Rate:P2} = {Fee} cents",
                invoice.InvoiceNumber, balanceDue, feeRate, applicationFeeAmount);
        }

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = $"{successUrl}?invoice_id={invoice.Id}&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{cancelUrl}?invoice_id={invoice.Id}",
            LineItems = stripeLineItems,
            CustomerEmail = invoice.BillToEmail,
            Metadata = new Dictionary<string, string>
            {
                ["invoice_id"] = invoice.Id.ToString(),
                ["invoice_number"] = invoice.InvoiceNumber,
                ["tenant_id"] = invoice.TenantId.ToString(),
                ["reserve_study_id"] = invoice.ReserveStudyId.ToString()
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                // Save payment method for future invoice payments
                SetupFutureUsage = "off_session",
                Metadata = new Dictionary<string, string>
                {
                    ["invoice_id"] = invoice.Id.ToString(),
                    ["invoice_number"] = invoice.InvoiceNumber
                },
                // Application fee goes to the platform (ALX Reserve Cloud)
                ApplicationFeeAmount = applicationFeeAmount
            },
            // Enable multiple payment methods including ACH
            PaymentMethodTypes = ["card", "us_bank_account"],
            ExpiresAt = DateTime.UtcNow.AddHours(24) // Session expires in 24 hours
        };

        // Configure for Direct Charges to Connected Account if Stripe Connect is set up
        Stripe.RequestOptions? requestOptions = null;
        if (hasConnectAccount && tenant != null)
        {
            // Direct Charge: Create the checkout session on the connected account
            requestOptions = new Stripe.RequestOptions
            {
                StripeAccount = tenant.StripeConnectAccountId
            };

            _logger.LogInformation(
                "Using Direct Charge to connected account {AccountId} for invoice {InvoiceNumber}",
                tenant.StripeConnectAccountId, invoice.InvoiceNumber);
        }
        else
        {
            _logger.LogInformation(
                "No Stripe Connect account for tenant {TenantId}, creating standard checkout for invoice {InvoiceNumber}",
                invoice.TenantId, invoice.InvoiceNumber);

            // Remove application fee if no connected account (fee only applies to Connect)
            sessionOptions.PaymentIntentData.ApplicationFeeAmount = null;
        }

        var session = await sessionService.CreateAsync(sessionOptions, requestOptions, ct);

        // Update invoice with payment URL
        invoice.StripeCheckoutSessionId = session.Id;
        invoice.PaymentUrl = session.Url;
        invoice.PaymentUrlExpires = session.ExpiresAt;
        invoice.DateModified = DateTime.UtcNow;

        await context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created Stripe checkout session {SessionId} for invoice {InvoiceNumber} amount {Amount:C}",
            session.Id, invoice.InvoiceNumber, balanceDue);

        return session.Url;
    }

    public async Task HandlePaymentSucceededAsync(
        string checkoutSessionId, 
        string? paymentIntentId, 
        long amountPaidCents, 
        CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.StripeCheckoutSessionId == checkoutSessionId && i.DateDeleted == null, ct);

        if (invoice == null)
        {
            _logger.LogWarning("No invoice found for checkout session {SessionId}", checkoutSessionId);
            return;
        }

        var amountPaid = amountPaidCents / 100m; // Convert from cents

            invoice.AmountPaid += amountPaid;
            invoice.StripePaymentIntentId = paymentIntentId;
            invoice.PaymentMethod = "Stripe";
            invoice.PaymentReference = paymentIntentId;
            invoice.DateModified = DateTime.UtcNow;

            // Update status based on payment
            var wasPaidInFull = false;
            if (invoice.AmountPaid >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidAt = DateTime.UtcNow;
                wasPaidInFull = true;
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.Status = InvoiceStatus.PartiallyPaid;
            }

            // Create payment record
            var paymentRecord = new PaymentRecord
            {
                TenantId = invoice.TenantId,
                InvoiceId = invoice.Id,
                Amount = amountPaid,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = "Stripe",
                ReferenceNumber = paymentIntentId,
                IsAutomatic = true,
                Notes = $"Stripe payment via checkout session {checkoutSessionId}"
            };
            context.PaymentRecords.Add(paymentRecord);

            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Recorded Stripe payment for invoice {InvoiceNumber}: {Amount:C} via {PaymentIntent}",
                invoice.InvoiceNumber, amountPaid, paymentIntentId);

            // Send payment receipt email
            await SendPaymentReceiptAsync(invoice, amountPaid, paymentIntentId, context, ct);

            // Auto-generate next milestone if enabled
            if (wasPaidInFull && invoice.MilestoneType.HasValue)
            {
                await TryGenerateNextMilestoneAsync(invoice, context, ct);
            }
        }

        /// <summary>
        /// Sends a payment receipt email to the invoice recipient.
        /// </summary>
        private async Task SendPaymentReceiptAsync(
            Invoice invoice, 
            decimal amountPaid, 
            string? paymentIntentId, 
            ApplicationDbContext context, 
            CancellationToken ct)
        {
            try
            {
                if (string.IsNullOrEmpty(invoice.BillToEmail))
                {
                    _logger.LogWarning("Cannot send payment receipt for invoice {InvoiceNumber}: no BillToEmail", invoice.InvoiceNumber);
                    return;
                }

                // Get tenant info for the email
                var tenant = await context.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == invoice.TenantId, ct);

                var baseUrl = _configuration["App:RootDomain"] ?? "reservecloud.com";
                var subdomain = tenant?.Subdomain ?? "app";
                var invoiceUrl = $"https://{subdomain}.{baseUrl}/Invoices/Details/{invoice.Id}";

                var receiptEmail = new PaymentReceiptEmail
                {
                    RecipientEmail = invoice.BillToEmail,
                    RecipientName = invoice.BillToName,
                    TenantName = tenant?.Name ?? "ALX Reserve Cloud",
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceId = invoice.Id,
                    ClientName = invoice.BillToName,
                    TotalAmount = invoice.TotalAmount,
                    AmountPaid = amountPaid,
                    BalanceRemaining = invoice.TotalAmount - invoice.AmountPaid,
                    PaymentMethod = "Credit Card", // Could be enhanced to detect ACH
                    PaymentReference = paymentIntentId,
                    PaymentDate = DateTime.UtcNow,
                    Description = $"Reserve Study Services",
                    InvoiceUrl = invoiceUrl,
                    SupportEmail = tenant?.DefaultNotificationEmail
                };

                var mailable = new PaymentReceiptMailable(receiptEmail);
                await _mailer.SendAsync(mailable);

                _logger.LogInformation(
                    "Sent payment receipt email to {Email} for invoice {InvoiceNumber}",
                    invoice.BillToEmail, invoice.InvoiceNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send payment receipt email for invoice {InvoiceNumber}", 
                    invoice.InvoiceNumber);
                // Don't rethrow - receipt email failure shouldn't break payment processing
            }
        }

        /// <summary>
        /// Attempts to auto-generate the next milestone invoice if settings allow.
        /// </summary>
        private async Task TryGenerateNextMilestoneAsync(Invoice paidInvoice, ApplicationDbContext context, CancellationToken ct)
        {
            try
            {
                // Get tenant settings
                var settings = await context.TenantInvoiceSettings
                    .FirstOrDefaultAsync(s => s.TenantId == paidInvoice.TenantId && s.DateDeleted == null, ct);

                if (settings == null || !settings.AutoGenerateNextMilestone)
                {
                    return;
                }

                // Determine the next milestone type
                var nextMilestone = GetNextMilestone(paidInvoice.MilestoneType);
                if (nextMilestone == null)
                {
                    _logger.LogDebug("No next milestone for {CurrentMilestone}", paidInvoice.MilestoneType);
                    return;
                }

                // Check if next milestone invoice already exists
                var existingNext = await context.Invoices
                    .AnyAsync(i => i.ReserveStudyId == paidInvoice.ReserveStudyId &&
                                  i.MilestoneType == nextMilestone &&
                                  i.DateDeleted == null, ct);

                if (existingNext)
                {
                    _logger.LogDebug("Next milestone invoice already exists for reserve study {StudyId}", paidInvoice.ReserveStudyId);
                    return;
                }

                // Create the next milestone invoice
                var nextInvoice = await _invoiceService.CreateNextMilestoneInvoiceAsync(
                    paidInvoice.Id, 
                    nextMilestone.Value, 
                    ct);

                if (nextInvoice != null)
                            {
                                _logger.LogInformation(
                                    "Auto-generated next milestone invoice {InvoiceNumber} ({Milestone}) for reserve study {StudyId}",
                                    nextInvoice.InvoiceNumber, nextMilestone, paidInvoice.ReserveStudyId);

                                // Send notification if enabled
                                if (settings.NotifyOnAutoGenerate)
                                {
                                    await SendAutoGeneratedNotificationAsync(nextInvoice, paidInvoice, settings, ct);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to auto-generate next milestone invoice for {InvoiceId}", paidInvoice.Id);
                            // Don't throw - this is a best-effort operation
                        }
                    }

                /// <summary>
                /// Sends notification email when an invoice is auto-generated.
                /// </summary>
                private async Task SendAutoGeneratedNotificationAsync(
                    Invoice newInvoice, 
                    Invoice paidInvoice, 
                    TenantInvoiceSettings settings,
                    CancellationToken ct)
                {
                    try
                    {
                        // Determine recipient - use settings email or fall back
                        var recipientEmail = settings.NotificationEmail;

                        if (string.IsNullOrEmpty(recipientEmail))
                        {
                            _logger.LogDebug("No notification email configured for tenant {TenantId}", settings.TenantId);
                            return;
                        }

                        await using var context = await _dbFactory.CreateDbContextAsync(ct);

                        // Load reserve study for the notification
                        var reserveStudy = await context.ReserveStudies
                            .Include(rs => rs.Community)
                            .FirstOrDefaultAsync(rs => rs.Id == newInvoice.ReserveStudyId, ct);

                        var baseUrl = _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://app.reservecloud.com";

                        var notification = new InvoiceStaffNotificationEmail
                        {
                            Invoice = newInvoice,
                            ReserveStudy = reserveStudy,
                            BaseUrl = baseUrl,
                            NotificationType = InvoiceNotificationType.AutoGenerated,
                            PreviousInvoice = paidInvoice,
                            Details = $"A new invoice has been automatically generated because invoice {paidInvoice.InvoiceNumber} was paid in full."
                        };

                        var mailable = new InvoiceStaffNotificationMailable(notification, recipientEmail);
                        await _mailer.SendAsync(mailable);

                        _logger.LogInformation(
                            "Sent auto-generated notification for invoice {InvoiceNumber} to {Email}",
                            newInvoice.InvoiceNumber, recipientEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send auto-generated notification for invoice {InvoiceId}", newInvoice.Id);
                        // Don't throw - notification failure shouldn't break the flow
                    }
                }

                    /// <summary>
                    /// Gets the next milestone type based on the current milestone.
                    /// </summary>
                    private static InvoiceMilestoneType? GetNextMilestone(InvoiceMilestoneType? current)
                    {
                        return current switch
                        {
                            InvoiceMilestoneType.Deposit => InvoiceMilestoneType.SiteVisitComplete,
                            InvoiceMilestoneType.SiteVisitComplete => InvoiceMilestoneType.DraftReportDelivery,
                            InvoiceMilestoneType.DraftReportDelivery => InvoiceMilestoneType.FinalDelivery,
                            _ => null // FullPayment, FinalDelivery, or Custom have no automatic next
                        };
                    }

                    public async Task<string> GetOrCreatePaymentUrlAsync(Guid invoiceId, string baseUrl, CancellationToken ct = default)
                {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        // Check if existing URL is still valid
        if (HasValidPaymentUrl(invoice))
        {
            return invoice.PaymentUrl!;
        }

        // Create new checkout session
        var successUrl = $"{baseUrl}/Invoices/PaymentSuccess";
        var cancelUrl = $"{baseUrl}/Invoices/Details/{invoiceId}";

        return await CreateCheckoutSessionAsync(invoiceId, successUrl, cancelUrl, ct);
    }

    public bool HasValidPaymentUrl(Invoice invoice)
    {
        return !string.IsNullOrEmpty(invoice.PaymentUrl) &&
               invoice.PaymentUrlExpires.HasValue &&
               invoice.PaymentUrlExpires.Value > DateTime.UtcNow.AddMinutes(5); // 5 minute buffer
    }
}
