using CRS.Data;
using CRS.Models;
using CRS.Services.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace CRS.Controllers {
    [ApiController]
    [Route("api/billing")]
    [Authorize(Policy = "RequireTenantOwner")] // Only tenant owners initiate billing actions
    public class BillingController : ControllerBase {
        private readonly IBillingService _billing;
        private readonly ApplicationDbContext _db;
        private readonly IStripeClientFactory _clientFactory;
        public BillingController(IBillingService billing, ApplicationDbContext db, IStripeClientFactory clientFactory) { _billing = billing; _db = db; _clientFactory = clientFactory; }

        [HttpPost("create-checkout-session")] // body: { tenantId: 1, tier: "Startup", interval: "Monthly" }
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request, CancellationToken ct) {
            if (!Enum.TryParse<SubscriptionTier>(request.Tier, true, out var tier)) return BadRequest("Invalid tier");
            if (!Enum.TryParse<Services.Billing.BillingInterval>(request.Interval, true, out var interval)) interval = Services.Billing.BillingInterval.Monthly;
            var url = await _billing.CreateCheckoutSessionAsync(request.TenantId, tier, interval, ct);
            return Ok(new { checkoutUrl = url });
        }

        [HttpPost("portal-session")] // body: { tenantId: 1 }
        public async Task<IActionResult> CreatePortalSession([FromBody] PortalSessionRequest request, CancellationToken ct) {
            var url = await _billing.CreateBillingPortalSessionAsync(request.TenantId, ct);
            return Ok(new { portalUrl = url });
        }

        [AllowAnonymous]
        [HttpGet("status/{tenantId:int}")]
        public async Task<IActionResult> GetStatus([FromRoute] int tenantId, CancellationToken ct) {
            var tenant = await _db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null) return NotFound();
            var communitiesUsed = await _db.Communities.CountAsync(c => c.TenantId == tenantId, ct);
            
            // Get role IDs for team members (TenantOwner and TenantSpecialist)
            var teamRoleIds = await _db.Roles2
                .AsNoTracking()
                .Where(r => r.Name == "TenantOwner" || r.Name == "TenantSpecialist")
                .Select(r => r.Id)
                .ToListAsync(ct);
            
            // Count distinct users with team member roles
            var specialistsUsed = teamRoleIds.Any() 
                ? await _db.UserRoleAssignments
                    .AsNoTracking()
                    .Where(a => a.TenantId == tenantId && teamRoleIds.Contains(a.RoleId))
                    .Select(a => a.UserId)
                    .Distinct()
                    .CountAsync(ct)
                : 0;

            string? interval = null;
            if (!string.IsNullOrWhiteSpace(tenant.StripeSubscriptionId)) {
                try {
                    var client = _clientFactory.CreateClient();
                    var svc = new SubscriptionService(client);
                    var sub = await svc.GetAsync(tenant.StripeSubscriptionId, null, null, ct);
                    var firstPrice = sub.Items.Data.FirstOrDefault()?.Price;
                    interval = firstPrice?.Recurring?.Interval;
                } catch { }
            }

            var ownerRole = await _db.Roles2.FirstOrDefaultAsync(r => r.Name == "TenantOwner", ct);
            bool ownerExists = false;
            if (ownerRole != null) {
                ownerExists = await _db.UserRoleAssignments.AnyAsync(a => a.TenantId == tenantId && a.RoleId == ownerRole.Id, ct);
            }
            var pendingOwnerEmail = tenant.PendingOwnerEmail;

            return Ok(new {
                tenantId = tenant.Id,
                tier = tenant.Tier?.ToString() ?? "None",
                status = tenant.SubscriptionStatus.ToString(),
                interval,
                maxCommunities = tenant.MaxCommunities,
                communitiesUsed,
                maxSpecialists = tenant.MaxSpecialistUsers,
                specialistsUsed,
                stripeCustomerId = tenant.StripeCustomerId,
                stripeSubscriptionId = tenant.StripeSubscriptionId,
                active = tenant.IsActive,
                pendingOwnerEmail,
                ownerExists,
                subdomain = tenant.Subdomain
            });
        }

        [HttpPost("cancel")] // body: { subscriptionId: "sub_xxx" }
        public async Task<IActionResult> Cancel([FromBody] CancelRequest request, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(request.SubscriptionId)) return BadRequest("subscriptionId is required");
            var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == request.SubscriptionId, ct);
            if (tenant == null) return NotFound("Subscription not found for tenant");

            try {
                var client = _clientFactory.CreateClient();
                var svc = new SubscriptionService(client);
                var sub = await svc.CancelAsync(request.SubscriptionId, null, null, ct);
                // Webhook will update DB; return current data
                return Ok(new { canceled = true, subId = sub.Id, status = sub.Status });
            } catch (StripeException ex) {
                return BadRequest(new { error = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpGet("invoices/{tenantId:int}")]
        public async Task<IActionResult> GetInvoices([FromRoute] int tenantId, CancellationToken ct) {
            try {
                var invoices = await _billing.GetRecentInvoicesAsync(tenantId, 10, ct);
                return Ok(invoices);
            } catch (Exception ex) {
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Pauses a subscription at the end of the current billing period.
        /// </summary>
        [HttpPost("pause")]
        public async Task<IActionResult> PauseSubscription([FromBody] PauseResumeRequest request, CancellationToken ct) {
            var success = await _billing.PauseSubscriptionAsync(request.TenantId, ct);
            if (!success) {
                return BadRequest(new { error = "Failed to pause subscription. Tenant may not have an active subscription." });
            }
            return Ok(new { paused = true, tenantId = request.TenantId });
        }

        /// <summary>
        /// Resumes a paused subscription.
        /// </summary>
        [HttpPost("resume")]
        public async Task<IActionResult> ResumeSubscription([FromBody] PauseResumeRequest request, CancellationToken ct) {
            var success = await _billing.ResumeSubscriptionAsync(request.TenantId, ct);
            if (!success) {
                return BadRequest(new { error = "Failed to resume subscription. Tenant may not have a paused subscription." });
            }
            return Ok(new { resumed = true, tenantId = request.TenantId });
        }

        public record CreateCheckoutSessionRequest(int TenantId, string Tier, string Interval);
        public record PortalSessionRequest(int TenantId);
        public record CancelRequest(string SubscriptionId);
        public record PauseResumeRequest(int TenantId);
    }
}
