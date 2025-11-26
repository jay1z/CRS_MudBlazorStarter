using CRS.Data;
using CRS.Models;
using CRS.Services.Billing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRS.Controllers {
    [ApiController]
    [Route("api/signup")] // public unauthenticated signup entry
    public class PublicSignupController : ControllerBase {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IBillingService _billing;
        private readonly ILogger<PublicSignupController> _logger;
        public PublicSignupController(IDbContextFactory<ApplicationDbContext> dbFactory, IBillingService billing, ILogger<PublicSignupController> logger) {
            _dbFactory = dbFactory; _billing = billing; _logger = logger;
        }

        public record StartSignupRequest(string CompanyName, string Subdomain, string AdminEmail, string Tier, string Interval);
        public record StartSignupResponse(string CheckoutUrl, int TenantId, Guid SignupToken);

        [HttpPost("start")] // now only creates Stripe checkout session; tenant deferred until payment
        public async Task<ActionResult<StartSignupResponse>> Start([FromBody] StartSignupRequest request, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.Subdomain) || string.IsNullOrWhiteSpace(request.AdminEmail)) return BadRequest("Missing required fields");
            if (!Enum.TryParse<SubscriptionTier>(request.Tier, true, out var tier)) return BadRequest("Invalid tier");
            if (!Enum.TryParse<Services.Billing.BillingInterval>(request.Interval, true, out var interval)) interval = Services.Billing.BillingInterval.Monthly;

            var sub = request.Subdomain.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(sub, "^[a-z0-9-]{1,63}$")) return BadRequest("Invalid subdomain format");

            // Ensure subdomain not already used by an existing tenant
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            if (await db.Tenants.AnyAsync(t => t.Subdomain == sub, ct)) return Conflict("Subdomain already in use");

            // Create checkout session without tenant; embed metadata for webhook to create tenant later
            var checkoutUrl = await _billing.CreateDeferredTenantCheckoutSessionAsync(request.CompanyName.Trim(), sub, request.AdminEmail.Trim(), tier, interval, ct);
            return Ok(new StartSignupResponse(checkoutUrl, 0, Guid.Empty));
        }

        [HttpDelete("purge-incomplete")] // optional maintenance, query hours=24
        public async Task<IActionResult> Purge([FromQuery] int hours = 24, CancellationToken ct = default) {
            var cutoff = DateTime.UtcNow.AddHours(-Math.Abs(hours));
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var stale = await db.Tenants.Where(t => !t.IsActive && t.SubscriptionStatus == SubscriptionStatus.Incomplete && t.CreatedAt < cutoff).ToListAsync(ct);
            if (stale.Count == 0) return Ok(new { deleted = 0 });
            db.Tenants.RemoveRange(stale);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Purged {Count} stale incomplete tenants", stale.Count);
            return Ok(new { deleted = stale.Count });
        }

        [HttpGet("resolve")] // api/signup/resolve?sessionId=cs_test_...
        public async Task<IActionResult> Resolve([FromQuery] string sessionId, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(sessionId)) return BadRequest("sessionId required");
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.LastStripeCheckoutSessionId == sessionId, ct);
            if (tenant == null) return NotFound();
            return Ok(new {
                tenantId = tenant.Id,
                subdomain = tenant.Subdomain,
                active = tenant.IsActive,
                ownerEmail = tenant.PendingOwnerEmail
            });
        }
    }
}
