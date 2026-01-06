using CRS.Core.ReserveCalculator.Models;
using CRS.Models;
using CRS.Models.Workflow;

namespace CRS.Services.Interfaces;

/// <summary>
/// Result of a report generation operation.
/// </summary>
public class ReportGenerationResult
{
    /// <summary>
    /// Indicates if the generation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// The generated report record.
    /// </summary>
    public GeneratedReport? Report { get; set; }

    /// <summary>
    /// The calculation result used for the report.
    /// </summary>
    public ReserveStudyResult? CalculationResult { get; set; }

    /// <summary>
    /// Error message if generation failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Any warnings from the generation process.
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    public static ReportGenerationResult Success(GeneratedReport report, ReserveStudyResult result)
    {
        return new ReportGenerationResult
        {
            IsSuccess = true,
            Report = report,
            CalculationResult = result
        };
    }

    public static ReportGenerationResult Failure(string error)
    {
        return new ReportGenerationResult
        {
            IsSuccess = false,
            ErrorMessage = error
        };
    }
}

/// <summary>
/// Options for report generation.
/// </summary>
public class ReportGenerationOptions
{
    /// <summary>
    /// The type of report to generate.
    /// </summary>
    public ReportType ReportType { get; set; } = ReportType.FundingPlan;

    /// <summary>
    /// Optional notes to include with the report.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Custom title for the report.
    /// </summary>
    public string? CustomTitle { get; set; }

    /// <summary>
    /// Whether to generate PDF format.
    /// </summary>
    public bool GeneratePdf { get; set; } = true;

    /// <summary>
    /// Whether to generate Excel format.
    /// </summary>
    public bool GenerateExcel { get; set; } = true;

    /// <summary>
    /// Scenario ID to use for calculation. If null, uses study data directly.
    /// </summary>
    public int? ScenarioId { get; set; }
}

/// <summary>
/// Service for generating complete reserve study reports with calculator integration.
/// Orchestrates calculation, file generation, storage, and report record creation.
/// </summary>
public interface IReportGenerationService
{
    /// <summary>
    /// Checks if a reserve study is ready for report generation.
    /// Returns true if the study is at FundingPlanReady stage or later.
    /// </summary>
    /// <param name="studyId">The reserve study ID.</param>
    /// <returns>True if ready for report generation.</returns>
    Task<bool> CanGenerateReportAsync(Guid studyId);

    /// <summary>
    /// Gets the minimum workflow status required for report generation.
    /// </summary>
    StudyStatus MinimumRequiredStatus => StudyStatus.FundingPlanReady;

    /// <summary>
    /// Generates a complete report for a reserve study.
    /// Runs the calculator, generates PDF/Excel, stores files, and creates the report record.
    /// </summary>
    /// <param name="studyId">The reserve study ID.</param>
    /// <param name="generatedByUserId">The user generating the report.</param>
    /// <param name="options">Generation options.</param>
    /// <returns>The generation result with report and calculation data.</returns>
    Task<ReportGenerationResult> GenerateReportAsync(
        Guid studyId,
        Guid generatedByUserId,
        ReportGenerationOptions? options = null);

    /// <summary>
    /// Regenerates a report with updated calculation data.
    /// </summary>
    /// <param name="reportId">The existing report ID to regenerate.</param>
    /// <param name="regeneratedByUserId">The user regenerating the report.</param>
    /// <returns>The generation result with updated report.</returns>
    Task<ReportGenerationResult> RegenerateReportAsync(
        Guid reportId,
        Guid regeneratedByUserId);

    /// <summary>
    /// Gets the current calculation result for a study without generating a report.
    /// Useful for previewing before generation.
    /// </summary>
    /// <param name="studyId">The reserve study ID.</param>
    /// <returns>The calculation result or null if not available.</returns>
    Task<ReserveStudyResult?> PreviewCalculationAsync(Guid studyId);
}
