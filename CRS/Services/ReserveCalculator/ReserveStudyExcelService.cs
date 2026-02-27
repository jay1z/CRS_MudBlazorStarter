using ClosedXML.Excel;
using Horizon.Core.ReserveCalculator.Enums;
using Horizon.Core.ReserveCalculator.Models;

namespace Horizon.Services.ReserveCalculator;

/// <summary>
/// Service for importing/exporting reserve study data from Excel files.
/// </summary>
public interface IReserveStudyExcelService
{
    /// <summary>
    /// Imports components from an Excel file.
    /// </summary>
    Task<ExcelImportResult> ImportComponentsAsync(Stream fileStream, string fileName);

    /// <summary>
    /// Exports calculation results to an Excel file.
    /// </summary>
    byte[] ExportResults(ReserveStudyResult result, ReserveStudyReportOptions? options = null);

    /// <summary>
    /// Generates a template Excel file for component import.
    /// </summary>
    byte[] GenerateImportTemplate();
}

/// <summary>
/// Result of an Excel import operation.
/// </summary>
public class ExcelImportResult
{
    public bool IsSuccess { get; set; }
    public List<ReserveComponentInput> Components { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public int RowsProcessed { get; set; }
    public int RowsSkipped { get; set; }
}

/// <summary>
/// Implementation of Excel import/export service using ClosedXML.
/// </summary>
public class ReserveStudyExcelService : IReserveStudyExcelService
{
    public async Task<ExcelImportResult> ImportComponentsAsync(Stream fileStream, string fileName)
    {
        var result = new ExcelImportResult();

        try
        {
            using var workbook = new XLWorkbook(fileStream);
            var worksheet = workbook.Worksheets.FirstOrDefault();

            if (worksheet == null)
            {
                result.Errors.Add("No worksheet found in the Excel file.");
                return result;
            }

            // Find header row and map columns
            var headerRow = worksheet.Row(1);
            var columnMap = MapColumns(headerRow);

            if (!columnMap.ContainsKey("name"))
            {
                result.Errors.Add("Required column 'Name' not found. Please use the import template.");
                return result;
            }

            // Process data rows
            var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            for (int row = 2; row <= lastRow; row++)
            {
                result.RowsProcessed++;

                try
                {
                    var component = ParseComponentRow(worksheet.Row(row), columnMap, result);
                    if (component != null)
                    {
                        result.Components.Add(component);
                    }
                    else
                    {
                        result.RowsSkipped++;
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Row {row}: {ex.Message}");
                    result.RowsSkipped++;
                }
            }

            result.IsSuccess = result.Components.Count > 0;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error reading Excel file: {ex.Message}");
        }

        return await Task.FromResult(result);
    }

    public byte[] ExportResults(ReserveStudyResult result, ReserveStudyReportOptions? options = null)
    {
        options ??= new ReserveStudyReportOptions();

        using var workbook = new XLWorkbook();

        // Summary sheet
        var summarySheet = workbook.AddWorksheet("Summary");
        AddSummarySheet(summarySheet, result, options);

        // Cash Flow sheet
        var cashFlowSheet = workbook.AddWorksheet("Cash Flow");
        AddCashFlowSheet(cashFlowSheet, result);

        // Allocation sheet
        if (result.Allocation.Count > 0)
        {
            var allocationSheet = workbook.AddWorksheet("Allocation");
            AddAllocationSheet(allocationSheet, result);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] GenerateImportTemplate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Components");

        // Headers
        var headers = new[]
        {
            "Name", "Category", "Method", "Current Cost",
            "Useful Life (Years)", "Remaining Life (Years)",
            "Cycle Years", "Annual Cost Override", "Inflation Rate Override", "Notes"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Sample data
        var sampleData = new object[][]
        {
            new object[] { "Roof Replacement", "Building Exterior", "Replacement", 150000, 25, 10, "", "", "", "Main building roofing" },
            new object[] { "Exterior Painting", "Building Exterior", "Replacement", 80000, 7, 3, "", "", "", "" },
            new object[] { "Pool Resurfacing", "Amenities", "Replacement", 45000, 10, 4, "", "", "", "" },
            new object[] { "Landscaping", "Site", "PRN", 15000, "", "", 1, "", "", "Annual maintenance" },
            new object[] { "HVAC System", "Mechanical", "Replacement", 60000, 15, 6, "", "", "", "" }
        };

        for (int row = 0; row < sampleData.Length; row++)
        {
            for (int col = 0; col < sampleData[row].Length; col++)
            {
                worksheet.Cell(row + 2, col + 1).Value = XLCellValue.FromObject(sampleData[row][col]);
            }
        }

        // Instructions sheet
        var instructionsSheet = workbook.AddWorksheet("Instructions");
        instructionsSheet.Cell(1, 1).Value = "Reserve Study Component Import Template";
        instructionsSheet.Cell(1, 1).Style.Font.Bold = true;
        instructionsSheet.Cell(1, 1).Style.Font.FontSize = 14;

        var instructions = new[]
        {
            "",
            "Instructions:",
            "1. Fill in the 'Components' sheet with your reserve study components",
            "2. Required fields: Name, Current Cost",
            "3. Method can be 'Replacement', 'PRN', or 'Combo'",
            "4. For Replacement method: provide Useful Life and Remaining Life",
            "5. For PRN method: provide Cycle Years (1 = annual)",
            "6. Delete the sample rows before importing",
            "",
            "Column Descriptions:",
            "- Name: Component name (required)",
            "- Category: Grouping category (e.g., Building Exterior, Amenities)",
            "- Method: Replacement, PRN (Periodic), or Combo",
            "- Current Cost: Today's replacement/repair cost",
            "- Useful Life: How long the component lasts (years)",
            "- Remaining Life: Years until next replacement (can be 0 if past due)",
            "- Cycle Years: For PRN, how often the cost recurs (1 = annual)",
            "- Annual Cost Override: Alternative annual cost for PRN",
            "- Inflation Rate Override: Component-specific inflation (e.g., 0.04 for 4%)",
            "- Notes: Optional notes"
        };

        for (int i = 0; i < instructions.Length; i++)
        {
            instructionsSheet.Cell(i + 2, 1).Value = instructions[i];
        }

        // Auto-fit columns
        worksheet.Columns().AdjustToContents();
        instructionsSheet.Column(1).Width = 80;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    #region Private Methods

    private Dictionary<string, int> MapColumns(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastCol = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 1;

        for (int col = 1; col <= lastCol; col++)
        {
            var header = headerRow.Cell(col).GetString().Trim().ToLowerInvariant()
                .Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");

            if (!string.IsNullOrEmpty(header))
            {
                map[header] = col;
            }
        }

        return map;
    }

    private ReserveComponentInput? ParseComponentRow(IXLRow row, Dictionary<string, int> columnMap, ExcelImportResult result)
    {
        var name = GetCellString(row, columnMap, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return null; // Skip empty rows
        }

        var component = new ReserveComponentInput
        {
            Name = name,
            Category = GetCellString(row, columnMap, "category") ?? "General",
            Method = ParseMethod(GetCellString(row, columnMap, "method")),
            CurrentCost = GetCellDecimal(row, columnMap, "currentcost") ?? 0,
            UsefulLifeYears = GetCellInt(row, columnMap, "usefullifeyears") ?? GetCellInt(row, columnMap, "usefullife"),
            RemainingLifeOverrideYears = GetCellInt(row, columnMap, "remaininglifeyears") ?? GetCellInt(row, columnMap, "remaininglife"),
            CycleYears = GetCellInt(row, columnMap, "cycleyears"),
            AnnualCostOverride = GetCellDecimal(row, columnMap, "annualcostoverride"),
            InflationRateOverride = GetCellDecimal(row, columnMap, "inflationrateoverride")
        };

        // Validate
        var errors = component.Validate();
        foreach (var error in errors)
        {
            result.Warnings.Add($"Row {row.RowNumber()}: {error}");
        }

        return component;
    }

    private string? GetCellString(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var value = row.Cell(col).GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private int? GetCellInt(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue(out double d)) return (int)d;
        if (int.TryParse(cell.GetString(), out var i)) return i;
        return null;
    }

    private decimal? GetCellDecimal(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col)) return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty()) return null;
        if (cell.TryGetValue(out double d)) return (decimal)d;
        if (decimal.TryParse(cell.GetString(), out var dec)) return dec;
        return null;
    }

