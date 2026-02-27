using Horizon.Services.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Horizon.Controllers;

/// <summary>
/// API controller for invoice operations including PDF generation.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "RequireTenantStaff")]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly IInvoicePdfService _pdfService;
    private readonly ILogger<InvoicesController> _logger;

    public InvoicesController(
        IInvoiceService invoiceService,
        IInvoicePdfService pdfService,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Downloads the invoice as a PDF file.
    /// </summary>
    /// <param name="id">Invoice ID</param>
    /// <returns>PDF file</returns>
    [HttpGet("{id:guid}/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        try
        {
            var invoice = await _invoiceService.GetByIdAsync(id, ct);
            if (invoice == null)
            {
                return NotFound(new { error = "Invoice not found" });
            }

            var pdfBytes = await _pdfService.GeneratePdfAsync(id, ct);
            var filename = _pdfService.GetPdfFilename(invoice);

            _logger.LogInformation("Generated PDF for invoice {InvoiceNumber}", invoice.InvoiceNumber);

            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate PDF for invoice {InvoiceId}", id);
            return StatusCode(500, new { error = "Failed to generate invoice PDF" });
        }
    }

        /// <summary>
        /// Gets invoice details by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoice(Guid id, CancellationToken ct)
        {
            var invoice = await _invoiceService.GetByIdAsync(id, ct);
            if (invoice == null)
            {
                return NotFound(new { error = "Invoice not found" });
            }

            return Ok(invoice);
        }

        /// <summary>
        /// Creates a Stripe checkout session for paying an invoice.
        /// </summary>
        [HttpPost("{id:guid}/pay")]
        [ProducesResponseType(typeof(PaymentUrlResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [AllowAnonymous] // Allow clients to pay without being logged in
        public async Task<IActionResult> CreatePaymentSession(Guid id, CancellationToken ct)
        {
            try
            {
                var invoice = await _invoiceService.GetByIdAsync(id, ct);
                if (invoice == null)
                {
                    return NotFound(new { error = "Invoice not found" });
                }

                // Get payment service
                var paymentService = HttpContext.RequestServices.GetRequiredService<IInvoicePaymentService>();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var paymentUrl = await paymentService.GetOrCreatePaymentUrlAsync(id, baseUrl, ct);

                _logger.LogInformation("Created payment URL for invoice {InvoiceNumber}", invoice.InvoiceNumber);

                return Ok(new PaymentUrlResponse { PaymentUrl = paymentUrl });
                        }
                        catch (InvalidOperationException ex)
                        {
                            _logger.LogWarning(ex, "Cannot create payment session for invoice {InvoiceId}", id);
                            return BadRequest(new { error = ex.Message });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create payment session for invoice {InvoiceId}", id);
                            return StatusCode(500, new { error = "Failed to create payment session" });
                        }
                    }

                /// <summary>
                /// Generates or retrieves a public access token for an invoice.
                /// </summary>
                [HttpPost("{id:guid}/share")]
                [ProducesResponseType(typeof(ShareLinkResponse), StatusCodes.Status200OK)]
                [ProducesResponseType(StatusCodes.Status404NotFound)]
                public async Task<IActionResult> GenerateShareLink(Guid id, CancellationToken ct)
                {
                    try
                    {
                        var invoice = await _invoiceService.GetByIdAsync(id, ct);
                        if (invoice == null)
                        {
                            return NotFound(new { error = "Invoice not found" });
                        }

                        // If invoice already has an access token, return it
                        if (!string.IsNullOrEmpty(invoice.AccessToken))
                        {
                            var existingUrl = $"{Request.Scheme}://{Request.Host}/invoice/{invoice.AccessToken}";
                            return Ok(new ShareLinkResponse { ShareUrl = existingUrl, AccessToken = invoice.AccessToken });
                        }

                        // Generate new access token
                        var accessToken = await _invoiceService.GenerateAccessTokenAsync(id, ct);
                        var shareUrl = $"{Request.Scheme}://{Request.Host}/invoice/{accessToken}";

                        _logger.LogInformation("Generated share link for invoice {InvoiceId}", id);

                        return Ok(new ShareLinkResponse { ShareUrl = shareUrl, AccessToken = accessToken });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate share link for invoice {InvoiceId}", id);
                        return StatusCode(500, new { error = "Failed to generate share link" });
                    }
                }

                public class PaymentUrlResponse
                {
                    public string PaymentUrl { get; set; } = string.Empty;
                }

                public class ShareLinkResponse
                {
                    public string ShareUrl { get; set; } = string.Empty;
                    public string AccessToken { get; set; } = string.Empty;
                }
            }
