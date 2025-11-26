using CRS.Data;
using CRS.Models.Billing;
using CRS.Services.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Coravel.Mailer.Mail.Interfaces; // mailer retained for potential future notifications
using CRS.Services.Email; // BasicMailable (unused here but kept if needed later)
using CRS.Services.Provisioning; // owner provisioning service

namespace CRS.Controllers {
    [ApiController]
    [Route("api/stripe/webhook")]
    public class StripeWebhookController : ControllerBase {
        private readonly ILogger<StripeWebhookController> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IBillingService _billingService;
        private readonly StripeOptions _stripeOptions;
        private readonly IMailer _mailer;
        private readonly IOwnerProvisioningService _ownerProvisioning;
        private readonly IStripeClientFactory _stripeClientFactory;

        public StripeWebhookController(ILogger<StripeWebhookController> logger, ApplicationDbContext db, IBillingService billingService, IOptions<StripeOptions> stripeOptions, IMailer mailer, IOwnerProvisioningService ownerProvisioning, IStripeClientFactory stripeClientFactory) {
            _logger = logger;
            _db = db;
            _billingService = billingService;
            _stripeOptions = stripeOptions.Value;
            _mailer = mailer;
            _ownerProvisioning = ownerProvisioning;
            _stripeClientFactory = stripeClientFactory;
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

            _logger.LogInformation("Stripe webhook received type={Type} id={Id}", stripeEvent.Type, stripeEvent.Id);

            // Early idempotency check
            var existingLog = await _db.StripeEventLogs.FirstOrDefaultAsync(e => e.EventId == stripeEvent.Id, ct);
            if (existingLog != null && existingLog.Processed) {
                _logger.LogInformation("Stripe event {Id} already processed; skipping", stripeEvent.Id);
                return Ok();
            }
            if (existingLog == null) {
                existingLog = new StripeEventLog { EventId = stripeEvent.Id, Type = stripeEvent.Type, RawJson = json, ReceivedAt = DateTime.UtcNow, Processed = false };
                _db.StripeEventLogs.Add(existingLog);
                await _db.SaveChangesAsync(ct);
            }

            try {
                var strategy = _db.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () => {
                    await using var tx = await _db.Database.BeginTransactionAsync(ct);
                    switch (stripeEvent.Type) {
                        case "checkout.session.completed":
                        case "checkout.session.async_payment_succeeded":
                            await HandleCheckoutSessionCompletedAsync(stripeEvent, ct);
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
                            _logger.LogInformation("Unhandled Stripe event type {Type}; ignoring", stripeEvent.Type);
                            break;
                    }
                    existingLog.Processed = true;
                    await _db.SaveChangesAsync(ct);
                    await tx.CommitAsync(ct);
                });
            } catch (Exception ex) {
                existingLog.Error = ex.Message;
                await _db.SaveChangesAsync(ct);
                _logger.LogError(ex, "Error handling Stripe event {Id}", stripeEvent.Id);
            }

            return Ok();
        }

