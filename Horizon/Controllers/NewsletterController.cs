using Horizon.Services.Interfaces;

using Microsoft.AspNetCore.Mvc;

namespace Horizon.Controllers;

/// <summary>
/// Controller for newsletter subscription management endpoints.
/// These are public endpoints (no authentication required).
/// </summary>
[Route("newsletter")]
public class NewsletterController : Controller
{
    private readonly INewsletterService _newsletterService;
    private readonly ILogger<NewsletterController> _logger;

    public NewsletterController(
        INewsletterService newsletterService,
        ILogger<NewsletterController> logger)
    {
        _newsletterService = newsletterService;
        _logger = logger;
    }

    /// <summary>
    /// Confirms a newsletter subscription via the token from the confirmation email.
    /// </summary>
    [HttpGet("confirm")]
    public async Task<IActionResult> Confirm([FromQuery] Guid token, CancellationToken ct)
    {
        if (token == Guid.Empty)
        {
            return RedirectToPage("/newsletter-error", new { message = "Invalid confirmation link." });
        }

        var success = await _newsletterService.ConfirmSubscriptionAsync(token, ct);

        if (success)
        {
            _logger.LogInformation("Newsletter subscription confirmed via token");
            return Redirect("/newsletter-confirmed");
        }

        _logger.LogWarning("Failed newsletter confirmation attempt with token {Token}", token);
        return Redirect("/newsletter-error?message=expired");
    }

    /// <summary>
    /// Unsubscribes a user from the newsletter.
    /// </summary>
    [HttpGet("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromQuery] Guid id, [FromQuery] string? reason = null, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return Redirect("/newsletter-error?message=invalid");
        }

        var success = await _newsletterService.UnsubscribeByIdAsync(id, reason, ct);

        if (success)
        {
            _logger.LogInformation("Newsletter unsubscribe via link for subscriber {Id}", id);
            return Redirect("/newsletter-unsubscribed");
        }

        return Redirect("/newsletter-error?message=not-found");
    }

    /// <summary>
    /// API endpoint for subscribing via AJAX.
    /// </summary>
    [HttpPost("api/subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] NewsletterSubscribeRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains("@"))
        {
            return BadRequest(new { success = false, message = "Please provide a valid email address." });
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var result = await _newsletterService.SubscribeAsync(
            request.Email,
            request.Name,
            request.Company,
            request.Source ?? "api",
            ipAddress,
            userAgent,
            ct);

        return Ok(new
        {
            success = result.Success,
            message = result.Message,
            alreadySubscribed = result.AlreadySubscribed,
            confirmationRequired = result.ConfirmationRequired
        });
    }

    /// <summary>
    /// API endpoint for getting newsletter stats (admin only in production).
    /// </summary>
    [HttpGet("api/stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        // In production, add authorization check here
        var stats = await _newsletterService.GetStatsAsync(ct);
        return Ok(stats);
    }
}

/// <summary>
/// Request model for newsletter subscription API.
/// </summary>
public record NewsletterSubscribeRequest(
    string Email,
    string? Name = null,
    string? Company = null,
    string? Source = null
);
