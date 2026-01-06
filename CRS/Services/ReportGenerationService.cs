using CRS.Core.ReserveCalculator.Models;
using CRS.Data;
using CRS.Models;
using CRS.Models.Workflow;
using CRS.Services.Interfaces;
using CRS.Services.ReserveCalculator;
using CRS.Services.Storage;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services;

/// <summary>
/// Implementation of report generation service.
/// Orchestrates calculator, file generation, storage, and report record management.
/// </summary>
public class ReportGenerationService : IReportGenerationService
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly IReserveStudyCalculatorService _calculatorService;
    private readonly IReserveStudyPdfService _pdfService;
    private readonly IReserveStudyExcelService _excelService;
    private readonly IDocumentStorageService _storageService;
    private readonly IGeneratedReportService _reportService;

    public ReportGenerationService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IReserveStudyCalculatorService calculatorService,
        IReserveStudyPdfService pdfService,
        IReserveStudyExcelService excelService,
        IDocumentStorageService storageService,
        IGeneratedReportService reportService)
    {
        _dbFactory = dbFactory;
        _calculatorService = calculatorService;
        _pdfService = pdfService;
        _excelService = excelService;
        _storageService = storageService;
        _reportService = reportService;
    }

    /// <inheritdoc />
    public StudyStatus MinimumRequiredStatus => StudyStatus.FundingPlanReady;

    /// <inheritdoc />
    public async Task<bool> CanGenerateReportAsync(Guid studyId)
    {
        await using var context = await _dbFactory.CreateDbContextAsync();
        
        var study = await context.ReserveStudies
            .Include(s => s.StudyRequest)
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study?.StudyRequest == null)
            return false;

        // Check if at FundingPlanReady or later
        return study.StudyRequest.CurrentStatus >= MinimumRequiredStatus;
    }

    /// <inheritdoc />
    public async Task<ReportGenerationResult> GenerateReportAsync(
        Guid studyId,
        Guid generatedByUserId,
        ReportGenerationOptions? options = null)
    {
        options ??= new ReportGenerationOptions();

        try
        {
            // Step 1: Verify study is ready
            if (!await CanGenerateReportAsync(studyId))
            {
                return ReportGenerationResult.Failure(
                    $"Reserve study is not ready for report generation. Minimum status required: {MinimumRequiredStatus}");
            }

            // Step 2: Load study data
            await using var context = await _dbFactory.CreateDbContextAsync();
            var study = await context.ReserveStudies
                .Include(s => s.Community)
                .Include(s => s.Proposal)
                .FirstOrDefaultAsync(s => s.Id == studyId);

            if (study == null)
            {
                return ReportGenerationResult.Failure("Reserve study not found.");
            }

            // Step 3: Run calculation
            ReserveStudyResult calculationResult;
            if (options.ScenarioId.HasValue)
            {
                calculationResult = await _calculatorService.CalculateScenarioAsync(options.ScenarioId.Value);
            }
            else
            {
                calculationResult = await _calculatorService.CalculateFromStudyAsync(studyId);
            }

            if (!calculationResult.IsSuccess)
            {
                return ReportGenerationResult.Failure(
                    $"Calculation failed: {calculationResult.ErrorMessage}");
            }

            // Step 4: Generate report files
            var pdfOptions = new ReserveStudyReportOptions
            {
                Title = options.CustomTitle ?? "Reserve Study Funding Plan",
                CommunityName = study.Community?.Name ?? study.Name,
                PreparedBy = study.PreparedBy,
                PreparedDate = DateTime.UtcNow,
                IncludeSummary = true,
                IncludeCashFlowTable = true,
                IncludeAllocation = true,
                IncludeComponents = true
            };

            string? pdfStorageUrl = null;
            string? excelStorageUrl = null;
            long totalFileSize = 0;
            int pageCount = 0;

            // Generate and store PDF
            if (options.GeneratePdf)
            {
                var pdfBytes = _pdfService.GenerateReport(calculationResult, pdfOptions);
                var pdfFileName = $"ReserveStudy_{study.Community?.Name ?? study.Id.ToString()}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                
                var pdfUploadResult = await _storageService.UploadAsync(
                    study.TenantId,
                    studyId,
                    pdfFileName,
                    "application/pdf",
                    pdfBytes);

                pdfStorageUrl = pdfUploadResult.StorageUrl;
                totalFileSize += pdfUploadResult.FileSizeBytes;
                
                // Estimate page count (rough: ~3KB per page for PDF)
                pageCount = Math.Max(1, pdfBytes.Length / 3000);
            }

            // Generate and store Excel
            if (options.GenerateExcel)
            {
                var excelBytes = _excelService.ExportResults(calculationResult, pdfOptions);
                var excelFileName = $"ReserveStudy_{study.Community?.Name ?? study.Id.ToString()}_{DateTime.UtcNow:yyyyMMdd}.xlsx";
                
                var excelUploadResult = await _storageService.UploadAsync(
                    study.TenantId,
                    studyId,
                    excelFileName,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    excelBytes);

                excelStorageUrl = excelUploadResult.StorageUrl;
                totalFileSize += excelUploadResult.FileSizeBytes;
            }

            // Step 5: Determine version number
            var existingReports = await _reportService.GetByTypeAsync(studyId, options.ReportType);
            var maxVersion = existingReports.Any() 
                ? existingReports.Max(r => int.TryParse(r.Version, out var v) ? v : 0) 
                : 0;
            var nextVersion = (maxVersion + 1).ToString();

            // Step 6: Create report record
            var report = new GeneratedReport
            {
                TenantId = study.TenantId,
                ReserveStudyId = studyId,
                Type = options.ReportType,
                Status = ReportStatus.Draft,
                Version = nextVersion,
                Title = options.CustomTitle ?? $"{GetReportTypeDisplayName(options.ReportType)} v{nextVersion}",
                StorageUrl = pdfStorageUrl ?? excelStorageUrl ?? string.Empty,
                ContentType = options.GeneratePdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileSizeBytes = totalFileSize,
                PageCount = pageCount,
                GeneratedByUserId = generatedByUserId,
                GeneratedAt = DateTime.UtcNow,
                Notes = options.Notes,
                OutputFormat = options.GeneratePdf && options.GenerateExcel 
                    ? "PDF,Excel" 
                    : options.GeneratePdf ? "PDF" : "Excel"
            };

            var savedReport = await _reportService.CreateAsync(report);

            // Build result with warnings
            var result = ReportGenerationResult.Success(savedReport, calculationResult);
            
            if (calculationResult.Warnings.Any())
            {
                result.Warnings.AddRange(calculationResult.Warnings);
            }

            if (!calculationResult.IsFullyFunded)
            {
                result.Warnings.Add($"Warning: Funding plan shows deficit in year {calculationResult.FirstDeficitYear}.");
            }

            return result;
        }
        catch (Exception ex)
        {
            return ReportGenerationResult.Failure($"Report generation failed: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ReportGenerationResult> RegenerateReportAsync(
        Guid reportId,
        Guid regeneratedByUserId)
    {
        var existingReport = await _reportService.GetByIdAsync(reportId);
        if (existingReport == null)
        {
            return ReportGenerationResult.Failure("Report not found.");
        }

        // Generate new report with same options
        var options = new ReportGenerationOptions
        {
            ReportType = existingReport.Type,
            CustomTitle = existingReport.Title,
            GeneratePdf = existingReport.ContentType?.Contains("pdf") ?? true,
            GenerateExcel = existingReport.OutputFormat?.Contains("Excel") ?? false,
            Notes = $"Regenerated from v{existingReport.Version}"
        };

        return await GenerateReportAsync(
            existingReport.ReserveStudyId,
            regeneratedByUserId,
            options);
    }

    /// <inheritdoc />
    public async Task<ReserveStudyResult?> PreviewCalculationAsync(Guid studyId)
    {
        try
        {
            var result = await _calculatorService.CalculateFromStudyAsync(studyId);
            return result.IsSuccess ? result : null;
        }
        catch
        {
            return null;
        }
    }

    private static string GetReportTypeDisplayName(ReportType type) => type switch
    {
        ReportType.Draft => "Draft Report",
        ReportType.Final => "Final Report",
        ReportType.FundingPlan => "Funding Plan",
        ReportType.ExecutiveSummary => "Executive Summary",
        ReportType.FullReport => "Full Report",
        ReportType.ComponentInventory => "Component Inventory",
        ReportType.UpdateReport => "Update Report",
        ReportType.Addendum => "Addendum",
        ReportType.CorrectionNotice => "Correction Notice",
        _ => type.ToString()
    };
}
