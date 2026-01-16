using CRS.Models;

namespace CRS.Services.Interfaces;

/// <summary>
/// Service interface for generating invoice PDFs.
/// </summary>
public interface IInvoicePdfService
{
    /// <summary>
    /// Generates a PDF for the specified invoice.
    /// </summary>
    /// <param name="invoiceId">The invoice ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF as byte array</returns>
    Task<byte[]> GeneratePdfAsync(Guid invoiceId, CancellationToken ct = default);

    /// <summary>
    /// Generates a PDF for the specified invoice.
    /// </summary>
    /// <param name="invoice">The invoice entity (must include LineItems and ReserveStudy)</param>
    /// <returns>PDF as byte array</returns>
    byte[] GeneratePdf(Invoice invoice);

    /// <summary>
    /// Gets the filename for an invoice PDF.
    /// </summary>
    string GetPdfFilename(Invoice invoice);
}
