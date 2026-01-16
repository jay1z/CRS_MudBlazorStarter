using CRS.Data;
using CRS.Models;
using CRS.Models.NarrativeTemplates;
using CRS.Services.Interfaces;
using CRS.Services.ReserveCalculator;
using CRS.Services.Storage;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CRS.Services.NarrativeReport;

/// <summary>
/// Implementation of <see cref="IReportContextBuilder"/>.
/// Aggregates data from reserve study, community, tenant, and related entities.
/// </summary>
public class ReportContextBuilder : IReportContextBuilder
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITenantContext _tenantContext;
    private readonly ISiteVisitPhotoService _photoService;
    private readonly IPhotoStorageService _photoStorageService;
    private readonly IReserveStudyCalculatorService _calculatorService;
    private readonly ILogoStorageService _logoStorageService;
    private readonly INarrativeService _narrativeService;
    private readonly ILogger<ReportContextBuilder> _logger;

    public ReportContextBuilder(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ISiteVisitPhotoService photoService,
        IPhotoStorageService photoStorageService,
        IReserveStudyCalculatorService calculatorService,
        ILogoStorageService logoStorageService,
        INarrativeService narrativeService,
        ILogger<ReportContextBuilder> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _photoService = photoService;
        _photoStorageService = photoStorageService;
        _calculatorService = calculatorService;
        _logoStorageService = logoStorageService;
        _narrativeService = narrativeService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<ReserveStudyReportContext> BuildContextAsync(
        Guid reserveStudyId,
        CancellationToken cancellationToken = default)
    {
        return BuildContextAsync(reserveStudyId, new ReportContextOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ReserveStudyReportContext> BuildContextAsync(
        Guid reserveStudyId,
        ReportContextOptions options,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Load the reserve study with all related data
        var study = await context.ReserveStudies
            .AsNoTracking()
            .Include(s => s.Community)
                .ThenInclude(c => c!.PhysicalAddress)
            .Include(s => s.FinancialInfo)
            .Include(s => s.StudyRequest)
            .Include(s => s.CurrentProposal)
            .Include(s => s.Specialist)
            .Include(s => s.Contact)
            .Include(s => s.PropertyManager)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.ServiceContact)
            .Include(s => s.ReserveStudyBuildingElements!)
                .ThenInclude(e => e.BuildingElement)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.ServiceContact)
            .Include(s => s.ReserveStudyCommonElements!)
                .ThenInclude(e => e.CommonElement)
            .Include(s => s.ReserveStudyAdditionalElements!)
                .ThenInclude(e => e.ServiceContact)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == reserveStudyId, cancellationToken)
            ?? throw new InvalidOperationException($"Reserve study {reserveStudyId} not found.");

        // Load tenant for branding
        Models.Tenant? tenant = null;
        if (_tenantContext.TenantId.HasValue)
        {
            tenant = await context.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId.Value, cancellationToken);
        }

        // Log source data for debugging
        _logger.LogDebug("Building context for study {StudyId}", reserveStudyId);
        _logger.LogDebug("  Community: {Name}", study.Community?.Name ?? "NULL");
        _logger.LogDebug("  FinancialInfo: {HasData}", study.FinancialInfo != null ? "Present" : "NULL");
        if (study.FinancialInfo != null)
        {
            _logger.LogDebug("    JanuaryFirstReserveBalance: {Value}", study.FinancialInfo.JanuaryFirstReserveBalance);
            _logger.LogDebug("    BudgetedContributionCurrentYear: {Value}", study.FinancialInfo.BudgetedContributionCurrentYear);
            _logger.LogDebug("    TotalNumberOfUnits: {Value}", study.FinancialInfo.TotalNumberOfUnits);
            _logger.LogDebug("    InterestRateOnReserveFunds: {Value}", study.FinancialInfo.InterestRateOnReserveFunds);
        }
        _logger.LogDebug("  StudyRequest: {HasData}", study.StudyRequest != null ? "Present" : "NULL");
        if (study.StudyRequest != null)
        {
            _logger.LogDebug("    SiteVisitCompletedAt: {Value}", study.StudyRequest.SiteVisitCompletedAt);
            _logger.LogDebug("    CurrentStatus: {Value}", study.StudyRequest.CurrentStatus);
        }
        _logger.LogDebug("  CurrentProposal: {HasData}", study.CurrentProposal != null ? "Present" : "NULL");
        _logger.LogDebug("  Specialist: {Name}", study.Specialist?.FullName ?? "NULL");
        _logger.LogDebug("  Tenant: {Name}", tenant?.Name ?? "NULL");

        // Build the context
        var reportContext = new ReserveStudyReportContext
        {
            Association = BuildAssociationInfo(study),
            Study = BuildStudyInfo(study, options),
            FinancialAssumptions = BuildFinancialAssumptions(study),
            Branding = await BuildBrandingInfoAsync(tenant, cancellationToken),
            Signatories = BuildSignatories(study)
        };

        // Add photos if requested
        if (options.IncludePhotos)
        {
            reportContext.Photos = await BuildPhotosAsync(reserveStudyId, cancellationToken);
            _logger.LogDebug("  Photos loaded: {Count}", reportContext.Photos.Count);
        }

        // Add vendors if requested
        if (options.IncludeVendors)
        {
            reportContext.Vendors = BuildVendors(study);
        }

        // Add glossary if requested
        if (options.IncludeGlossary)
        {
            reportContext.GlossaryTerms = GetDefaultGlossaryTerms();
        }

        // Add calculated outputs if requested
        if (options.IncludeCalculatedOutputs)
        {
            reportContext.CalculatedOutputs = await BuildCalculatedOutputsAsync(study, cancellationToken);
            _logger.LogDebug("  Calculated outputs:");
            _logger.LogDebug("    FundStatusLabel: {Value}", reportContext.CalculatedOutputs.FundStatusLabel);
            _logger.LogDebug("    PercentFunded: {Value}", reportContext.CalculatedOutputs.PercentFunded);
            _logger.LogDebug("    IdealBalance: {Value}", reportContext.CalculatedOutputs.IdealBalance);
            _logger.LogDebug("    FirstYear.StartingBalance: {Value}", reportContext.CalculatedOutputs.FirstYear.StartingBalance);
            _logger.LogDebug("    FirstYear.Contribution: {Value}", reportContext.CalculatedOutputs.FirstYear.Contribution);
            _logger.LogDebug("    FirstYear.EndingBalance: {Value}", reportContext.CalculatedOutputs.FirstYear.EndingBalance);
        }

            // Add info furnished items
            reportContext.InfoFurnished = BuildInfoFurnished(study);

            // Load user-edited narrative content if available
            reportContext.UserNarrative = await BuildNarrativeContentAsync(reserveStudyId, cancellationToken);
            if (reportContext.UserNarrative?.HasUserContent == true)
            {
                _logger.LogDebug("  User narrative content loaded for study {StudyId}", reserveStudyId);
            }

            _logger.LogDebug("Built report context for study {StudyId}", reserveStudyId);
            return reportContext;
        }

    private static AssociationInfo BuildAssociationInfo(ReserveStudy study)
    {
        var community = study.Community;
        var address = community?.PhysicalAddress;
        
        return new AssociationInfo
        {
            Name = community?.Name ?? "Unknown Community",
            Address = address?.Street,
            City = address?.City,
            State = address?.State,
            Zip = address?.Zip,
            CommunityType = null, // Community doesn't have CommunityType in current model
            UnitCount = community?.NumberOfUnits ?? study.FinancialInfo?.TotalNumberOfUnits,
            EstablishedYear = community?.YearBuilt,
            PhoneNumber = community?.PhoneNumber,
            Email = community?.Email
        };
    }

    private static StudyInfo BuildStudyInfo(ReserveStudy study, ReportContextOptions options)
    {
        var proposal = study.CurrentProposal;
        var financialInfo = study.FinancialInfo;
        var studyRequest = study.StudyRequest;
        var fiscalStartMonth = financialInfo?.FiscalYearStartMonth ?? 1;

        // Determine inspection date from multiple sources
        // Priority 1: StudyRequest.SiteVisitCompletedAt (actual site visit date)
        // Priority 2: Proposal approval date as fallback
        // Priority 3: Study creation date as last resort
        DateTime? inspectionDate = studyRequest?.SiteVisitCompletedAt 
            ?? proposal?.DateApproved 
            ?? study.DateCreated;

        return new StudyInfo
        {
            StudyType = proposal?.ServiceLevel, // Use ServiceLevel as study type indicator
            ReportTitle = options.ReportTitleOverride ?? $"Reserve Study for {study.Community?.Name ?? "Community"}",
            InspectionDate = inspectionDate,
            EffectiveDate = proposal?.ProposalDate ?? DateTime.UtcNow,
            FiscalYearStart = GetFiscalYearStart(fiscalStartMonth),
            FiscalYearEnd = GetFiscalYearEnd(fiscalStartMonth),
            PreparedBy = study.Specialist?.FullName ?? study.Specialist?.Email ?? study.PreparedBy,
            ReportDate = options.ReportDateOverride ?? DateTime.UtcNow,
            ProjectionYears = 30 // Standard projection period
        };
    }

    private static DateTime GetFiscalYearStart(int startMonth)
    {
        var now = DateTime.UtcNow;
        var year = now.Month >= startMonth ? now.Year : now.Year - 1;
        return new DateTime(year, startMonth, 1);
    }

    private static DateTime GetFiscalYearEnd(int startMonth)
    {
        return GetFiscalYearStart(startMonth).AddYears(1).AddDays(-1);
    }

    private static FinancialAssumptions BuildFinancialAssumptions(ReserveStudy study)
    {
        var financialInfo = study.FinancialInfo;
        return new FinancialAssumptions
        {
            InflationRate = 0.03m, // Default 3% - could be stored in proposal or settings
            InterestRate = financialInfo?.InterestRateOnReserveFunds ?? 0.02m,
            TaxTreatment = null, // Could be added to model
            MinimumComponentThreshold = null,
            OperatingBudgetTotal = financialInfo?.OperatingBudgetCurrentYear,
            ContingencyPercent = null
        };
    }

    private async Task<BrandingInfo> BuildBrandingInfoAsync(Models.Tenant? tenant, CancellationToken cancellationToken)
    {
        if (tenant == null)
        {
            return new BrandingInfo
            {
                CompanyName = "Reserve Study Provider",
                FooterText = "Professional Reserve Study Services"
            };
        }

        // Parse branding JSON if available
        BrandingData? brandingData = null;
        if (!string.IsNullOrWhiteSpace(tenant.BrandingJson))
        {
            try
            {
                brandingData = JsonSerializer.Deserialize<BrandingData>(tenant.BrandingJson);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse branding JSON for tenant {TenantId}", tenant.Id);
            }
        }

        // Get logo URL from blob storage (this is where tenant logos are actually stored)
        string? logoUrl = null;
        try
        {
            logoUrl = await _logoStorageService.GetLogoUrlAsync(tenant.Id, cancellationToken);
            _logger.LogDebug("Retrieved logo URL for tenant {TenantId}: {LogoUrl}", tenant.Id, logoUrl ?? "(none)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve logo URL for tenant {TenantId}", tenant.Id);
        }

        // Fallback to branding JSON if logo not found in storage
        logoUrl ??= brandingData?.LogoUrl ?? brandingData?.CompanyLogoUrl;

            return new BrandingInfo
            {
                CompanyName = tenant.Name,
                Phone = brandingData?.CompanyPhone ?? brandingData?.Phone,
                Email = brandingData?.CompanyEmail ?? brandingData?.Email,
                Website = brandingData?.CompanyWebsite ?? brandingData?.Website,
                Address = brandingData?.CompanyAddress ?? brandingData?.Address,
                LogoUrl = logoUrl,
                CoverImageUrl = brandingData?.CoverImageUrl,
                FooterText = brandingData?.FooterText,
                Tagline = brandingData?.CompanyTagline ?? brandingData?.Tagline
            };
        }

        private async Task<NarrativeContent?> BuildNarrativeContentAsync(Guid reserveStudyId, CancellationToken cancellationToken)
        {
            try
            {
                var narrative = await _narrativeService.GetByStudyIdAsync(reserveStudyId, cancellationToken);

                if (narrative == null)
                {
                    _logger.LogDebug("No narrative found for study {StudyId}", reserveStudyId);
                    return null;
                }

                // Check if narrative has any meaningful content
                var hasContent = !string.IsNullOrWhiteSpace(narrative.ExecutiveSummary) ||
                                 !string.IsNullOrWhiteSpace(narrative.Introduction) ||
                                 !string.IsNullOrWhiteSpace(narrative.PropertyDescription) ||
                                 !string.IsNullOrWhiteSpace(narrative.Methodology) ||
                                 !string.IsNullOrWhiteSpace(narrative.Findings) ||
                                 !string.IsNullOrWhiteSpace(narrative.FundingAnalysis) ||
                                 !string.IsNullOrWhiteSpace(narrative.Recommendations) ||
                                 !string.IsNullOrWhiteSpace(narrative.Conclusion);

                if (!hasContent)
                {
                    _logger.LogDebug("Narrative for study {StudyId} exists but has no content", reserveStudyId);
                    return new NarrativeContent { HasUserContent = false };
                }

                _logger.LogDebug("Loading user narrative content for study {StudyId}, status: {Status}", 
                    reserveStudyId, narrative.Status);

                return new NarrativeContent
                {
                    HasUserContent = true,
                    ExecutiveSummary = narrative.ExecutiveSummary,
                    Introduction = narrative.Introduction,
                    PropertyDescription = narrative.PropertyDescription,
                    Methodology = narrative.Methodology,
                    Findings = narrative.Findings,
                    FundingAnalysis = narrative.FundingAnalysis,
                    Recommendations = narrative.Recommendations,
                    Conclusion = narrative.Conclusion,
                    AdditionalNotes = narrative.AdditionalNotes
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load narrative content for study {StudyId}", reserveStudyId);
                return null;
            }
        }

        private static List<SignatoryInfo> BuildSignatories(ReserveStudy study)
    {
        var signatories = new List<SignatoryInfo>();

        // Add specialist as primary signatory
        if (study.Specialist != null)
        {
            signatories.Add(new SignatoryInfo
            {
                Name = study.Specialist.FullName ?? study.Specialist.Email ?? "Unknown",
                Title = "Reserve Specialist",
                Credentials = null, // Could be added to ApplicationUser
                SignatureDate = DateTime.UtcNow
            });
        }

        return signatories;
    }

    private async Task<List<PhotoItem>> BuildPhotosAsync(Guid studyId, CancellationToken ct)
    {
        try
        {
            var photos = await _photoService.GetForReportAsync(studyId, ct);

            // Generate SAS URLs for each photo (valid for 24 hours for PDF generation)
            var sasValidity = TimeSpan.FromHours(24);

            return photos.Select(p => new PhotoItem
            {
                // Use SAS URL for actual access instead of raw storage URL
                Url = !string.IsNullOrEmpty(p.StorageUrl) 
                    ? _photoStorageService.GetSasUrl(p.StorageUrl, sasValidity) 
                    : string.Empty,
                ThumbnailUrl = !string.IsNullOrEmpty(p.ThumbnailUrl)
                    ? _photoStorageService.GetSasUrl(p.ThumbnailUrl, sasValidity)
                    : null,
                Caption = p.Caption,
                Category = p.Category.ToString(),
                Condition = p.Condition.ToString(),
                SortOrder = p.SortOrder
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load photos for study {StudyId}", studyId);
            return [];
        }
    }

    private static List<VendorInfo> BuildVendors(ReserveStudy study)
    {
        var vendors = new List<VendorInfo>();
        var seenVendors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Collect service contacts from elements
        void AddServiceContact(ServiceContact? contact, string category)
        {
            if (contact == null || string.IsNullOrWhiteSpace(contact.CompanyName))
                return;

            var key = $"{contact.CompanyName}|{category}";
            if (seenVendors.Contains(key))
                return;

            seenVendors.Add(key);
            vendors.Add(new VendorInfo
            {
                Name = contact.CompanyName,
                Category = category,
                Phone = contact.Phone,
                Email = contact.Email,
                Website = null,
                Notes = null
            });
        }

        foreach (var element in study.ReserveStudyBuildingElements ?? [])
        {
            AddServiceContact(element.ServiceContact, element.BuildingElement?.Name ?? "Building");
        }

        foreach (var element in study.ReserveStudyCommonElements ?? [])
        {
            AddServiceContact(element.ServiceContact, element.CommonElement?.Name ?? "Common Area");
        }

        foreach (var element in study.ReserveStudyAdditionalElements ?? [])
        {
            AddServiceContact(element.ServiceContact, element.Name ?? "Additional");
        }

        return vendors;
    }

    private static List<GlossaryTerm> GetDefaultGlossaryTerms()
    {
        // Standard reserve study glossary terms
        return
        [
            new GlossaryTerm
            {
                Term = "Reserve Fund",
                Definition = "Money accumulated to pay for anticipated major repairs and replacements of common area components."
            },
            new GlossaryTerm
            {
                Term = "Useful Life",
                Definition = "The estimated time a component can be expected to serve its intended function in its current configuration."
            },
            new GlossaryTerm
            {
                Term = "Remaining Life",
                Definition = "The estimated time until a component will require major repair or replacement."
            },
            new GlossaryTerm
            {
                Term = "Current Cost",
                Definition = "The cost to repair or replace a component at today's prices."
            },
            new GlossaryTerm
            {
                Term = "Percent Funded",
                Definition = "The ratio of the actual reserve fund balance to the ideal (fully funded) reserve balance, expressed as a percentage."
            },
            new GlossaryTerm
            {
                Term = "Fully Funded",
                Definition = "Having reserves equal to the sum of the depreciation of all reserve components."
            },
            new GlossaryTerm
            {
                Term = "Baseline Funding",
                Definition = "A funding goal that maintains a reserve balance above zero throughout the projection period."
            },
            new GlossaryTerm
            {
                Term = "Threshold Funding",
                Definition = "A funding goal that maintains a reserve balance above a specified minimum amount."
            },
            new GlossaryTerm
            {
                Term = "Component Method",
                Definition = "A method of calculating reserve contributions based on the depreciation of individual components."
            },
            new GlossaryTerm
            {
                Term = "Cash Flow Method",
                Definition = "A method of calculating reserve contributions based on maintaining adequate cash flow over time."
            }
        ];
    }

    private async Task<CalculatedOutputs> BuildCalculatedOutputsAsync(ReserveStudy study, CancellationToken ct)
    {
        var outputs = new CalculatedOutputs();

        try
        {
            // Get calculation results from the study
            var result = await _calculatorService.CalculateFromStudyAsync(study.Id);

            outputs.FundStatusLabel = GetFundStatusLabel(result.PercentFunded);
            outputs.FundingMethod = "Cash Flow"; // Default
            outputs.PercentFunded = result.PercentFunded;
            outputs.IdealBalance = result.FullyFundedBalance;

            // First year summary from Years collection
            if (result.Years.Count > 0)
            {
                var firstYear = result.Years[0];
                outputs.FirstYear = new FirstYearSummary
                {
                    StartingBalance = firstYear.BeginningBalance,
                    Contribution = firstYear.Contribution,
                    Interest = firstYear.InterestEarned,
                    Expenditures = firstYear.Expenditures,
                    EndingBalance = firstYear.EndingBalance
                };
            }

            // Contribution schedule from Years
            outputs.ContributionSchedule = result.Years
                .Take(10) // First 10 years
                .Select((yr, idx) => new YearContribution
                {
                    Year = yr.CalendarYear,
                    Amount = yr.Contribution,
                    PercentIncrease = idx > 0 && result.Years[idx - 1].Contribution > 0
                        ? (yr.Contribution - result.Years[idx - 1].Contribution) / result.Years[idx - 1].Contribution * 100
                        : null,
                    MonthlyPerUnit = study.FinancialInfo?.TotalNumberOfUnits > 0
                        ? yr.Contribution / 12 / study.FinancialInfo.TotalNumberOfUnits.Value
                        : null
                })
                .ToList();

            // Allocation by category - map from Core CategoryAllocation to NarrativeTemplates CategoryAllocation
            outputs.AllocationByCategory = result.Allocation
                .Select(a => new Models.NarrativeTemplates.CategoryAllocation
                {
                    Category = a.Category,
                    Amount = a.TotalSpend,
                    Percent = a.PercentOfTotal
                })
                .OrderByDescending(a => a.Amount)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate outputs for study {StudyId}", study.Id);
        }

        return outputs;
    }

    private static string GetFundStatusLabel(decimal percentFunded)
    {
        return percentFunded switch
        {
            >= 100 => "Fully Funded",
            >= 70 => "Strong",
            >= 30 => "Fair",
            _ => "Below Threshold"
        };
    }

    private static List<InfoFurnishedItem> BuildInfoFurnished(ReserveStudy study)
    {
        var items = new List<InfoFurnishedItem>();
        var sortOrder = 1;

        var fi = study.FinancialInfo;
        if (fi == null) return items;

        void Add(string item, string? value, string? source = "Association")
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                items.Add(new InfoFurnishedItem
                {
                    Item = item,
                    Value = value,
                    Source = source,
                    SortOrder = sortOrder++
                });
            }
        }

        Add("Current Reserve Balance", fi.JanuaryFirstReserveBalance?.ToString("C0"));
        Add("Annual Contribution", fi.BudgetedContributionCurrentYear?.ToString("C0"));
        Add("Operating Budget", fi.OperatingBudgetCurrentYear?.ToString("C0"));
        Add("Number of Units", fi.TotalNumberOfUnits?.ToString());
        Add("Interest Rate", fi.InterestRateOnReserveFunds?.ToString("P2"));
        Add("Fiscal Year Start", new DateTime(2000, fi.FiscalYearStartMonth, 1).ToString("MMMM"));

        if (fi.LoanAmount.HasValue && fi.LoanAmount > 0)
        {
            Add("Outstanding Loan", fi.LoanBalanceRemaining?.ToString("C0"));
        }

        if (fi.SpecialAssessmentAmount.HasValue && fi.SpecialAssessmentAmount > 0)
        {
            Add("Special Assessment", fi.SpecialAssessmentBalanceRemaining?.ToString("C0"));
        }

        return items;
    }

        /// <summary>
        /// Helper class for parsing tenant branding JSON.
        /// Supports both legacy property names and BrandingPayload property names.
        /// </summary>
        private sealed class BrandingData
        {
            // Legacy property names
            public string? Phone { get; set; }
            public string? Email { get; set; }
            public string? Website { get; set; }
            public string? Address { get; set; }
            public string? LogoUrl { get; set; }
            public string? CoverImageUrl { get; set; }
            public string? FooterText { get; set; }
            public string? Tagline { get; set; }

            // BrandingPayload property names (from ThemeService)
            public string? CompanyPhone { get; set; }
            public string? CompanyEmail { get; set; }
            public string? CompanyWebsite { get; set; }
            public string? CompanyAddress { get; set; }
            public string? CompanyLogoUrl { get; set; }
            public string? CompanyTagline { get; set; }
        }
    }
