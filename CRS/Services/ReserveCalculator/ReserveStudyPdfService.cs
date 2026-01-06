using CRS.Core.ReserveCalculator.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CRS.Services.ReserveCalculator;

/// <summary>
/// Service for generating PDF reports from reserve study calculations.
/// </summary>
public interface IReserveStudyPdfService
{
    /// <summary>
    /// Generates a PDF report from a calculation result.
    /// </summary>
    byte[] GenerateReport(ReserveStudyResult result, ReserveStudyReportOptions? options = null);
}

/// <summary>
/// Options for customizing the PDF report.
/// </summary>
public class ReserveStudyReportOptions
{
    public string Title { get; set; } = "Reserve Study Funding Plan";
    public string? CommunityName { get; set; }
    public string? PreparedBy { get; set; }
    public DateTime? PreparedDate { get; set; }
    public bool IncludeSummary { get; set; } = true;
    public bool IncludeCashFlowTable { get; set; } = true;
    public bool IncludeAllocation { get; set; } = true;
    public bool IncludeComponents { get; set; } = true;
    public string? LogoPath { get; set; }
}

/// <summary>
/// Implementation of reserve study PDF generation using QuestPDF.
/// </summary>
public class ReserveStudyPdfService : IReserveStudyPdfService
{
    public ReserveStudyPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateReport(ReserveStudyResult result, ReserveStudyReportOptions? options = null)
    {
        options ??= new ReserveStudyReportOptions();

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.Letter);
                page.Margin(0.75f, Unit.Inch);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, result, options));
                page.Content().Element(c => ComposeContent(c, result, options));
                page.Footer().Element(c => ComposeFooter(c, options));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, ReserveStudyResult result, ReserveStudyReportOptions options)
    {
        container.Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(options.Title).Bold().FontSize(18).FontColor(Colors.Blue.Darken2);
                    if (!string.IsNullOrEmpty(options.CommunityName))
                    {
                        col.Item().Text(options.CommunityName).FontSize(14).FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(150).AlignRight().Column(col =>
                {
                    col.Item().Text($"Start Year: {result.StartYear}").FontSize(10);
                    col.Item().Text($"Projection: {result.ProjectionYears} Years").FontSize(10);
                    if (options.PreparedDate.HasValue)
                    {
                        col.Item().Text($"Prepared: {options.PreparedDate:MMMM d, yyyy}").FontSize(10);
                    }
                });
            });

            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container, ReserveStudyResult result, ReserveStudyReportOptions options)
    {
        container.PaddingTop(15).Column(column =>
        {
            if (options.IncludeSummary)
            {
                column.Item().Element(c => ComposeSummary(c, result));
                column.Item().PaddingTop(20);
            }

            if (options.IncludeCashFlowTable)
            {
                column.Item().Element(c => ComposeCashFlowTable(c, result));
                column.Item().PaddingTop(20);
            }

            if (options.IncludeAllocation && result.Allocation.Count > 0)
            {
                column.Item().Element(c => ComposeAllocation(c, result));
            }
        });
    }

    private void ComposeSummary(IContainer container, ReserveStudyResult result)
    {
        container.Column(column =>
        {
            column.Item().Text("Executive Summary").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(10);

            // Summary cards in a grid
            column.Item().Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Total Contributions").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(result.TotalContributions.ToString("C0")).Bold().FontSize(14).FontColor(Colors.Green.Darken2);
                });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Total Expenditures").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(result.TotalExpenditures.ToString("C0")).Bold().FontSize(14).FontColor(Colors.Red.Darken2);
                });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Interest Earned").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(result.TotalInterestEarned.ToString("C0")).Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
                });

                row.ConstantItem(10);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(col =>
                {
                    col.Item().Text("Final Balance").FontSize(9).FontColor(Colors.Grey.Darken1);
                    var balanceColor = result.FinalBalance >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2;
                    col.Item().Text(result.FinalBalance.ToString("C0")).Bold().FontSize(14).FontColor(balanceColor);
                });
            });

            column.Item().PaddingTop(10);

            // Funding status
            if (result.IsFullyFunded)
            {
                column.Item().Background(Colors.Green.Lighten5).Padding(10).Row(row =>
                {
                    row.AutoItem().Text("✓").Bold().FontColor(Colors.Green.Darken2);
                    row.ConstantItem(5);
                    row.RelativeItem().Text("This funding plan maintains a positive balance throughout the projection period.")
                        .FontColor(Colors.Green.Darken2);
                });
            }
            else
            {
                column.Item().Background(Colors.Red.Lighten5).Padding(10).Row(row =>
                {
                    row.AutoItem().Text("⚠").Bold().FontColor(Colors.Red.Darken2);
                    row.ConstantItem(5);
                    row.RelativeItem().Text($"Warning: This plan has {result.DeficitYearCount} deficit year(s). " +
                        $"First deficit in {result.FirstDeficitYear}. Minimum balance: {result.MinimumBalance:C0} in {result.MinimumBalanceYear}.")
                        .FontColor(Colors.Red.Darken2);
                });
            }
        });
    }

    private void ComposeCashFlowTable(IContainer container, ReserveStudyResult result)
    {
        container.Column(column =>
        {
            column.Item().Text("30-Year Cash Flow Projection").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(10);

            column.Item().Table(table =>
            {
                // Define columns
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(50);  // Year
                    columns.RelativeColumn();    // Beginning
                    columns.RelativeColumn();    // Contributions
                    columns.RelativeColumn();    // Interest
                    columns.RelativeColumn();    // Expenditures
                    columns.RelativeColumn();    // Ending
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Year").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Beginning").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Contributions").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Interest").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Expenditures").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Ending").Bold().FontColor(Colors.White).FontSize(9);
                });

                // Data rows
                foreach (var year in result.Years)
                {
                    var bgColor = year.YearIndex % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;
                    var endingColor = year.EndingBalance >= 0 ? Colors.Black : Colors.Red.Darken2;

                    table.Cell().Background(bgColor).Padding(4).Text(year.CalendarYear.ToString()).FontSize(9);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text(year.BeginningBalance.ToString("N0")).FontSize(9);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text(year.Contribution.ToString("N0")).FontSize(9).FontColor(Colors.Green.Darken2);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text(year.InterestEarned.ToString("N0")).FontSize(9).FontColor(Colors.Blue.Darken1);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text(year.Expenditures.ToString("N0")).FontSize(9).FontColor(Colors.Red.Darken1);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text(year.EndingBalance.ToString("N0")).Bold().FontSize(9).FontColor(endingColor);
                }
            });
        });
    }

    private void ComposeAllocation(IContainer container, ReserveStudyResult result)
    {
        container.Column(column =>
        {
            column.Item().Text("Expenditure Allocation by Category").Bold().FontSize(14).FontColor(Colors.Blue.Darken2);
            column.Item().PaddingTop(10);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);  // Category
                    columns.ConstantColumn(80); // Components
                    columns.RelativeColumn();   // Total Spend
                    columns.RelativeColumn();   // Percent
                });

                table.Header(header =>
                {
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).Text("Category").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignCenter().Text("Components").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("Total Spend").Bold().FontColor(Colors.White).FontSize(9);
                    header.Cell().Background(Colors.Blue.Darken2).Padding(5).AlignRight().Text("% of Total").Bold().FontColor(Colors.White).FontSize(9);
                });

                int index = 0;
                foreach (var alloc in result.Allocation)
                {
                    var bgColor = index % 2 == 0 ? Colors.Grey.Lighten4 : Colors.White;

                    table.Cell().Background(bgColor).Padding(4).Text(alloc.Category).FontSize(9);
                    table.Cell().Background(bgColor).Padding(4).AlignCenter().Text(alloc.ComponentCount.ToString()).FontSize(9);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text(alloc.TotalSpend.ToString("C0")).FontSize(9);
                    table.Cell().Background(bgColor).Padding(4).AlignRight().Text($"{alloc.PercentOfTotal:N1}%").FontSize(9);

                    index++;
                }
            });
        });
    }

    private void ComposeFooter(IContainer container, ReserveStudyReportOptions options)
    {
        container.Column(column =>
        {
            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text(text =>
                {
                    text.Span("Generated by Reserve Study Calculator").FontSize(8).FontColor(Colors.Grey.Darken1);
                    if (!string.IsNullOrEmpty(options.PreparedBy))
                    {
                        text.Span($" | Prepared by: {options.PreparedBy}").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                row.ConstantItem(100).AlignRight().Text(text =>
                {
                    text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Darken1);
                    text.TotalPages().FontSize(8).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }
}
