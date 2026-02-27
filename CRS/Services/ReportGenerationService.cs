using Horizon.Core.ReserveCalculator.Models;
using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Workflow;
using Horizon.Services.Interfaces;
using Horizon.Services.NarrativeReport;
using Horizon.Services.ReserveCalculator;
using Horizon.Services.Storage;
using Horizon.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Services;

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
    private readonly IReserveStudyWorkflowService _workflowService;
    private readonly INarrativeService _narrativeService;
    private readonly IReportContextBuilder _contextBuilder;
    private readonly IReserveStudyHtmlComposer _htmlComposer;
    private readonly IPdfConverter _pdfConverter;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ReportGenerationService> _logger;

    public ReportGenerationService(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        IReserveStudyCalculatorService calculatorService,
        IReserveStudyPdfService pdfService,
        IReserveStudyExcelService excelService,
        IDocumentStorageService storageService,
        IGeneratedReportService reportService,
        IReserveStudyWorkflowService workflowService,
        INarrativeService narrativeService,
        IReportContextBuilder contextBuilder,
        IReserveStudyHtmlComposer htmlComposer,
        IPdfConverter pdfConverter,
        ITenantContext tenantContext,
        ILogger<ReportGenerationService> logger)
    {
        _dbFactory = dbFactory;
        _calculatorService = calculatorService;
        _pdfService = pdfService;
        _excelService = excelService;
        _storageService = storageService;
        _reportService = reportService;
        _workflowService = workflowService;
        _narrativeService = narrativeService;
        _contextBuilder = contextBuilder;
        _htmlComposer = htmlComposer;
        _pdfConverter = pdfConverter;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public StudyStatus MinimumRequiredStatus => StudyStatus.FundingPlanReady;

    /// <inheritdoc />
    public async Task<bool> CanGenerateReportAsync(Guid studyId)
    {
        var validation = await ValidateStudyDataAsync(studyId);
        return validation.IsReady;
    }

    /// <inheritdoc />
    public async Task<StudyDataValidationResult> ValidateStudyDataAsync(Guid studyId)
    {
        var result = new StudyDataValidationResult();
        
        await using var context = await _dbFactory.CreateDbContextAsync();
        
        var study = await context.ReserveStudies
            .Include(s => s.StudyRequest)
            .Include(s => s.FinancialInfo)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.BuildingElement)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.ServiceContact)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.CommonElement)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.ServiceContact)
            .Include(s => s.ReserveStudyAdditionalElements!)
                .ThenInclude(e => e.ServiceContact)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == studyId);

        if (study == null)
        {
            result.Errors.Add("Reserve study not found.");
            return result;
        }

        // Check workflow status
        if (study.StudyRequest == null || study.StudyRequest.CurrentStatus < MinimumRequiredStatus)
        {
            result.Errors.Add($"Study must be at '{MinimumRequiredStatus}' stage or later. Current: {study.StudyRequest?.CurrentStatus ?? StudyStatus.RequestCreated}");
        }

        // Check financial data
        ValidateFinancialData(study, result);

        // Check element data
        ValidateElementData(study, result);

        // Check service contacts
        ValidateServiceContacts(study, result);

        return result;
    }

    private void ValidateFinancialData(ReserveStudy study, StudyDataValidationResult result)
    {
        if (study.FinancialInfo == null)
        {
            result.Errors.Add("Financial information has not been submitted.");
            result.MissingFinancialData.Add("All financial data");
            return;
        }

        // Check required financial fields
        if (!study.FinancialInfo.JanuaryFirstReserveBalance.HasValue && 
            !study.CurrentReserveFunds.HasValue)
        {
            result.Warnings.Add("Current reserve fund balance is not set.");
            result.MissingFinancialData.Add("Current Reserve Balance");
        }

        if (!study.FinancialInfo.BudgetedContributionCurrentYear.HasValue &&
            !study.MonthlyReserveContribution.HasValue)
        {
            result.Warnings.Add("Monthly/annual contribution is not set.");
            result.MissingFinancialData.Add("Reserve Contribution");
        }
    }

    private void ValidateElementData(ReserveStudy study, StudyDataValidationResult result)
    {
        var allElements = new List<(string Name, string Type, bool HasUsefulLife, bool HasRemainingLife)>();

        // Building elements
        if (study.ReserveStudyBuildingElements != null)
        {
            foreach (var element in study.ReserveStudyBuildingElements)
            {
                var name = element.BuildingElement?.Name ?? "Unknown Building Element";
                var hasUsefulLife = element.MinUsefulLifeOptionId.HasValue || 
                                    element.MaxUsefulLifeOptionId.HasValue;
                var hasRemainingLife = element.RemainingLifeYears.HasValue || 
                                       element.LastServiced.HasValue;
                allElements.Add((name, "Building", hasUsefulLife, hasRemainingLife));
            }
        }

        // Common elements
        if (study.ReserveStudyCommonElements != null)
        {
            foreach (var element in study.ReserveStudyCommonElements)
            {
                var name = element.ElementName ?? element.CommonElement?.Name ?? "Unknown Common Element";
                var hasUsefulLife = element.MinUsefulLifeOptionId.HasValue || 
                                    element.MaxUsefulLifeOptionId.HasValue;
                var hasRemainingLife = element.RemainingLifeYears.HasValue || 
                                       element.LastServiced.HasValue;
                allElements.Add((name, "Common", hasUsefulLife, hasRemainingLife));
            }
        }

        // Additional elements
        if (study.ReserveStudyAdditionalElements != null)
        {
            foreach (var element in study.ReserveStudyAdditionalElements)
            {
                var name = element.Name ?? "Unknown Additional Element";
                var hasUsefulLife = element.MinUsefulLifeOptionId.HasValue || 
                                    element.MaxUsefulLifeOptionId.HasValue;
                var hasRemainingLife = element.RemainingLifeYears.HasValue || 
                                       element.LastServiced.HasValue;
                allElements.Add((name, "Additional", hasUsefulLife, hasRemainingLife));
            }
        }

        if (!allElements.Any())
        {
            result.TotalElementCount = 0;
            result.Errors.Add("No property elements have been added to this study. At least one element is required.");
            return;
        }

        result.TotalElementCount = allElements.Count;

        // Check for missing useful life
        var missingUsefulLife = allElements.Where(e => !e.HasUsefulLife).ToList();
        if (missingUsefulLife.Any())
        {
            foreach (var element in missingUsefulLife)
            {
                result.MissingElementData.Add($"{element.Name} ({element.Type}): Missing Useful Life");
            }
            result.Warnings.Add($"{missingUsefulLife.Count} element(s) are missing Useful Life data.");
        }

        // Check for missing remaining life
        var missingRemainingLife = allElements.Where(e => !e.HasRemainingLife).ToList();
        if (missingRemainingLife.Any())
        {
            foreach (var element in missingRemainingLife)
            {
                result.MissingElementData.Add($"{element.Name} ({element.Type}): Missing Remaining Life");
            }
            result.Warnings.Add($"{missingRemainingLife.Count} element(s) are missing Remaining Life data.");
        }
    }

    private void ValidateServiceContacts(ReserveStudy study, StudyDataValidationResult result)
    {
        var elementsNeedingContacts = new List<(string Name, string Type, bool HasContact)>();

        // Building elements that need service
        if (study.ReserveStudyBuildingElements != null)
        {
            foreach (var element in study.ReserveStudyBuildingElements.Where(e => e.BuildingElement?.NeedsService == true))
            {
                var name = element.BuildingElement?.Name ?? "Unknown";
                var hasContact = element.ServiceContact != null && 
                                 (!string.IsNullOrEmpty(element.ServiceContact.CompanyName) ||
                                  !string.IsNullOrEmpty(element.ServiceContact.Phone) ||
                                  !string.IsNullOrEmpty(element.ServiceContact.Email));
                elementsNeedingContacts.Add((name, "Building", hasContact));
            }
        }

        // Common elements that need service
        if (study.ReserveStudyCommonElements != null)
        {
            foreach (var element in study.ReserveStudyCommonElements.Where(e => e.CommonElement?.NeedsService == true))
            {
                var name = element.ElementName ?? element.CommonElement?.Name ?? "Unknown";
                var hasContact = element.ServiceContact != null &&
                                 (!string.IsNullOrEmpty(element.ServiceContact.CompanyName) ||
                                  !string.IsNullOrEmpty(element.ServiceContact.Phone) ||
                                  !string.IsNullOrEmpty(element.ServiceContact.Email));
                elementsNeedingContacts.Add((name, "Common", hasContact));
            }
        }

        // Additional elements that need service
        if (study.ReserveStudyAdditionalElements != null)
        {
            foreach (var element in study.ReserveStudyAdditionalElements.Where(e => e.NeedsService))
            {
                var name = element.Name ?? "Unknown";
                var hasContact = element.ServiceContact != null &&
                                 (!string.IsNullOrEmpty(element.ServiceContact.CompanyName) ||
                                  !string.IsNullOrEmpty(element.ServiceContact.Phone) ||
                                  !string.IsNullOrEmpty(element.ServiceContact.Email));
                elementsNeedingContacts.Add((name, "Additional", hasContact));
            }
        }

        var missingContacts = elementsNeedingContacts.Where(e => !e.HasContact).ToList();
        if (missingContacts.Any())
        {
            foreach (var element in missingContacts)
            {
                result.MissingServiceContacts.Add($"{element.Name} ({element.Type})");
            }
            result.Warnings.Add($"{missingContacts.Count} element(s) require service contacts but are missing contact information.");
        }
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
            // Step 1: Validate study data completeness
            var validation = await ValidateStudyDataAsync(studyId);
            
            if (!validation.IsReady)
            {
                var errorMessage = string.Join("; ", validation.Errors);
                return ReportGenerationResult.Failure(
                    $"Study data is incomplete: {errorMessage}");
            }

            // Step 2: Load study data
            await using var context = await _dbFactory.CreateDbContextAsync();
            var study = await context.ReserveStudies
                .Include(s => s.Community)
                .Include(s => s.CurrentProposal)
                .FirstOrDefaultAsync(s => s.Id == studyId);

            if (study == null)
            {
                return ReportGenerationResult.Failure("Reserve study not found.");
            }

            // Step 2b: Load narrative if available (for full reports with narrative sections)
            var narrative = await _narrativeService.GetByStudyIdAsync(studyId);
            // Note: For FullReport type, use GenerateNarrativeReportAsync which handles narrative integration
            // For FundingPlan reports, narrative is available but not currently embedded in the PDF

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

            // Step 4: Generate report files with options based on report type
            var pdfOptions = BuildReportOptions(options.ReportType, study, options.CustomTitle);

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
                ExcelStorageUrl = excelStorageUrl,
                ContentType = options.GeneratePdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileSizeBytes = totalFileSize,
                PageCount = pageCount,
                GeneratedByUserId = generatedByUserId,
                GeneratedAt = DateTime.UtcNow,
                Notes = options.Notes,
                SupersedesReportId = options.SupersedesReportId, // Link to superseded report
                OutputFormat = options.GeneratePdf && options.GenerateExcel 
                    ? "PDF,Excel" 
                    : options.GeneratePdf ? "PDF" : "Excel"
            };

            // If this report supersedes another, mark the old report as Superseded
            if (options.SupersedesReportId.HasValue)
            {
                await _reportService.MarkAsSupersededAsync(options.SupersedesReportId.Value);
                _logger.LogInformation("Report {OldReportId} marked as superseded by new report", options.SupersedesReportId.Value);
            }

            var savedReport = await _reportService.CreateAsync(report);

            // Advance workflow status when FundingPlan report is generated
            if (options.ReportType == ReportType.FundingPlan)
            {
                await _workflowService.TryTransitionStudyAsync(studyId, StudyStatus.FundingPlanInProcess);
            }

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

        /// <summary>
        /// Builds PDF report options based on the report type.
        /// Different report types have different content and formatting requirements.
        /// </summary>
        private static ReserveStudyReportOptions BuildReportOptions(ReportType reportType, ReserveStudy study, string? customTitle)
        {
            var communityName = study.Community?.Name ?? study.Name;

            return reportType switch
            {
                // Executive Summary: Condensed 1-2 page report with key metrics only
                ReportType.ExecutiveSummary => new ReserveStudyReportOptions
                {
                    Title = customTitle ?? "Executive Summary",
                    CommunityName = communityName,
                    PreparedBy = study.PreparedBy,
                    PreparedDate = DateTime.UtcNow,
                    IsExecutiveSummary = true,
                    IncludeSummary = true,
                    IncludeFundStatus = true,
                    IncludeFirstYearSummary = true,
                    IncludeCashFlowTable = false,      // No detailed cash flow in exec summary
                    IncludeAllocation = false,         // No allocation breakdown
                    IncludeComponents = false,         // No component details
                    MaxYearsToShow = 5,                // Show only 5 years if any table is included
                    IsDraft = false
                },

                // Draft: Full report with DRAFT watermark
                ReportType.Draft => new ReserveStudyReportOptions
                {
                    Title = customTitle ?? "Reserve Study Funding Plan - DRAFT",
                    CommunityName = communityName,
                    PreparedBy = study.PreparedBy,
                    PreparedDate = DateTime.UtcNow,
                    IsDraft = true,
                    IncludeSummary = true,
                    IncludeFundStatus = true,
                    IncludeFirstYearSummary = true,
                    IncludeCashFlowTable = true,
                    IncludeAllocation = true,
                    IncludeComponents = true,
                    MaxYearsToShow = null              // Show all years
                },

                // Funding Plan (default): Full detailed report
                ReportType.FundingPlan or ReportType.Final or _ => new ReserveStudyReportOptions
                {
                    Title = customTitle ?? "Reserve Study Funding Plan",
                    CommunityName = communityName,
                    PreparedBy = study.PreparedBy,
                    PreparedDate = DateTime.UtcNow,
                    IsDraft = false,
                    IncludeSummary = true,
                    IncludeFundStatus = true,
                    IncludeFirstYearSummary = true,
                    IncludeCashFlowTable = true,
                    IncludeAllocation = true,
                    IncludeComponents = true,
                    MaxYearsToShow = null              // Show all years
                }
            };
        }

        /// <inheritdoc />
        public async Task<ReportGenerationResult> GenerateNarrativeReportAsync(
            Guid studyId,
            Guid generatedByUserId,
            NarrativeReportOptions? options = null)
        {
            options ??= new NarrativeReportOptions();

            try
            {
                _logger.LogInformation("Generating narrative report for study {StudyId}", studyId);

                // Step 1: Validate study data
                var validation = await ValidateStudyDataAsync(studyId);
                if (!validation.IsReady)
                {
                    var errorMessage = string.Join("; ", validation.Errors);
                    return ReportGenerationResult.Failure($"Study data is incomplete: {errorMessage}");
                }

                // Step 2: Build the report context (aggregates all data)
                var contextOptions = new ReportContextOptions
                {
                    IncludePhotos = options.IncludePhotos,
                    IncludeCalculatedOutputs = options.IncludeCalculationTables,
                    ReportTitleOverride = options.Title
                };

                var context = await _contextBuilder.BuildContextAsync(studyId, contextOptions);

                // Step 3: Compose the HTML document
                var compositionOptions = new NarrativeCompositionOptions
                {
                    IncludeDocumentWrapper = true,
                    IncludePrintCss = true,
                    IncludePageBreaks = true,
                    ExcludedSections = options.ExcludedSections
                };

                var html = await _htmlComposer.ComposeAsync(
                    context,
                    _tenantContext.TenantId,
                    compositionOptions);

                _logger.LogDebug("Composed HTML: {Length} characters", html.Length);

                // Step 4: Convert HTML to PDF
                var pdfOptions = new PdfConversionOptions
                {
                    PageSize = options.PageSize,
                    Orientation = options.Landscape ? PdfOrientation.Landscape : PdfOrientation.Portrait,
                    Title = options.Title ?? $"Reserve Study - {context.Association.Name}",
                    Author = context.Branding.CompanyName,
                    ShowPageNumbers = true
                };

                var pdfBytes = await _pdfConverter.ConvertToPdfAsync(html, pdfOptions);

                _logger.LogInformation("Generated PDF: {Size} bytes", pdfBytes.Length);

                // Step 5: Upload to storage
                await using var dbContext = await _dbFactory.CreateDbContextAsync();
                var study = await dbContext.ReserveStudies.FindAsync(studyId);
                if (study == null)
                {
                    return ReportGenerationResult.Failure("Reserve study not found.");
                }

                var fileName = $"NarrativeReport_{context.Association.Name?.Replace(" ", "_") ?? studyId.ToString()}_{DateTime.UtcNow:yyyyMMdd}.pdf";
                var uploadResult = await _storageService.UploadAsync(
                    study.TenantId,
                    studyId,
                    fileName,
                    "application/pdf",
                    pdfBytes);

                // Step 6: Determine version
                var existingReports = await _reportService.GetByTypeAsync(studyId, ReportType.FullReport);
                var maxVersion = existingReports.Any()
                    ? existingReports.Max(r => int.TryParse(r.Version, out var v) ? v : 0)
                    : 0;
                var nextVersion = (maxVersion + 1).ToString();

                // Step 7: Create report record
                var report = new GeneratedReport
                {
                    TenantId = study.TenantId,
                    ReserveStudyId = studyId,
                    Type = ReportType.FullReport,
                    Status = ReportStatus.Draft,
                    Version = nextVersion,
                    Title = options.Title ?? $"Full Narrative Report v{nextVersion}",
                    StorageUrl = uploadResult.StorageUrl,
                    ContentType = "application/pdf",
                    FileSizeBytes = uploadResult.FileSizeBytes,
                    PageCount = EstimatePageCount(pdfBytes.Length),
                    GeneratedByUserId = generatedByUserId,
                    GeneratedAt = DateTime.UtcNow,
                    TemplateUsed = "NarrativeTemplate",
                    OutputFormat = "PDF",
                    Notes = options.Notes
                };

                var savedReport = await _reportService.CreateAsync(report);

                _logger.LogInformation("Narrative report created: {ReportId}", savedReport.Id);

                // Step 8: Advance workflow status
                // Progress through narrative workflow stages
                await AdvanceNarrativeWorkflowAsync(studyId);

                // Get calculation result for the response
                var calcResult = await _calculatorService.CalculateFromStudyAsync(studyId);

                return ReportGenerationResult.Success(savedReport, calcResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate narrative report for study {StudyId}", studyId);
                return ReportGenerationResult.Failure($"Narrative report generation failed: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public async Task<string> PreviewNarrativeHtmlAsync(Guid studyId, string? sectionKey = null)
        {
            try
            {
                // Build context
                var context = await _contextBuilder.BuildContextAsync(studyId);

                if (!string.IsNullOrEmpty(sectionKey))
                {
                    // Render single section
                    return await _htmlComposer.ComposeSectionAsync(
                        sectionKey,
                        context,
                        _tenantContext.TenantId);
                }

                // Render full document with preview flag
                var options = new NarrativeCompositionOptions
                {
                    IncludeDocumentWrapper = true,
                    IncludePrintCss = true,
                    IsPreview = true
                };

                return await _htmlComposer.ComposeAsync(
                    context,
                    _tenantContext.TenantId,
                    options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preview narrative HTML for study {StudyId}", studyId);
                throw;
            }
        }

            private static int EstimatePageCount(int pdfSizeBytes)
            {
                // Rough estimate: ~3-5KB per page for text-heavy PDFs
                // With images it could be much larger per page
                return Math.Max(1, pdfSizeBytes / 4000);
            }

                /// <summary>
                /// Advances the workflow through narrative-related stages when a narrative report is generated.
                /// Follows the proper state machine transitions.
                /// </summary>
                private async Task AdvanceNarrativeWorkflowAsync(Guid studyId)
                {
                    try
                    {
                        // Get current status
                        await using var context = await _dbFactory.CreateDbContextAsync();
                        var studyRequest = await context.StudyRequests.FirstOrDefaultAsync(r => r.Id == studyId);

                        if (studyRequest == null)
                        {
                            _logger.LogWarning("No StudyRequest found for study {StudyId}, cannot advance workflow", studyId);
                            return;
                        }

                        var currentStatus = studyRequest.CurrentStatus;
                        _logger.LogInformation("Current workflow status for study {StudyId}: {Status}", studyId, currentStatus);

                        // Define the sequence of transitions needed to reach NarrativeComplete
                        // based on current status. We advance one step at a time following the state machine.
                        var transitionPath = GetTransitionPathToNarrativeComplete(currentStatus);

                        if (transitionPath.Count == 0)
                        {
                            _logger.LogDebug("No workflow advancement needed for study {StudyId} at status {Status}", 
                                studyId, currentStatus);
                            return;
                        }

                        // Execute each transition in sequence
                        foreach (var targetStatus in transitionPath)
                        {
                            var success = await _workflowService.TryTransitionStudyAsync(studyId, targetStatus);
                            if (success)
                            {
                                _logger.LogInformation("Advanced workflow for study {StudyId} to {Status}", 
                                    studyId, targetStatus);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to advance workflow for study {StudyId} to {Status}, stopping advancement", 
                                    studyId, targetStatus);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Don't fail the report generation if workflow update fails
                        _logger.LogError(ex, "Error advancing workflow for study {StudyId}", studyId);
                    }
                }

                /// <summary>
                /// Gets the sequence of workflow transitions needed to reach NarrativeComplete from the current status.
                /// </summary>
                private static List<StudyStatus> GetTransitionPathToNarrativeComplete(StudyStatus currentStatus)
                {
                    var path = new List<StudyStatus>();

                    // Map current status to the path needed
                    switch (currentStatus)
                    {
                        case StudyStatus.FundingPlanReady:
                            path.Add(StudyStatus.FundingPlanInProcess);
                            path.Add(StudyStatus.FundingPlanComplete);
                            path.Add(StudyStatus.NarrativeReady);
                            path.Add(StudyStatus.NarrativeInProcess);
                            path.Add(StudyStatus.NarrativeComplete);
                            break;

                        case StudyStatus.FundingPlanInProcess:
                            path.Add(StudyStatus.FundingPlanComplete);
                            path.Add(StudyStatus.NarrativeReady);
                            path.Add(StudyStatus.NarrativeInProcess);
                            path.Add(StudyStatus.NarrativeComplete);
                            break;

                        case StudyStatus.FundingPlanComplete:
                            path.Add(StudyStatus.NarrativeReady);
                            path.Add(StudyStatus.NarrativeInProcess);
                            path.Add(StudyStatus.NarrativeComplete);
                            break;

                        case StudyStatus.NarrativeReady:
                            path.Add(StudyStatus.NarrativeInProcess);
                            path.Add(StudyStatus.NarrativeComplete);
                            break;

                        case StudyStatus.NarrativeInProcess:
                            path.Add(StudyStatus.NarrativeComplete);
                            break;

                        case StudyStatus.NarrativeComplete:
                            // Already at target, advance to print ready
                            path.Add(StudyStatus.NarrativePrintReady);
                            break;

                        // Already past narrative completion - no advancement needed
                        default:
                            break;
                    }

                    return path;
                }
            }