        private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent, CancellationToken ct) {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;
            _logger.LogInformation("Checkout session completed {SessionId} customer {CustomerId}", session.Id, session.CustomerId);

            // Prefer session metadata; fallback to subscription metadata if needed
            IReadOnlyDictionary<string, string> meta = session.Metadata ?? new Dictionary<string, string>();
            Subscription? sub = null;
            if ((meta == null || meta.Count == 0) && !string.IsNullOrWhiteSpace(session.SubscriptionId)) {
                try {
                    var client = _stripeClientFactory.CreateClient();
                    var subSvc = new SubscriptionService(client);
                    sub = await subSvc.GetAsync(session.SubscriptionId, options: null, requestOptions: null, cancellationToken: ct);
                    if (sub?.Metadata != null && sub.Metadata.Count > 0) meta = sub.Metadata;
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Unable to fetch subscription {SubId} for metadata fallback", session.SubscriptionId);
                }
            }

            if (meta != null && meta.Count > 0) {
                _logger.LogInformation("Session/subscription metadata keys: {Keys}", string.Join(",", meta.Keys));
            } else {
                _logger.LogWarning("No metadata available on session or subscription for {SessionId}", session.Id);
            }

            var isDeferred = meta != null && meta.TryGetValue("deferred_tenant", out var def) && string.Equals(def, "true", StringComparison.OrdinalIgnoreCase);
            if (isDeferred) {
                var companyName = meta.TryGetValue("company_name", out var cn) ? cn : null;
                var subdomain = meta.TryGetValue("subdomain", out var sd) ? sd : null;
                var adminEmail = meta.TryGetValue("admin_email", out var ae) ? ae : null;
                var tierStr = meta.TryGetValue("tier", out var tr) ? tr : null;
                _logger.LogInformation("Deferred flow metadata company={Company} subdomain={Subdomain} admin={Admin} tier={Tier}", companyName, subdomain, adminEmail, tierStr);
                if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(subdomain)) {
                    _logger.LogWarning("Deferred checkout missing required metadata. company={Company} subdomain={Subdomain}", companyName, subdomain);
                    return;
                }
                var existing = await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
                if (existing == null) {
                    CRS.Models.SubscriptionTier? tier = null;
                    if (Enum.TryParse<CRS.Models.SubscriptionTier>(tierStr, true, out var parsedTier)) tier = parsedTier;
                    var tenant = new CRS.Models.Tenant {
                        Name = companyName,
                        Subdomain = subdomain,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        ProvisioningStatus = CRS.Models.TenantProvisioningStatus.Active,
                        SubscriptionStatus = CRS.Models.SubscriptionStatus.Incomplete,
                        PendingOwnerEmail = adminEmail,
                        StripeCustomerId = session.CustomerId,
                        LastStripeCheckoutSessionId = session.Id,
                        Tier = tier
                    };
                    _db.Tenants.Add(tenant);
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Deferred tenant created {TenantId} for subdomain {Subdomain}", tenant.Id, subdomain);
                    await _ownerProvisioning.ProvisionAsync(tenant, adminEmail, ct);
                } else {
                    if (string.IsNullOrWhiteSpace(existing.StripeCustomerId) && !string.IsNullOrWhiteSpace(session.CustomerId)) {
                        existing.StripeCustomerId = session.CustomerId;
                        await _db.SaveChangesAsync(ct);
                    }
                    await _ownerProvisioning.ProvisionAsync(existing, adminEmail, ct);
                }
                return;
            }

            // legacy path
            var tenantIdMeta = session.Metadata != null && session.Metadata.TryGetValue("tenant_id", out var tid) ? tid : null;
            var adminEmailMeta = session.Metadata != null && session.Metadata.TryGetValue("admin_email", out var aem) ? aem : null;
            if (int.TryParse(tenantIdMeta, out var tenantId)) {
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
                if (tenant != null) {
                    tenant.StripeCustomerId ??= session.CustomerId;
                    if (!string.IsNullOrWhiteSpace(adminEmailMeta) && string.IsNullOrWhiteSpace(tenant.PendingOwnerEmail)) tenant.PendingOwnerEmail = adminEmailMeta.Trim();
                    tenant.ProvisioningStatus = CRS.Models.TenantProvisioningStatus.Active;
                    tenant.SubscriptionStatus = CRS.Models.SubscriptionStatus.Incomplete;
                    tenant.IsActive = true;
                    tenant.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Activated tenant {TenantId} after checkout session, pending owner {Email}", tenantId, tenant.PendingOwnerEmail);
                    await _ownerProvisioning.ProvisionAsync(tenant, adminEmailMeta, ct);
                } else {
                    _logger.LogWarning("Legacy path: tenant id {TenantId} not found", tenantId);
                }
            } else {
                _logger.LogInformation("Legacy path not taken: no tenant_id metadata on session");
            }
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

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == subscription.CustomerId || t.StripeSubscriptionId == subscription.Id, ct);
            if (tenant == null && subscription.Metadata != null && subscription.Metadata.TryGetValue("deferred_tenant", out var def) && def == "true") {
                // Subscription event arrived before checkout.session.completed; create tenant now
                subscription.Metadata.TryGetValue("company_name", out var companyName);
                subscription.Metadata.TryGetValue("subdomain", out var subdomain);
                subscription.Metadata.TryGetValue("admin_email", out var adminEmail);
                if (!string.IsNullOrWhiteSpace(companyName) && !string.IsNullOrWhiteSpace(subdomain)) {
                    var existing = await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
                    if (existing == null) {
                        var newTenant = new CRS.Models.Tenant {
                            Name = companyName,
                            Subdomain = subdomain,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            ProvisioningStatus = CRS.Models.TenantProvisioningStatus.Active,
                            SubscriptionStatus = CRS.Models.SubscriptionStatus.Incomplete,
                            PendingOwnerEmail = adminEmail,
                            StripeCustomerId = subscription.CustomerId,
                            StripeSubscriptionId = subscription.Id,
                            Tier = tier
                        };
                        _db.Tenants.Add(newTenant);
                        await _db.SaveChangesAsync(ct);
                        _logger.LogInformation("Tenant created from subscription.updated {TenantId} for subdomain {Subdomain}", newTenant.Id, subdomain);
                        await _ownerProvisioning.ProvisionAsync(newTenant, adminEmail, ct);
                        tenant = newTenant; // continue applying update below
                    } else {
                        tenant = existing;
                        await _ownerProvisioning.ProvisionAsync(existing, adminEmail, ct);
                    }
                }
            }

            if (tenant != null) {
                await _billingService.ApplySubscriptionUpdateAsync(subscription.Id, subscription.CustomerId!, tier, status, ct);
            }
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