    private ComponentMethod ParseMethod(string? method)
    {
        if (string.IsNullOrWhiteSpace(method)) return ComponentMethod.Replacement;
        return method.ToUpperInvariant() switch
        {
            "PRN" => ComponentMethod.PRN,
            "PERIODIC" => ComponentMethod.PRN,
            "COMBO" => ComponentMethod.Combo,
            "COMBINATION" => ComponentMethod.Combo,
            _ => ComponentMethod.Replacement
        };
    }

    private void AddSummarySheet(IXLWorksheet sheet, ReserveStudyResult result, ReserveStudyReportOptions options)
    {
        int row = 1;

        sheet.Cell(row, 1).Value = options.Title;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 16;
        row += 2;

        if (!string.IsNullOrEmpty(options.CommunityName))
        {
            sheet.Cell(row, 1).Value = "Community:";
            sheet.Cell(row, 2).Value = options.CommunityName;
            row++;
        }

        sheet.Cell(row, 1).Value = "Start Year:";
        sheet.Cell(row, 2).Value = result.StartYear;
        row++;

        sheet.Cell(row, 1).Value = "Projection Years:";
        sheet.Cell(row, 2).Value = result.ProjectionYears;
        row += 2;

        // Summary values
        sheet.Cell(row, 1).Value = "Total Contributions:";
        sheet.Cell(row, 2).Value = result.TotalContributions;
        sheet.Cell(row, 2).Style.NumberFormat.Format = "$#,##0";
        row++;

        sheet.Cell(row, 1).Value = "Total Expenditures:";
        sheet.Cell(row, 2).Value = result.TotalExpenditures;
        sheet.Cell(row, 2).Style.NumberFormat.Format = "$#,##0";
        row++;

        sheet.Cell(row, 1).Value = "Interest Earned:";
        sheet.Cell(row, 2).Value = result.TotalInterestEarned;
        sheet.Cell(row, 2).Style.NumberFormat.Format = "$#,##0";
        row++;

        sheet.Cell(row, 1).Value = "Final Balance:";
        sheet.Cell(row, 2).Value = result.FinalBalance;
        sheet.Cell(row, 2).Style.NumberFormat.Format = "$#,##0";
        if (result.FinalBalance < 0)
            sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
        row += 2;

        sheet.Cell(row, 1).Value = "Fully Funded:";
        sheet.Cell(row, 2).Value = result.IsFullyFunded ? "Yes" : "No";
        if (!result.IsFullyFunded)
        {
            sheet.Cell(row, 2).Style.Font.FontColor = XLColor.Red;
            row++;
            sheet.Cell(row, 1).Value = "Deficit Years:";
            sheet.Cell(row, 2).Value = result.DeficitYearCount;
        }

        sheet.Columns().AdjustToContents();
    }

