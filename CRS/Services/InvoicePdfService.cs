using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestDocument = QuestPDF.Fluent.Document;

namespace CRS.Services;

/// <summary>
/// Service for generating invoice PDFs using QuestPDF.
/// </summary>
public class InvoicePdfService : IInvoicePdfService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<InvoicePdfService> _logger;

    public InvoicePdfService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ILogger<InvoicePdfService> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _logger = logger;

        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid invoiceId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var invoice = await context.Invoices
            .Include(i => i.LineItems.Where(li => li.DateDeleted == null))
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .Include(i => i.ReserveStudy)
                .ThenInclude(rs => rs!.Contact)
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Invoice {invoiceId} not found");

        // Get tenant and invoice settings for branding
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == invoice.TenantId, ct);
        var settings = await context.TenantInvoiceSettings
            .FirstOrDefaultAsync(s => s.TenantId == invoice.TenantId && s.DateDeleted == null, ct);

        var branding = InvoiceBranding.FromSettings(settings, tenant);

        return GeneratePdfInternal(invoice, branding);
    }

    public byte[] GeneratePdf(Invoice invoice)
    {
        return GeneratePdfInternal(invoice, InvoiceBranding.CreateDefault());
    }

    public string GetPdfFilename(Invoice invoice)
    {
        return $"Invoice-{invoice.InvoiceNumber}.pdf";
    }

    private byte[] GeneratePdfInternal(Invoice invoice, InvoiceBranding branding)
    {
        var primaryColor = ParseColor(branding.PrimaryColor, Colors.Blue.Darken2);
        var secondaryColor = ParseColor(branding.SecondaryColor, Colors.Purple.Darken1);

        var document = QuestDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.Grey.Darken3));

                page.Header().Element(c => ComposeHeader(c, invoice, branding, primaryColor));
                page.Content().Element(c => ComposeContent(c, invoice, branding, primaryColor));
                page.Footer().Element(c => ComposeFooter(c, invoice, branding, primaryColor));
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Parses a hex color string to QuestPDF color.
    /// </summary>
    private static string ParseColor(string hexColor, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            return fallback;

        // Remove # if present
        hexColor = hexColor.TrimStart('#');

        // Validate hex format
        if (hexColor.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(hexColor, "^[0-9A-Fa-f]{6}$"))
            return fallback;

        return $"#{hexColor}";
    }

    private void ComposeHeader(IContainer container, Invoice invoice, InvoiceBranding branding, string primaryColor)
    {
        container.Row(row =>
        {
            // Company Info (Left side)
            row.RelativeItem().Column(column =>
            {
                // Logo if available
                if (!string.IsNullOrWhiteSpace(branding.LogoUrl))
                {
                    try
                    {
                        // For now, just show company name with logo placeholder
                        // Full logo support would require downloading the image
                        column.Item().Text(branding.CompanyName)
                            .FontSize(20).Bold().FontColor(primaryColor);
                    }
                    catch
                    {
                        column.Item().Text(branding.CompanyName)
                            .FontSize(20).Bold().FontColor(primaryColor);
                    }
                }
                else
                {
                    column.Item().Text(branding.CompanyName)
                        .FontSize(20).Bold().FontColor(primaryColor);
                }

                if (!string.IsNullOrWhiteSpace(branding.Tagline))
                {
                    column.Item().PaddingTop(3).Text(branding.Tagline)
                        .FontSize(9).FontColor(Colors.Grey.Medium);
                }

                // Contact info
                column.Item().PaddingTop(8);
                if (!string.IsNullOrWhiteSpace(branding.Address))
                {
                    column.Item().Text(branding.Address).FontSize(8).FontColor(Colors.Grey.Darken1);
                }
                if (!string.IsNullOrWhiteSpace(branding.Phone))
                {
                    column.Item().Text($"Tel: {branding.Phone}").FontSize(8).FontColor(Colors.Grey.Darken1);
                }
                if (!string.IsNullOrWhiteSpace(branding.Email))
                {
                    column.Item().Text(branding.Email).FontSize(8).FontColor(primaryColor);
                }
                if (!string.IsNullOrWhiteSpace(branding.Website))
                {
                    column.Item().Text(branding.Website).FontSize(8).FontColor(primaryColor);
                }
            });

            // Invoice Title (Right side)
            row.RelativeItem().AlignRight().Column(column =>
                    {
                        column.Item().Text("INVOICE")
                            .FontSize(28).Bold().FontColor(primaryColor);

                        column.Item().PaddingTop(5).Text($"#{invoice.InvoiceNumber}")
                            .FontSize(14).FontColor(Colors.Grey.Darken1);

                        string statusColor = invoice.Status switch
                        {
                            InvoiceStatus.Paid => Colors.Green.Darken1,
                            InvoiceStatus.Voided => Colors.Grey.Darken1,
                            InvoiceStatus.Overdue => Colors.Red.Darken1,
                            _ => primaryColor
                        };

                        column.Item().PaddingTop(3).Text(invoice.Status.ToString().ToUpper())
                            .FontSize(10).Bold().FontColor(statusColor);
                    });
                });
            }

    private void ComposeContent(IContainer container, Invoice invoice, InvoiceBranding branding, string primaryColor)
    {
        container.PaddingTop(20).Column(column =>
        {
            // Bill To & Invoice Details
            column.Item().Row(row =>
            {
                // Bill To
                row.RelativeItem().Column(billTo =>
                {
                    billTo.Item().Text("BILL TO").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                    billTo.Item().PaddingTop(5).Text(invoice.BillToName ?? "Customer")
                        .FontSize(11).Bold();

                    if (!string.IsNullOrWhiteSpace(invoice.BillToAddress))
                    {
                        billTo.Item().Text(invoice.BillToAddress).FontSize(10);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.BillToEmail))
                    {
                        billTo.Item().Text(invoice.BillToEmail).FontSize(10).FontColor(primaryColor);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.BillToPhone))
                    {
                        billTo.Item().Text(invoice.BillToPhone).FontSize(10);
                    }
                });

                // Invoice Details
                row.RelativeItem().AlignRight().Column(details =>
                {
                    details.Item().Text("INVOICE DETAILS").FontSize(9).Bold().FontColor(Colors.Grey.Medium);

                    details.Item().PaddingTop(5).Row(r =>
                    {
                        r.AutoItem().Text("Invoice Date: ").FontSize(10);
                        r.AutoItem().Text(invoice.InvoiceDate.ToString("MMM dd, yyyy")).FontSize(10).Bold();
                    });

                    details.Item().Row(r =>
                    {
                        r.AutoItem().Text("Due Date: ").FontSize(10);
                        r.AutoItem().Text(invoice.DueDate.ToString("MMM dd, yyyy"))
                            .FontSize(10).Bold()
                            .FontColor(invoice.Status == InvoiceStatus.Overdue ? Colors.Red.Darken1 : Colors.Grey.Darken3);
                    });

                    if (!string.IsNullOrWhiteSpace(invoice.MilestoneDescription))
                    {
                        details.Item().PaddingTop(3).Row(r =>
                        {
                            r.AutoItem().Text("Milestone: ").FontSize(10);
                            r.AutoItem().Text(invoice.MilestoneDescription).FontSize(10).Bold();
                        });
                    }
                });
            });

            // Project/Study Reference
            if (invoice.ReserveStudy?.Community != null)
            {
                column.Item().PaddingTop(15).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.AutoItem().Text("Project: ").FontSize(10).FontColor(Colors.Grey.Darken1);
                    row.AutoItem().Text(invoice.ReserveStudy.Community.Name ?? "Reserve Study")
                        .FontSize(10).Bold();
                });
            }

            // Line Items Table
            column.Item().PaddingTop(20).Element(c => ComposeLineItemsTable(c, invoice, primaryColor));

            // Totals Section
            column.Item().PaddingTop(10).AlignRight().Width(250).Column(totals =>
            {
                // Subtotal
                totals.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Row(row =>
                {
                    row.RelativeItem().Text("Subtotal").FontSize(10);
                    row.AutoItem().Text(invoice.Subtotal.ToString("C")).FontSize(10).Bold();
                });

                // Tax
                if (invoice.TaxAmount > 0)
                {
                    totals.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text($"Tax ({invoice.TaxRate:N1}%)").FontSize(10);
                        row.AutoItem().Text(invoice.TaxAmount.ToString("C")).FontSize(10);
                    });
                }

                // Discount
                if (invoice.DiscountAmount > 0)
                {
                    totals.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text(invoice.DiscountDescription ?? "Discount").FontSize(10).FontColor(Colors.Green.Darken1);
                        row.AutoItem().Text($"-{invoice.DiscountAmount:C}").FontSize(10).FontColor(Colors.Green.Darken1);
                    });
                }

                // Total
                totals.Item().Background(primaryColor).Padding(8).Row(row =>
                {
                    row.RelativeItem().Text("TOTAL").FontSize(12).Bold().FontColor(Colors.White);
                    row.AutoItem().Text(invoice.TotalAmount.ToString("C")).FontSize(12).Bold().FontColor(Colors.White);
                });

                // Amount Paid
                if (invoice.AmountPaid > 0)
                {
                    totals.Item().Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Amount Paid").FontSize(10).FontColor(Colors.Green.Darken1);
                        row.AutoItem().Text($"-{invoice.AmountPaid:C}").FontSize(10).FontColor(Colors.Green.Darken1);
                    });

                    var balance = invoice.TotalAmount - invoice.AmountPaid;
                    totals.Item().Background(Colors.Grey.Lighten3).Padding(5).Row(row =>
                    {
                        row.RelativeItem().Text("Balance Due").FontSize(11).Bold();
                        row.AutoItem().Text(balance.ToString("C")).FontSize(11).Bold()
                            .FontColor(balance > 0 ? Colors.Red.Darken1 : Colors.Green.Darken1);
                    });
                }
            });

            // Early Payment Discount Notice
            if (invoice.IsEarlyPaymentDiscountAvailable)
            {
                column.Item().PaddingTop(15).Background(Colors.Green.Lighten4).Padding(10).Row(row =>
                {
                    row.AutoItem().PaddingRight(5).Text("💰").FontSize(14);
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text($"Early Payment Discount Available!").FontSize(10).Bold().FontColor(Colors.Green.Darken2);
                        c.Item().Text($"Pay by {invoice.EarlyPaymentDiscountDate:MMM dd, yyyy} to save {invoice.EarlyPaymentDiscountAmount:C} ({invoice.EarlyPaymentDiscountPercentage}% off)")
                            .FontSize(9).FontColor(Colors.Green.Darken1);
                    });
                });
            }

            // Payment Instructions
            if (!string.IsNullOrWhiteSpace(branding.PaymentInstructions))
            {
                column.Item().PaddingTop(15).Background(Colors.Blue.Lighten5).Padding(10).Column(c =>
                {
                    c.Item().Text("PAYMENT INSTRUCTIONS").FontSize(9).Bold().FontColor(primaryColor);
                    c.Item().PaddingTop(3).Text(branding.PaymentInstructions).FontSize(9);
                });
            }

            // Notes & Terms
            if (!string.IsNullOrWhiteSpace(invoice.Notes) || !string.IsNullOrWhiteSpace(invoice.Terms))
            {
                column.Item().PaddingTop(20).Column(notes =>
                {
                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        notes.Item().Text("NOTES").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                        notes.Item().PaddingTop(3).Text(invoice.Notes).FontSize(9);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Terms))
                    {
                        notes.Item().PaddingTop(10).Text("TERMS & CONDITIONS").FontSize(9).Bold().FontColor(Colors.Grey.Medium);
                        notes.Item().PaddingTop(3).Text(invoice.Terms).FontSize(9);
                    }
                });
            }

            // PAID watermark for paid invoices
            if (invoice.Status == InvoiceStatus.Paid && branding.ShowPaidWatermark)
            {
                column.Item().PaddingTop(20).AlignCenter()
                    .Text("PAID")
                    .FontSize(36).Bold().FontColor(Colors.Green.Lighten2);
            }
        });
    }

    private void ComposeLineItemsTable(IContainer tableContainer, Invoice invoice, string primaryColor)
    {
        tableContainer.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(4); // Description
                columns.RelativeColumn(1); // Qty
                columns.RelativeColumn(1); // Unit Price
                columns.RelativeColumn(1); // Total
            });

            // Header with branded color
            table.Header(header =>
            {
                header.Cell().Background(primaryColor).Padding(8)
                    .Text("Description").FontColor(Colors.White).Bold();
                header.Cell().Background(primaryColor).Padding(8).AlignCenter()
                    .Text("Qty").FontColor(Colors.White).Bold();
                header.Cell().Background(primaryColor).Padding(8).AlignRight()
                    .Text("Unit Price").FontColor(Colors.White).Bold();
                header.Cell().Background(primaryColor).Padding(8).AlignRight()
                    .Text("Total").FontColor(Colors.White).Bold();
            });

            // Line Items
            var lineItems = invoice.LineItems.OrderBy(li => li.SortOrder).ToList();
            for (int i = 0; i < lineItems.Count; i++)
            {
                var item = lineItems[i];
                var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                table.Cell().Background(bgColor).Padding(8)
                    .Text(item.Description ?? "Service");
                table.Cell().Background(bgColor).Padding(8).AlignCenter()
                    .Text(item.Quantity.ToString("N2"));
                table.Cell().Background(bgColor).Padding(8).AlignRight()
                    .Text(item.UnitPrice.ToString("C"));
                table.Cell().Background(bgColor).Padding(8).AlignRight()
                    .Text(item.LineTotal.ToString("C"));
            }
        });
    }

    private void ComposeFooter(IContainer container, Invoice invoice, InvoiceBranding branding, string primaryColor)
    {
        container.Column(column =>
        {
            column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span(branding.FooterText).FontSize(9).FontColor(Colors.Grey.Darken1);
                });

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8);
                    text.CurrentPageNumber().FontSize(8);
                    text.Span(" of ").FontSize(8);
                    text.TotalPages().FontSize(8);
                });
            });

            if (invoice.Status == InvoiceStatus.Voided)
            {
                column.Item().PaddingTop(10).AlignCenter()
                    .Text("*** VOID ***")
                    .FontSize(24).Bold().FontColor(Colors.Red.Darken1);
            }
        });
    }
}
