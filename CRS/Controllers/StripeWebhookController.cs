using CRS.Data;
using CRS.Models.Billing;
using CRS.Services.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Services.Email;
using CRS.Services.Provisioning;
using CRS.Models;
using CRS.Models.Email;
using CRS.Models.Emails;
using CRS.Services.Interfaces;

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
        private readonly IInvoicePaymentService _invoicePaymentService;

        public StripeWebhookController(ILogger<StripeWebhookController> logger, ApplicationDbContext db, IBillingService billingService, IOptions<StripeOptions> stripeOptions, IMailer mailer, IOwnerProvisioningService ownerProvisioning, IStripeClientFactory stripeClientFactory, IInvoicePaymentService invoicePaymentService) {
            _logger = logger;
            _db = db;
            _billingService = billingService;
            _stripeOptions = stripeOptions.Value;
            _mailer = mailer;
            _ownerProvisioning = ownerProvisioning;
            _stripeClientFactory = stripeClientFactory;
            _invoicePaymentService = invoicePaymentService;
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
                        case "customer.subscription.trial_will_end":
                            await HandleTrialWillEndAsync(stripeEvent, ct);
                            break;
                        case "invoice.payment_succeeded":
                            await HandleInvoicePaymentAsync(stripeEvent, true, ct);
                            break;
                        case "invoice.payment_failed":
                            await HandleInvoicePaymentAsync(stripeEvent, false, ct);
                            break;
                        case "invoice.upcoming":
                            await HandleInvoiceUpcomingAsync(stripeEvent, ct);
                            break;
                        case "payment_intent.succeeded":
                            await HandlePaymentIntentSucceededAsync(stripeEvent, ct);
                            break;
                        case "charge.dispute.created":
                            await HandleChargeDisputeCreatedAsync(stripeEvent, ct);
                            break;
                        // Stripe Connect account events
                        case "account.updated":
                            await HandleConnectAccountUpdatedAsync(stripeEvent, ct);
                            break;
                        case "account.application.deauthorized":
                            await HandleConnectAccountDeauthorizedAsync(stripeEvent, ct);
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
            if (session == null) {
                _logger.LogWarning("Checkout session is null for event {EventId}", stripeEvent.Id);
                return;
            }
            _logger.LogInformation("Checkout session completed {SessionId} customer {CustomerId} subscription {SubscriptionId}", 
                session.Id, session.CustomerId, session.SubscriptionId);

            // Prefer session metadata; fallback to subscription metadata if needed
            IReadOnlyDictionary<string, string>? meta = session.Metadata;
            
            // Log raw metadata state
            if (meta != null && meta.Count > 0) {
                _logger.LogInformation("Session metadata found with {Count} keys: {Keys}", 
                    meta.Count, string.Join(", ", meta.Keys));
                // Log all metadata values for debugging
                foreach (var kvp in meta) {
                    _logger.LogInformation("  Metadata[{Key}] = {Value}", kvp.Key, kvp.Value);
                }
            } else {
                _logger.LogWarning("Session metadata is null or empty for {SessionId}, attempting subscription fallback", session.Id);
            }
            
            Subscription? sub = null;
            if ((meta == null || meta.Count == 0) && !string.IsNullOrWhiteSpace(session.SubscriptionId)) {
                try {
                    _logger.LogInformation("Fetching subscription {SubId} metadata as fallback", session.SubscriptionId);
                    var client = _stripeClientFactory.CreateClient();
                    var subSvc = new SubscriptionService(client);
                    sub = await subSvc.GetAsync(session.SubscriptionId, options: null, requestOptions: null, cancellationToken: ct);
                    if (sub?.Metadata != null && sub.Metadata.Count > 0) {
                        meta = sub.Metadata;
                        _logger.LogInformation("Using subscription metadata with {Count} keys: {Keys}", 
                            meta.Count, string.Join(", ", meta.Keys));
                        foreach (var kvp in meta) {
                            _logger.LogInformation("  SubMetadata[{Key}] = {Value}", kvp.Key, kvp.Value);
                        }
                    } else {
                        _logger.LogWarning("Subscription {SubId} also has no metadata", session.SubscriptionId);
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Unable to fetch subscription {SubId} for metadata fallback", session.SubscriptionId);
                }
            }

            // Check if this is a deferred tenant creation (new signup flow)
            var isDeferred = meta != null && meta.TryGetValue("deferred_tenant", out var def) && string.Equals(def, "true", StringComparison.OrdinalIgnoreCase);
            var deferredValue = meta?.TryGetValue("deferred_tenant", out var dv) == true ? dv : "not found";
            _logger.LogInformation("Checking deferred flag: meta is null={MetaNull}, has deferred_tenant={HasFlag}, value={Value}, isDeferred={IsDeferred}", 
                meta == null, meta?.ContainsKey("deferred_tenant") ?? false, deferredValue, isDeferred);
            
            if (isDeferred) {
                var companyName = meta!.TryGetValue("company_name", out var cn) ? cn : null;
                var subdomain = meta.TryGetValue("subdomain", out var sd) ? sd : null;
                var adminEmail = meta.TryGetValue("admin_email", out var ae) ? ae : null;
                var tierStr = meta.TryGetValue("tier", out var tr) ? tr : null;
                _logger.LogInformation("Deferred tenant creation: company={Company} subdomain={Subdomain} admin={Admin} tier={Tier}", 
                    companyName, subdomain, adminEmail, tierStr);
                
                if (string.IsNullOrWhiteSpace(companyName) || string.IsNullOrWhiteSpace(subdomain)) {
                    var error = $"Deferred checkout missing required metadata. company='{companyName}' subdomain='{subdomain}'";
                    _logger.LogError(error);
                    throw new InvalidOperationException(error);
                }
                
                try {
                    var existing = await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
                    if (existing == null) {
                        _logger.LogInformation("Creating new tenant for subdomain {Subdomain}", subdomain);
                        CRS.Models.SubscriptionTier? tier = null;
                        if (!string.IsNullOrWhiteSpace(tierStr) && Enum.TryParse<CRS.Models.SubscriptionTier>(tierStr, true, out var parsedTier)) {
                            tier = parsedTier;
                        } else {
                            _logger.LogWarning("Could not parse tier '{Tier}', defaulting to null", tierStr);
                        }
                        
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
                        _logger.LogInformation("Successfully created tenant {TenantId} for subdomain {Subdomain}", tenant.Id, subdomain);
                        
                        // Provision owner account
                        _logger.LogInformation("Provisioning owner account for {Email}", adminEmail);
                        var provisionResult = await _ownerProvisioning.ProvisionAsync(tenant, adminEmail, ct);
                        _logger.LogInformation("Owner provisioning result: {Result}", provisionResult);
                    } else {
                        _logger.LogInformation("Tenant {TenantId} already exists for subdomain {Subdomain}, updating Stripe customer ID", existing.Id, subdomain);
                        if (string.IsNullOrWhiteSpace(existing.StripeCustomerId) && !string.IsNullOrWhiteSpace(session.CustomerId)) {
                            existing.StripeCustomerId = session.CustomerId;
                            existing.LastStripeCheckoutSessionId = session.Id;
                            await _db.SaveChangesAsync(ct);
                            _logger.LogInformation("Updated existing tenant {TenantId} with Stripe customer {CustomerId}", existing.Id, session.CustomerId);
                        }
                        var provisionResult = await _ownerProvisioning.ProvisionAsync(existing, adminEmail, ct);
                        _logger.LogInformation("Owner provisioning result for existing tenant: {Result}", provisionResult);
                    }
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to create/update tenant for subdomain {Subdomain}", subdomain);
                    throw; // Re-throw to be caught by outer try-catch
                }
                return;
            } else {
                _logger.LogInformation("Not a deferred tenant creation, checking legacy path");
            }

            // legacy path (for existing tenant upgrades/downgrades)
            var tenantIdMeta = meta != null && meta.TryGetValue("tenant_id", out var tid) ? tid : null;
            var adminEmailMeta = meta != null && meta.TryGetValue("admin_email", out var aem) ? aem : null;
            
            _logger.LogInformation("Checking legacy path: tenantIdMeta={TenantIdMeta}, canParse={CanParse}", 
                tenantIdMeta, int.TryParse(tenantIdMeta, out _));
            
            if (int.TryParse(tenantIdMeta, out var tenantId)) {
                _logger.LogInformation("Legacy path: activating existing tenant {TenantId}", tenantId);
                var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
                if (tenant != null) {
                    tenant.StripeCustomerId ??= session.CustomerId;
                    tenant.LastStripeCheckoutSessionId = session.Id;
                    if (!string.IsNullOrWhiteSpace(adminEmailMeta) && string.IsNullOrWhiteSpace(tenant.PendingOwnerEmail)) {
                        tenant.PendingOwnerEmail = adminEmailMeta.Trim();
                    }
                    tenant.ProvisioningStatus = CRS.Models.TenantProvisioningStatus.Active;
                    tenant.SubscriptionStatus = CRS.Models.SubscriptionStatus.Incomplete;
                    tenant.IsActive = true;
                    tenant.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync(ct);
                    _logger.LogInformation("Activated existing tenant {TenantId}, pending owner {Email}", tenantId, tenant.PendingOwnerEmail);
                    var provisionResult = await _ownerProvisioning.ProvisionAsync(tenant, adminEmailMeta, ct);
                    _logger.LogInformation("Legacy path owner provisioning result: {Result}", provisionResult);
                } else {
                    var error = $"Legacy path: tenant ID {tenantId} not found in database";
                    _logger.LogError(error);
                    throw new InvalidOperationException(error);
                }
            } else {
                var error = $"No valid tenant identifier found in metadata. Session has neither 'deferred_tenant=true' nor 'tenant_id'. Available keys: {(meta != null ? string.Join(", ", meta.Keys) : "none")}";
                _logger.LogError(error);
                throw new InvalidOperationException(error);
            }
        }

        private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent, CancellationToken ct) {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null) {
                _logger.LogWarning("Subscription is null for event {EventId}", stripeEvent.Id);
                return;
            }
            
            _logger.LogInformation("Subscription event: id={SubId} customer={CustomerId} status={Status}", 
                subscription.Id, subscription.CustomerId, subscription.Status);
            
            var status = MapStatus(subscription.Status);
            CRS.Models.SubscriptionTier? tier = null;
            var priceId = subscription.Items.Data.FirstOrDefault()?.Price?.Id;
            _logger.LogInformation("Subscription price ID: {PriceId}", priceId);
            
            if (priceId != null) {
                if (priceId == _stripeOptions.StarterMonthlyPriceId || priceId == _stripeOptions.StarterYearlyPriceId || priceId == _stripeOptions.StartupPriceId) tier = CRS.Models.SubscriptionTier.Startup;
                else if (priceId == _stripeOptions.ProMonthlyPriceId || priceId == _stripeOptions.ProYearlyPriceId || priceId == _stripeOptions.ProPriceId) tier = CRS.Models.SubscriptionTier.Pro;
                else if (priceId == _stripeOptions.EnterpriseMonthlyPriceId || priceId == _stripeOptions.EnterpriseYearlyPriceId || priceId == _stripeOptions.EnterprisePriceId) tier = CRS.Models.SubscriptionTier.Enterprise;
                _logger.LogInformation("Mapped price to tier: {Tier}", tier);
            }

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == subscription.CustomerId || t.StripeSubscriptionId == subscription.Id, ct);
            
            if (tenant == null) {
                _logger.LogInformation("No existing tenant found for customer {CustomerId} or subscription {SubId}, checking for deferred creation", 
                    subscription.CustomerId, subscription.Id);
                
                // Log subscription metadata
                if (subscription.Metadata != null && subscription.Metadata.Count > 0) {
                    _logger.LogInformation("Subscription metadata keys: {Keys}", string.Join(", ", subscription.Metadata.Keys));
                    foreach (var kvp in subscription.Metadata) {
                        _logger.LogInformation("  SubMetadata[{Key}] = {Value}", kvp.Key, kvp.Value);
                    }
                } else {
                    _logger.LogWarning("Subscription has no metadata");
                }
                
                if (subscription.Metadata != null && subscription.Metadata.TryGetValue("deferred_tenant", out var def) && def == "true") {
                    _logger.LogInformation("Deferred tenant flag found in subscription, attempting creation");
                    // Subscription event arrived before checkout.session.completed; create tenant now
                    subscription.Metadata.TryGetValue("company_name", out var companyName);
                    subscription.Metadata.TryGetValue("subdomain", out var subdomain);
                    subscription.Metadata.TryGetValue("admin_email", out var adminEmail);
                    
                    _logger.LogInformation("Subscription deferred metadata: company={Company} subdomain={Subdomain} admin={Admin}", 
                        companyName, subdomain, adminEmail);
                    
                    if (!string.IsNullOrWhiteSpace(companyName) && !string.IsNullOrWhiteSpace(subdomain)) {
                        var existing = await _db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);
                        if (existing == null) {
                            _logger.LogInformation("Creating new tenant from subscription event for subdomain {Subdomain}", subdomain);
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
                            _logger.LogInformation("Successfully created tenant {TenantId} from subscription for subdomain {Subdomain}", newTenant.Id, subdomain);
                            var provisionResult = await _ownerProvisioning.ProvisionAsync(newTenant, adminEmail, ct);
                            _logger.LogInformation("Owner provisioning result from subscription: {Result}", provisionResult);
                            tenant = newTenant; // continue applying update below
                        } else {
                            _logger.LogInformation("Tenant {TenantId} already exists for subdomain {Subdomain}", existing.Id, subdomain);
                            tenant = existing;
                            var provisionResult = await _ownerProvisioning.ProvisionAsync(existing, adminEmail, ct);
                            _logger.LogInformation("Owner provisioning result for existing tenant from subscription: {Result}", provisionResult);
                        }
                    } else {
                        var error = $"Subscription deferred creation missing required metadata: company='{companyName}' subdomain='{subdomain}'";
                        _logger.LogError(error);
                        throw new InvalidOperationException(error);
                    }
                } else {
                    _logger.LogWarning("No deferred_tenant flag in subscription metadata, cannot create tenant");
                }
            } else {
                _logger.LogInformation("Found existing tenant {TenantId} for subscription, applying update", tenant.Id);
            }

            if (tenant != null) {
                _logger.LogInformation("Applying subscription update to tenant {TenantId}: status={Status} tier={Tier}", tenant.Id, status, tier);
                await _billingService.ApplySubscriptionUpdateAsync(subscription.Id, subscription.CustomerId!, tier, status, ct);
            } else {
                _logger.LogWarning("Cannot apply subscription update - no tenant found and deferred creation failed");
            }
        }

        private async Task HandleSubscriptionDeletedAsync(Event stripeEvent, CancellationToken ct) {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null) {
                _logger.LogWarning("Subscription is null for event {EventId}", stripeEvent.Id);
                return;
            }
            
            var tenant = await _db.Tenants.FirstOrDefaultAsync(
                t => t.StripeSubscriptionId == subscription.Id || t.StripeCustomerId == subscription.CustomerId, ct);
            
            if (tenant == null) {
                _logger.LogWarning("No tenant found for subscription {SubId} or customer {CustomerId}", 
                    subscription.Id, subscription.CustomerId);
                return;
            }
            
            // Subscription canceled/deleted - enter suspended state
            tenant.SubscriptionStatus = SubscriptionStatus.Suspended;
            tenant.SubscriptionCanceledAt = DateTime.UtcNow;
            tenant.SuspendedAt = DateTime.UtcNow;
            tenant.IsActive = false; // No access to application
            tenant.UpdatedAt = DateTime.UtcNow;
            
            // Schedule grace period end (30 days from now for data retention)
            tenant.GracePeriodEndsAt = DateTime.UtcNow.AddDays(30);
            
            _logger.LogInformation("Subscription deleted for tenant {TenantId} - entering Suspended state. Data will be retained until {GracePeriodEnd}", 
                tenant.Id, tenant.GracePeriodEndsAt);

            // Send suspension notification email
            await SendBillingNotificationAsync(tenant, BillingNotificationType.AccountSuspended);

                await _db.SaveChangesAsync(ct);
            }

            private async Task HandleInvoicePaymentAsync(Event stripeEvent, bool succeeded, CancellationToken ct) {
                var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                if (invoice == null) {
                    _logger.LogWarning("Invoice is null for event {EventId}", stripeEvent.Id);
                    return;
                }
            
            var customerId = invoice.CustomerId;
            if (string.IsNullOrWhiteSpace(customerId)) {
                _logger.LogWarning("Invoice has no customer ID for event {EventId}", stripeEvent.Id);
                return;
            }
            
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == customerId, ct);
            if (tenant == null) {
                _logger.LogWarning("No tenant found for Stripe customer {CustomerId}", customerId);
                return;
            }
            
            if (succeeded) {
                // Payment successful - restore full access
                var wasInactive = !tenant.IsActive || 
                    tenant.SubscriptionStatus == SubscriptionStatus.PastDue ||
                    tenant.SubscriptionStatus == SubscriptionStatus.GracePeriod ||
                    tenant.SubscriptionStatus == SubscriptionStatus.Suspended;
                
                tenant.SubscriptionStatus = SubscriptionStatus.Active;
                tenant.IsActive = true;
                tenant.SuspendedAt = null;
                tenant.GracePeriodEndsAt = null;
                tenant.LastPaymentFailureAt = null;
                tenant.UpdatedAt = DateTime.UtcNow;
                
                if (wasInactive) {
                    tenant.ReactivationCount++;
                    tenant.LastReactivatedAt = DateTime.UtcNow;
                    _logger.LogInformation("Payment succeeded for tenant {TenantId} - account reactivated (reactivation #{Count})", 
                        tenant.Id, tenant.ReactivationCount);

                    // Send reactivation success email
                    await SendBillingNotificationAsync(tenant, BillingNotificationType.AccountReactivated);
                } else {
                    _logger.LogInformation("Payment succeeded for tenant {TenantId}", tenant.Id);
                }
            } else {
                // Payment failed
                tenant.LastPaymentFailureAt = DateTime.UtcNow;
                tenant.UpdatedAt = DateTime.UtcNow;
                
                if (tenant.SubscriptionStatus != SubscriptionStatus.PastDue) {
                    // First payment failure - enter PastDue state
                    tenant.SubscriptionStatus = SubscriptionStatus.PastDue;
                    tenant.IsActive = true; // Keep active during Stripe's automatic retry period (7 days)
                    _logger.LogWarning("Payment failed for tenant {TenantId} - entering PastDue status. Stripe will retry automatically.", tenant.Id);

                    // Send payment failed notification email
                    await SendBillingNotificationAsync(tenant, BillingNotificationType.PaymentFailed, invoice.AmountDue / 100m);
                } else {
                    _logger.LogWarning("Payment failed again for tenant {TenantId} (already in PastDue)", tenant.Id);
                }
            }
            
                        await _db.SaveChangesAsync(ct);
                    }

                    /// <summary>
                    /// Handles payment_intent.succeeded events for invoice payments.
                    /// </summary>
                    private async Task HandlePaymentIntentSucceededAsync(Event stripeEvent, CancellationToken ct) {
                        var paymentIntent = stripeEvent.Data.Object as Stripe.PaymentIntent;
                        if (paymentIntent == null) {
                            _logger.LogWarning("PaymentIntent is null for event {EventId}", stripeEvent.Id);
                            return;
                        }

                        // Check if this payment is for an invoice (has invoice_id in metadata)
                        if (!paymentIntent.Metadata.TryGetValue("invoice_id", out var invoiceIdStr)) {
                            _logger.LogInformation("PaymentIntent {PaymentIntentId} has no invoice_id metadata, skipping", paymentIntent.Id);
                            return;
                        }

                        if (!Guid.TryParse(invoiceIdStr, out var invoiceId)) {
                                            _logger.LogWarning("PaymentIntent {PaymentIntentId} has invalid invoice_id: {InvoiceId}", paymentIntent.Id, invoiceIdStr);
                                            return;
                                        }

                                        _logger.LogInformation("Processing invoice payment for invoice {InvoiceId} amount {Amount}", invoiceId, paymentIntent.Amount);

                                        // Find the invoice by ID and update payment
                                        var invoice = await _db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct);
                                        if (invoice == null) {
                                            _logger.LogWarning("Invoice {InvoiceId} not found for payment intent {PaymentIntentId}", invoiceId, paymentIntent.Id);
                                            return;
                                        }

                                        var amountPaid = paymentIntent.Amount / 100m; // Convert from cents

                                        // Create payment record for audit trail
                                        var paymentRecord = new CRS.Models.PaymentRecord
                                        {
                                            TenantId = invoice.TenantId,
                                            InvoiceId = invoiceId,
                                            Amount = amountPaid,
                                            PaymentDate = DateTime.UtcNow,
                                            PaymentMethod = "Stripe",
                                            ReferenceNumber = paymentIntent.Id,
                                            StripePaymentIntentId = paymentIntent.Id,
                                            IsAutomatic = true,
                                            Notes = "Payment via Stripe Checkout"
                                        };
                                        _db.PaymentRecords.Add(paymentRecord);

                                        invoice.AmountPaid += amountPaid;
                                        invoice.StripePaymentIntentId = paymentIntent.Id;
                                        invoice.PaymentMethod = "Stripe";
                                        invoice.PaymentReference = paymentIntent.Id;
                                        invoice.DateModified = DateTime.UtcNow;

                                        // Update status based on payment
                                        if (invoice.AmountPaid >= invoice.TotalAmount) {
                                            invoice.Status = CRS.Models.InvoiceStatus.Paid;
                                        } else if (invoice.AmountPaid > 0) {
                                            invoice.Status = CRS.Models.InvoiceStatus.PartiallyPaid;
                                        }

                                        await _db.SaveChangesAsync(ct);

                                        _logger.LogInformation(
                                            "Recorded Stripe payment for invoice {InvoiceNumber}: {Amount:C} via PaymentIntent {PaymentIntent}",
                                            invoice.InvoiceNumber, amountPaid, paymentIntent.Id);
                                    }

                                    private static CRS.Models.SubscriptionStatus MapStatus(string? stripeStatus) => stripeStatus switch {
                                        "active" => CRS.Models.SubscriptionStatus.Active,
                                        "trialing" => CRS.Models.SubscriptionStatus.Trialing,
                                        "past_due" => CRS.Models.SubscriptionStatus.PastDue,
                                        "canceled" => CRS.Models.SubscriptionStatus.Canceled,
                                        "unpaid" => CRS.Models.SubscriptionStatus.Unpaid,
                                        "incomplete" => CRS.Models.SubscriptionStatus.Incomplete,
                                        "paused" => CRS.Models.SubscriptionStatus.Paused,
                                        _ => CRS.Models.SubscriptionStatus.None
                                    };

        /// <summary>
        /// Handles the customer.subscription.trial_will_end webhook.
        /// Sent 3 days before a trial ends - use to notify customer.
        /// </summary>
        private async Task HandleTrialWillEndAsync(Event stripeEvent, CancellationToken ct) {
            var subscription = stripeEvent.Data.Object as Subscription;
            if (subscription == null) {
                _logger.LogWarning("Subscription is null for trial_will_end event {EventId}", stripeEvent.Id);
                return;
            }

            var tenant = await _db.Tenants.FirstOrDefaultAsync(
                t => t.StripeSubscriptionId == subscription.Id || t.StripeCustomerId == subscription.CustomerId, ct);

            if (tenant == null) {
                _logger.LogWarning("No tenant found for subscription {SubId} trial ending", subscription.Id);
                return;
            }

            _logger.LogInformation("Trial ending in 3 days for tenant {TenantId} ({Name}), subscription {SubId}",
                tenant.Id, tenant.Name, subscription.Id);

            // Send trial ending notification
            await SendBillingNotificationAsync(tenant, BillingNotificationType.TrialEnding);
        }

        /// <summary>
        /// Handles the invoice.upcoming webhook.
        /// Sent ~3 days before the next invoice is created - useful for notifying customers.
        /// </summary>
        private async Task HandleInvoiceUpcomingAsync(Event stripeEvent, CancellationToken ct) {
            var invoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (invoice == null) {
                _logger.LogWarning("Invoice is null for invoice.upcoming event {EventId}", stripeEvent.Id);
                return;
            }

            var customerId = invoice.CustomerId;
            if (string.IsNullOrWhiteSpace(customerId)) {
                _logger.LogWarning("Invoice.upcoming has no customer ID for event {EventId}", stripeEvent.Id);
                return;
            }

            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == customerId, ct);
            if (tenant == null) {
                _logger.LogWarning("No tenant found for Stripe customer {CustomerId} (upcoming invoice)", customerId);
                return;
            }

            var amountDue = invoice.AmountDue / 100m; // Convert cents to dollars
            _logger.LogInformation("Upcoming invoice for tenant {TenantId} ({Name}): {Amount:C} due on {DueDate}",
                tenant.Id, tenant.Name, amountDue, invoice.DueDate);

            // Optionally send upcoming invoice notification
            // await SendBillingNotificationAsync(tenant, BillingNotificationType.UpcomingInvoice, amountDue);
        }

        /// <summary>
        /// Handles the charge.dispute.created webhook.
        /// Sent when a customer disputes a charge (chargeback) - critical to handle promptly.
        /// </summary>
        private async Task HandleChargeDisputeCreatedAsync(Event stripeEvent, CancellationToken ct) {
            var dispute = stripeEvent.Data.Object as Stripe.Dispute;
            if (dispute == null) {
                _logger.LogWarning("Dispute is null for charge.dispute.created event {EventId}", stripeEvent.Id);
                return;
            }

            // Get the charge to find the customer
            var chargeId = dispute.ChargeId;
            string? customerId = null;

            if (!string.IsNullOrWhiteSpace(chargeId)) {
                try {
                    var client = _stripeClientFactory.CreateClient();
                    var chargeService = new ChargeService(client);
                    var charge = await chargeService.GetAsync(chargeId, cancellationToken: ct);
                    customerId = charge?.CustomerId;
                } catch (Exception ex) {
                    _logger.LogError(ex, "Failed to fetch charge {ChargeId} for dispute", chargeId);
                }
            }

            Tenant? tenant = null;
            if (!string.IsNullOrWhiteSpace(customerId)) {
                tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeCustomerId == customerId, ct);
            }

            var disputeAmount = dispute.Amount / 100m;

            _logger.LogCritical(
                "CHARGEBACK DISPUTE CREATED: Dispute {DisputeId} for {Amount:C} - Tenant: {TenantName} ({TenantId}), Charge: {ChargeId}, Reason: {Reason}, Status: {Status}",
                dispute.Id, disputeAmount, tenant?.Name ?? "UNKNOWN", tenant?.Id, chargeId, dispute.Reason, dispute.Status);

            // If tenant found, mark for review
            if (tenant != null) {
                // You might want to add a flag to the tenant for disputes pending review
                // tenant.HasPendingDispute = true;
                // await _db.SaveChangesAsync(ct);

                // Send critical notification to admin
                await SendBillingNotificationAsync(tenant, BillingNotificationType.DisputeCreated, disputeAmount);
            }
        }

        /// <summary>
        /// Sends a billing notification email to the tenant owner.
        /// </summary>
        private async Task SendBillingNotificationAsync(
            Tenant tenant, 
            BillingNotificationType notificationType, 
            decimal? amountDue = null)
        {
            try
            {
                // Get owner email - prefer OwnerId lookup, fallback to PendingOwnerEmail
                string? ownerEmail = null;
                string? ownerName = null;

                if (!string.IsNullOrEmpty(tenant.OwnerId) && Guid.TryParse(tenant.OwnerId, out var ownerId))
                {
                    var owner = await _db.Users.FirstOrDefaultAsync(u => u.Id == ownerId);
                    if (owner != null)
                    {
                        ownerEmail = owner.Email;
                        ownerName = owner.FullName;
                    }
                }

                ownerEmail ??= tenant.PendingOwnerEmail;

                if (string.IsNullOrEmpty(ownerEmail))
                {
                    _logger.LogWarning("Cannot send billing notification for tenant {TenantId}: no owner email found", tenant.Id);
                    return;
                }

                // Build billing portal URL
                var billingPortalUrl = !string.IsNullOrEmpty(tenant.StripeCustomerId)
                    ? $"https://{tenant.Subdomain}.reservecloud.com/account/billing"
                    : null;

                var dashboardUrl = $"https://{tenant.Subdomain}.reservecloud.com";

                var email = new BillingNotificationEmail
                {
                    NotificationType = notificationType,
                    TenantName = tenant.Name,
                    OwnerEmail = ownerEmail,
                    OwnerName = ownerName,
                    PlanName = tenant.Tier?.ToString(),
                    AmountDue = amountDue,
                    GracePeriodEndsAt = tenant.GracePeriodEndsAt,
                    SuspendedAt = tenant.SuspendedAt,
                    ReactivatedAt = tenant.LastReactivatedAt,
                    ReactivationCount = tenant.ReactivationCount,
                    UpdatePaymentUrl = billingPortalUrl,
                    BillingPortalUrl = billingPortalUrl,
                    DashboardUrl = dashboardUrl
                };

                var mailable = new BillingNotificationMailable(email);
                await _mailer.SendAsync(mailable);

                _logger.LogInformation(
                    "Sent {NotificationType} email to {Email} for tenant {TenantId}",
                    notificationType, ownerEmail, tenant.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Failed to send {NotificationType} email for tenant {TenantId}", 
                    notificationType, tenant.Id);
                // Don't rethrow - billing notification failure shouldn't break webhook processing
            }
        }

        /// <summary>
        /// Handles account.updated webhook for Stripe Connect accounts.
        /// Syncs the connected account status (onboarding complete, payouts enabled, etc.)
        /// </summary>
        private async Task HandleConnectAccountUpdatedAsync(Event stripeEvent, CancellationToken ct) {
            var account = stripeEvent.Data.Object as Stripe.Account;
            if (account == null) {
                _logger.LogWarning("Account is null for account.updated event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogInformation(
                "Stripe Connect account updated: {AccountId} payouts={PayoutsEnabled} details_submitted={DetailsSubmitted}",
                account.Id, account.PayoutsEnabled, account.DetailsSubmitted);

            // Find the tenant with this Connect account
            var tenant = await _db.Tenants.FirstOrDefaultAsync(
                t => t.StripeConnectAccountId == account.Id, ct);

            if (tenant == null) {
                _logger.LogWarning("No tenant found for Connect account {AccountId}", account.Id);
                return;
            }

            // Update tenant with current Connect status
            tenant.StripeConnectPayoutsEnabled = account.PayoutsEnabled;
            tenant.StripeConnectCardPaymentsEnabled = account.Capabilities?.CardPayments == "active";

            // Check if onboarding is complete
            var hasCurrentlyDue = account.Requirements?.CurrentlyDue?.Any() ?? false;
            var hasPastDue = account.Requirements?.PastDue?.Any() ?? false;
            var wasOnboardingComplete = tenant.StripeConnectOnboardingComplete;
            tenant.StripeConnectOnboardingComplete = !hasCurrentlyDue && !hasPastDue && account.DetailsSubmitted == true;

            tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;
            tenant.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Updated Connect status for tenant {TenantId}: Onboarding={Onboarding} (was {WasOnboarding}), Payouts={Payouts}, Cards={Cards}",
                tenant.Id, tenant.StripeConnectOnboardingComplete, wasOnboardingComplete,
                tenant.StripeConnectPayoutsEnabled, tenant.StripeConnectCardPaymentsEnabled);

            // If onboarding just completed, we could send a notification
            if (tenant.StripeConnectOnboardingComplete && !wasOnboardingComplete) {
                _logger.LogInformation(
                    "Stripe Connect onboarding completed for tenant {TenantId} ({TenantName})",
                    tenant.Id, tenant.Name);
                // TODO: Send email notification that they can now accept payments
            }
        }

        /// <summary>
        /// Handles account.application.deauthorized webhook.
        /// This is called when a connected account disconnects from your platform.
        /// </summary>
        private async Task HandleConnectAccountDeauthorizedAsync(Event stripeEvent, CancellationToken ct) {
            var account = stripeEvent.Data.Object as Stripe.Account;
            if (account == null) {
                _logger.LogWarning("Account is null for account.application.deauthorized event {EventId}", stripeEvent.Id);
                return;
            }

            _logger.LogWarning("Stripe Connect account deauthorized: {AccountId}", account.Id);

            var tenant = await _db.Tenants.FirstOrDefaultAsync(
                t => t.StripeConnectAccountId == account.Id, ct);

            if (tenant == null) {
                _logger.LogWarning("No tenant found for deauthorized Connect account {AccountId}", account.Id);
                return;
            }

            // Mark the Connect account as deauthorized - tenant can reconnect later
            tenant.StripeConnectOnboardingComplete = false;
            tenant.StripeConnectPayoutsEnabled = false;
            tenant.StripeConnectCardPaymentsEnabled = false;
            tenant.StripeConnectLastSyncedAt = DateTime.UtcNow;
            tenant.UpdatedAt = DateTime.UtcNow;
            // Don't clear the StripeConnectAccountId - keep for reference

            await _db.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Marked Connect account as deauthorized for tenant {TenantId} ({TenantName}). Account ID preserved: {AccountId}",
                tenant.Id, tenant.Name, account.Id);
        }
                                }
                            }