    private void AddCashFlowSheet(IXLWorksheet sheet, ReserveStudyResult result)
    {
        // Headers
        var headers = new[] { "Year", "Beginning Balance", "Contributions", "Interest", "Expenditures", "Ending Balance" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        // Data
        int row = 2;
        foreach (var year in result.Years)
        {
            sheet.Cell(row, 1).Value = year.CalendarYear;
            sheet.Cell(row, 2).Value = year.BeginningBalance;
            sheet.Cell(row, 3).Value = year.Contribution;
            sheet.Cell(row, 4).Value = year.InterestEarned;
            sheet.Cell(row, 5).Value = year.Expenditures;
            sheet.Cell(row, 6).Value = year.EndingBalance;

            // Format
            for (int col = 2; col <= 6; col++)
            {
                sheet.Cell(row, col).Style.NumberFormat.Format = "$#,##0";
            }

            if (year.EndingBalance < 0)
            {
                sheet.Cell(row, 6).Style.Font.FontColor = XLColor.Red;
            }

            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    private void AddAllocationSheet(IXLWorksheet sheet, ReserveStudyResult result)
    {
        var headers = new[] { "Category", "Components", "Total Spend", "% of Total" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
        }

        int row = 2;
        foreach (var alloc in result.Allocation)
        {
            sheet.Cell(row, 1).Value = alloc.Category;
            sheet.Cell(row, 2).Value = alloc.ComponentCount;
            sheet.Cell(row, 3).Value = alloc.TotalSpend;
            sheet.Cell(row, 3).Style.NumberFormat.Format = "$#,##0";
            sheet.Cell(row, 4).Value = alloc.PercentOfTotal / 100;
            sheet.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            row++;
        }

        sheet.Columns().AdjustToContents();
    }

    #endregion
}
