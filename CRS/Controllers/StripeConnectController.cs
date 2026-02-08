using CRS.Services.Billing;
using CRS.Services.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CRS.Controllers;

/// <summary>
/// API controller for Stripe Connect operations.
/// Handles account creation, onboarding, and status checks for tenants.
/// </summary>
[ApiController]
[Route("api/stripe/connect")]
[Authorize]
public class StripeConnectController : ControllerBase
{
    private readonly IStripeConnectService _connectService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<StripeConnectController> _logger;

    public StripeConnectController(
        IStripeConnectService connectService,
        ITenantContext tenantContext,
        ILogger<StripeConnectController> logger)
    {
        _connectService = connectService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current Stripe Connect status for the tenant.
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == null)
        {
            return BadRequest(new { error = "Tenant context not available" });
        }

        var status = await _connectService.GetAccountStatusAsync(tenantId.Value, ct);
        return Ok(status);
    }

    /// <summary>
    /// Starts the Stripe Connect onboarding process.
    /// Creates an account if one doesn't exist, then returns the onboarding URL.
    /// </summary>
    [HttpPost("onboard")]
    public async Task<IActionResult> StartOnboarding([FromBody] OnboardingRequest request, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == null)
        {
            return BadRequest(new { error = "Tenant context not available" });
        }

        // Build return URLs
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var returnUrl = string.IsNullOrEmpty(request.ReturnUrl) 
            ? $"{baseUrl}/account/payment-settings?onboarding=complete"
            : request.ReturnUrl;
        var refreshUrl = string.IsNullOrEmpty(request.RefreshUrl)
            ? $"{baseUrl}/account/payment-settings?onboarding=refresh"
            : request.RefreshUrl;

        var result = await _connectService.CreateAccountAndGetOnboardingUrlAsync(
            tenantId.Value, returnUrl, refreshUrl, ct);

        if (!result.Success)
        {
            _logger.LogWarning("Failed to start onboarding for tenant {TenantId}: {Error}", 
                tenantId, result.ErrorMessage);
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new
        {
            accountId = result.AccountId,
            onboardingUrl = result.OnboardingUrl
        });
    }

    /// <summary>
    /// Gets a new onboarding URL for an existing account (if user needs to resume onboarding).
    /// </summary>
    [HttpPost("onboard/resume")]
    public async Task<IActionResult> ResumeOnboarding([FromBody] OnboardingRequest request, CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == null)
        {
            return BadRequest(new { error = "Tenant context not available" });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var returnUrl = string.IsNullOrEmpty(request.ReturnUrl)
            ? $"{baseUrl}/account/payment-settings?onboarding=complete"
            : request.ReturnUrl;
        var refreshUrl = string.IsNullOrEmpty(request.RefreshUrl)
            ? $"{baseUrl}/account/payment-settings?onboarding=refresh"
            : request.RefreshUrl;

        var url = await _connectService.GetOnboardingUrlAsync(tenantId.Value, returnUrl, refreshUrl, ct);

        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new { error = "No Stripe Connect account found. Please start onboarding first." });
        }

        return Ok(new { onboardingUrl = url });
    }

    /// <summary>
    /// Syncs the Connect account status from Stripe.
    /// Call this after user returns from onboarding or periodically.
    /// </summary>
    [HttpPost("sync")]
    public async Task<IActionResult> SyncStatus(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == null)
        {
            return BadRequest(new { error = "Tenant context not available" });
        }

        var status = await _connectService.SyncAccountStatusAsync(tenantId.Value, ct);
        return Ok(status);
    }

    /// <summary>
    /// Gets a login link to the Stripe Express Dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardLink(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == null)
        {
            return BadRequest(new { error = "Tenant context not available" });
        }

        var url = await _connectService.CreateDashboardLoginLinkAsync(tenantId.Value, ct);

        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new { error = "Could not create dashboard link. Ensure Stripe Connect is set up." });
        }

        return Ok(new { dashboardUrl = url });
    }
}

/// <summary>
/// Request model for starting/resuming onboarding.
/// </summary>
public class OnboardingRequest
{
    /// <summary>
    /// URL to redirect to after successful onboarding.
    /// </summary>
    public string? ReturnUrl { get; set; }
    
    /// <summary>
    /// URL to redirect to if user needs to restart onboarding (e.g., link expired).
    /// </summary>
    public string? RefreshUrl { get; set; }
}
