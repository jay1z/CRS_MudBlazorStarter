using CRS.Data;
using CRS.Models.Billing;
using CRS.Services.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CRS.Controllers {
    [ApiController]
    [Route("api/stripe/webhook")]
    public class StripeWebhookController : ControllerBase {
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IBillingService _billingService;
        private readonly StripeOptions _stripeOptions;

        public StripeWebhookController(ILogger<StripeWebhookController> logger, ApplicationDbContext db, IBillingService billingService, IOptions<StripeOptions> stripeOptions) {
            _logger = logger;
            _db = db;
            _billingService = billingService;
            _stripeOptions = stripeOptions.Value;
        }

        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken ct) {
            var json = await new StreamReader(Request.Body).ReadToEndAsync(ct);
            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            Event stripeEvent;
            try {
                stripeEvent = EventUtility.ConstructEvent(json, signature, _stripeOptions.WebhookSecret, throwOnApiVersionMismatch: false);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Stripe webhook signature validation failed");
                return BadRequest();
            }

            // Idempotency: persist event id; skip if processed
            var exists = await _db.StripeEventLogs.AsNoTracking().AnyAsync(e => e.EventId == stripeEvent.Id, ct);
            if (!exists) {
                _db.StripeEventLogs.Add(new StripeEventLog { EventId = stripeEvent.Id, Type = stripeEvent.Type, RawJson = json, ReceivedAt = DateTime.UtcNow, Processed = false });
                await _db.SaveChangesAsync(ct);
            }

            try {
                switch (stripeEvent.Type) {
                    case "checkout.session.completed":
                        HandleCheckoutSessionCompleted(stripeEvent);
                        break;
                    case "customer.subscription.created":
                    case "customer.subscription.updated":
                        await HandleSubscriptionUpdatedAsync(stripeEvent, ct);
                        break;
                    case "customer.subscription.deleted":
                        await HandleSubscriptionDeletedAsync(stripeEvent, ct);
                        break;
                    case "invoice.payment_succeeded":
                        await HandleInvoicePaymentAsync(stripeEvent, true, ct);
                        break;
                    case "invoice.payment_failed":
                        await HandleInvoicePaymentAsync(stripeEvent, false, ct);
                        break;
                    default:
                        break;
                }
                // mark processed
                var log = await _db.StripeEventLogs.FirstOrDefaultAsync(e => e.EventId == stripeEvent.Id, ct);
                if (log != null) { log.Processed = true; await _db.SaveChangesAsync(ct); }
            } catch (Exception ex) {
                var log = await _db.StripeEventLogs.FirstOrDefaultAsync(e => e.EventId == stripeEvent.Id, ct);
                if (log != null) { log.Error = ex.Message; await _db.SaveChangesAsync(ct); }
                _logger.LogError(ex, "Error handling Stripe event {Id}", stripeEvent.Id);
            }

            return Ok();
        }

        private void HandleCheckoutSessionCompleted(Event stripeEvent) {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;
            _logger.LogInformation("Checkout session completed {SessionId} customer {CustomerId}", session.Id, session.CustomerId);
        }

        private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent, CancellationToken ct) {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null) return;
            var status = MapStatus(subscription.Status);
            CRS.Models.SubscriptionTier? tier = null;
            var priceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id;
            if (priceId != null) {
                if (priceId == _stripeOptions.StarterMonthlyPriceId || priceId == _stripeOptions.StarterYearlyPriceId || priceId == _stripeOptions.StartupPriceId) tier = CRS.Models.SubscriptionTier.Startup;
                else if (priceId == _stripeOptions.ProMonthlyPriceId || priceId == _stripeOptions.ProYearlyPriceId || priceId == _stripeOptions.ProPriceId) tier = CRS.Models.SubscriptionTier.Pro;
                else if (priceId == _stripeOptions.EnterpriseMonthlyPriceId || priceId == _stripeOptions.EnterpriseYearlyPriceId || priceId == _stripeOptions.EnterprisePriceId) tier = CRS.Models.SubscriptionTier.Enterprise;
            }
            await _billingService.ApplySubscriptionUpdateAsync(subscription.Id, subscription.CustomerId!, tier, status, ct);
        }

        private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken ct) {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null) return;
            await _billingService.ApplySubscriptionUpdateAsync(subscription.Id, subscription.CustomerId!, null, CRS.Models.SubscriptionStatus.Canceled, ct);
        }

        private async Task HandleInvoicePaymentAsync(Event stripeEvent, bool succeeded, CancellationToken ct) {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null) return;
            var customerId = invoice.CustomerId;
            if (string.IsNullOrWhiteSpace(customerId)) return;
            await _billingService.ApplySubscriptionUpdateAsync(string.Empty, customerId!, null, succeeded ? CRS.Models.SubscriptionStatus.Active : CRS.Models.SubscriptionStatus.PastDue, ct);
        }

        private static CRS.Models.SubscriptionStatus MapStatus(string? stripeStatus) => stripeStatus switch {
            "active" => CRS.Models.SubscriptionStatus.Active,
            "trialing" => CRS.Models.SubscriptionStatus.Trialing,
            "past_due" => CRS.Models.SubscriptionStatus.PastDue,
            "canceled" => CRS.Models.SubscriptionStatus.Canceled,
            "unpaid" => CRS.Models.SubscriptionStatus.Unpaid,
            "incomplete" => CRS.Models.SubscriptionStatus.Incomplete,
            _ => CRS.Models.SubscriptionStatus.None
        };
    }
}
