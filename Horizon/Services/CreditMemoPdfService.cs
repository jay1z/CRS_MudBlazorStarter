using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Horizon.Services;

/// <summary>
/// Service for generating credit memo PDFs.
/// </summary>
public interface ICreditMemoPdfService
{
    Task<byte[]> GeneratePdfAsync(Guid creditMemoId, CancellationToken ct = default);
    string GetPdfFilename(CreditMemo creditMemo);
}

public class CreditMemoPdfService : ICreditMemoPdfService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<CreditMemoPdfService> _logger;

    public CreditMemoPdfService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ILogger<CreditMemoPdfService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<byte[]> GeneratePdfAsync(Guid creditMemoId, CancellationToken ct = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(ct);

        var creditMemo = await context.CreditMemos
            .Include(cm => cm.Invoice)
            .Include(cm => cm.ReserveStudy)
                .ThenInclude(rs => rs!.Community)
            .FirstOrDefaultAsync(cm => cm.Id == creditMemoId && cm.DateDeleted == null, ct)
            ?? throw new InvalidOperationException($"Credit memo {creditMemoId} not found");

        // Get tenant and invoice settings for branding
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == creditMemo.TenantId, ct);
        var settings = await context.TenantInvoiceSettings
            .FirstOrDefaultAsync(s => s.TenantId == creditMemo.TenantId && s.DateDeleted == null, ct);

        var branding = InvoiceBranding.FromSettings(settings, tenant);
        var primaryColor = ParseColor(branding.PrimaryColor, Colors.Green.Darken2);

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Helvetica"));

                page.Header().Element(header => ComposeHeader(header, creditMemo, branding, primaryColor));
                page.Content().Element(content => ComposeContent(content, creditMemo, primaryColor));
                page.Footer().Element(footer => ComposeFooter(footer, branding));
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);

        _logger.LogInformation("Generated PDF for credit memo {CreditMemoNumber}", creditMemo.CreditMemoNumber);
        return stream.ToArray();
    }

    public string GetPdfFilename(CreditMemo creditMemo)
    {
        return $"CreditMemo-{creditMemo.CreditMemoNumber}.pdf";
    }

    private static string ParseColor(string hexColor, string fallback)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
            return fallback;

        hexColor = hexColor.TrimStart('#');

        if (hexColor.Length != 6 || !System.Text.RegularExpressions.Regex.IsMatch(hexColor, "^[0-9A-Fa-f]{6}$"))
            return fallback;

        return $"#{hexColor}";
    }

    private void ComposeHeader(IContainer container, CreditMemo creditMemo, InvoiceBranding branding, string primaryColor)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                // Company branding
                column.Item().Text(branding.CompanyName)
                    .FontSize(16).Bold().FontColor(primaryColor);

                if (!string.IsNullOrWhiteSpace(branding.Tagline))
                {
                    column.Item().Text(branding.Tagline)
                        .FontSize(8).FontColor(Colors.Grey.Medium);
                }

                column.Item().PaddingTop(10);
                column.Item().Text("CREDIT MEMO")
                    .FontSize(24).Bold().FontColor(Colors.Green.Darken2);
                column.Item().Text(creditMemo.CreditMemoNumber)
                    .FontSize(14).FontColor(Colors.Grey.Darken1);
            });

            row.RelativeItem().AlignRight().Column(column =>
                    {
                        column.Item().Text($"Issue Date: {creditMemo.IssueDate:MMMM d, yyyy}");
                        if (creditMemo.AppliedAt.HasValue)
                        {
                            column.Item().Text($"Applied: {creditMemo.AppliedAt:MMMM d, yyyy}")
                                .FontColor(Colors.Green.Darken2);
                        }
                        column.Item().PaddingTop(5).Text($"Status: {creditMemo.Status}")
                            .Bold()
                            .FontColor(creditMemo.Status == CreditMemoStatus.Applied ? Colors.Green.Darken2 :
                                      creditMemo.Status == CreditMemoStatus.Voided ? Colors.Red.Darken2 :
                                      Colors.Orange.Darken2);
                    });
                });

                // Voided watermark
                if (creditMemo.Status == CreditMemoStatus.Voided)
                {
                    container.Layers(layers =>
                    {
                        layers.Layer().Text("VOID")
                            .FontSize(72)
                            .FontColor(Colors.Red.Lighten3)
                            .Bold()
                            .AlignCenter();
                    });
                }
            }

            private void ComposeContent(IContainer container, CreditMemo creditMemo, string primaryColor)
            {
                container.PaddingTop(20).Column(column =>
                {
                    // Bill To / Credit To
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Column(billTo =>
                        {
                            billTo.Item().Text("Credit Issued To:").Bold().FontSize(11);
                            billTo.Item().Text(creditMemo.BillToName ?? "N/A");
                            if (!string.IsNullOrEmpty(creditMemo.BillToEmail))
                            {
                                billTo.Item().Text(creditMemo.BillToEmail).FontColor(primaryColor);
                            }
                        });

                row.RelativeItem().AlignRight().Column(details =>
                {
                    details.Item().Text("Original Invoice:").Bold().FontSize(11);
                    details.Item().Text(creditMemo.Invoice?.InvoiceNumber ?? "N/A");
                    if (creditMemo.ReserveStudy?.Community != null)
                    {
                        details.Item().PaddingTop(10).Text("Property:").Bold().FontSize(11);
                        details.Item().Text(creditMemo.ReserveStudy.Community.Name);
                    }
                });
            });

            column.Item().PaddingVertical(15).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Credit Details
            column.Item().Text("Credit Details").Bold().FontSize(12);
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Description").Bold();
                    header.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text("Amount").Bold();
                });

                var reasonText = creditMemo.Reason.ToString().Replace("_", " ");
                var description = !string.IsNullOrEmpty(creditMemo.Description) 
                    ? creditMemo.Description 
                    : $"Credit - {reasonText}";

                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(description);
                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).AlignRight()
                    .Text(creditMemo.Amount.ToString("C")).Bold().FontColor(Colors.Green.Darken2);
            });

            // Total
            column.Item().PaddingTop(15).AlignRight().Row(row =>
            {
                row.AutoItem().PaddingRight(20).Text("Total Credit:").Bold().FontSize(14);
                row.AutoItem().Text(creditMemo.Amount.ToString("C")).Bold().FontSize(14).FontColor(Colors.Green.Darken2);
            });

            // Reason
            if (!string.IsNullOrEmpty(creditMemo.Description))
            {
                column.Item().PaddingTop(20).Column(notes =>
                {
                    notes.Item().Text("Reason / Notes:").Bold().FontSize(11);
                    notes.Item().PaddingTop(5).Text(creditMemo.Description);
                });
            }

            // Refund Info
            if (creditMemo.IsRefunded)
            {
                column.Item().PaddingTop(15).Background(Colors.Green.Lighten4).Padding(10).Column(refund =>
                {
                    refund.Item().Text("Refund Processed").Bold().FontColor(Colors.Green.Darken2);
                    refund.Item().Text($"Refunded on {creditMemo.RefundedAt:MMMM d, yyyy}");
                    if (!string.IsNullOrEmpty(creditMemo.StripeRefundId))
                    {
                        refund.Item().Text($"Reference: {creditMemo.StripeRefundId}").FontSize(9);
                    }
                });
                        }

                        // Void Reason
                        if (creditMemo.Status == CreditMemoStatus.Voided && !string.IsNullOrEmpty(creditMemo.VoidReason))
                        {
                            column.Item().PaddingTop(15).Background(Colors.Red.Lighten4).Padding(10).Column(voidInfo =>
                            {
                                voidInfo.Item().Text("Credit Memo Voided").Bold().FontColor(Colors.Red.Darken2);
                                voidInfo.Item().Text(creditMemo.VoidReason);
                                voidInfo.Item().Text($"Voided on {creditMemo.VoidedAt:MMMM d, yyyy}").FontSize(9);
                            });
                        }
                    });
                }

                private void ComposeFooter(IContainer container, InvoiceBranding branding)
                {
                    container.Column(column =>
                    {
                        column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(10);
                        column.Item().AlignCenter().Text(text =>
                        {
                            text.Span("This credit memo was generated electronically and is valid without signature.")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                        });

                        if (!string.IsNullOrWhiteSpace(branding.Website))
                        {
                            column.Item().AlignCenter().Text(branding.Website)
                                .FontSize(8).FontColor(Colors.Grey.Medium);
                        }
                    });
                }
            }
