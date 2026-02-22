using CRS.Data;
using CRS.Models;
using CRS.Services.Billing;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<ApplicationUser> _userManager;
        
        public PublicSignupController(
            IDbContextFactory<ApplicationDbContext> dbFactory, 
            IBillingService billing, 
            ILogger<PublicSignupController> logger,
            UserManager<ApplicationUser> userManager) {
            _dbFactory = dbFactory;
            _billing = billing;
            _logger = logger;
            _userManager = userManager;
        }

        public record StartSignupRequest(string CompanyName, string Subdomain, string AdminEmail, string Tier, string Interval);
        public record StartSignupResponse(string CheckoutUrl, int TenantId, Guid SignupToken);

        [HttpPost("start")] // now only creates Stripe checkout session; tenant deferred until payment
        public async Task<ActionResult<StartSignupResponse>> Start([FromBody] StartSignupRequest request, CancellationToken ct) {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.Subdomain) || string.IsNullOrWhiteSpace(request.AdminEmail)) {
                _logger.LogWarning("Signup attempt with missing required fields");
                return BadRequest("Missing required fields");
            }

            // Validate tier
            if (!Enum.TryParse<SubscriptionTier>(request.Tier, true, out var tier)) {
                _logger.LogWarning("Signup attempt with invalid tier: {Tier}", request.Tier);
                return BadRequest("Invalid tier");
            }

            // Validate interval
            if (!Enum.TryParse<Services.Billing.BillingInterval>(request.Interval, true, out var interval)) {
                interval = Services.Billing.BillingInterval.Monthly;
            }

            // Normalize subdomain
            var sub = request.Subdomain.Trim().ToLowerInvariant();
            if (!System.Text.RegularExpressions.Regex.IsMatch(sub, "^[a-z0-9-]{1,63}$")) {
                _logger.LogWarning("Signup attempt with invalid subdomain format: {Subdomain}", sub);
                return BadRequest("Invalid subdomain format");
            }

            // Check for reserved subdomains
            var reserved = new[] { "www", "api", "app", "admin", "support", "help", "mail", "ftp", 
                                   "localhost", "staging", "dev", "test", "demo", "platform", "dashboard" };
            if (reserved.Contains(sub)) {
                _logger.LogWarning("Signup attempt with reserved subdomain: {Subdomain}", sub);
                return Conflict($"The subdomain '{sub}' is reserved and cannot be used");
            }

            // CRITICAL: Check if subdomain already exists (prevent duplicates before Stripe)
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var existingTenant = await db.Tenants.AsNoTracking().AnyAsync(t => t.Subdomain == sub, ct);
            if (existingTenant) {
                _logger.LogWarning("Signup attempt with duplicate subdomain: {Subdomain}", sub);
                return Conflict("Subdomain already in use");
            }

            // CRITICAL: Check if email is already registered (prevent duplicate accounts)
            var normalizedEmail = request.AdminEmail.Trim().ToLowerInvariant();
            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
            if (existingUser != null) {
                _logger.LogWarning("Signup attempt with existing email: {Email}", normalizedEmail);
                return Conflict("Email address is already registered. Please use a different email or sign in.");
            }

            // Also check if email is pending in another tenant's owner provisioning
            var pendingTenant = await db.Tenants.AsNoTracking()
                .AnyAsync(t => t.PendingOwnerEmail != null && t.PendingOwnerEmail.ToLower() == normalizedEmail, ct);
            if (pendingTenant) {
                _logger.LogWarning("Signup attempt with pending owner email: {Email}", normalizedEmail);
                return Conflict("This email has a pending registration. Please check your email or contact support.");
            }

            // All validations passed - create Stripe checkout session (includes trial if configured)
            try {
                _logger.LogInformation("Creating Stripe checkout session for subdomain {Subdomain}, email {Email}, tier {Tier}", 
                    sub, normalizedEmail, tier);
                var checkoutUrl = await _billing.CreateDeferredTenantCheckoutSessionAsync(
                    request.CompanyName.Trim(), 
                    sub, 
                    normalizedEmail, 
                    tier, 
                    interval, 
                    ct);

                _logger.LogInformation("Stripe checkout session created successfully for {Subdomain}", sub);
                return Ok(new StartSignupResponse(checkoutUrl, 0, Guid.Empty));
            } catch (Exception ex) {
                _logger.LogError(ex, "Error creating Stripe checkout session for {Subdomain}", sub);
                return StatusCode(500, "Unable to create checkout session. Please try again.");
            }
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
