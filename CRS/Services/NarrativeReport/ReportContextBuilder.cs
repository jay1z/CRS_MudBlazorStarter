using CRS.Data;
using CRS.Models;
using CRS.Models.NarrativeTemplates;
using CRS.Services.Interfaces;
using CRS.Services.ReserveCalculator;
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
    private readonly IReserveStudyCalculatorService _calculatorService;
    private readonly ILogger<ReportContextBuilder> _logger;

    public ReportContextBuilder(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        ITenantContext tenantContext,
        ISiteVisitPhotoService photoService,
        IReserveStudyCalculatorService calculatorService,
        ILogger<ReportContextBuilder> logger)
    {
        _dbFactory = dbFactory;
        _tenantContext = tenantContext;
        _photoService = photoService;
        _calculatorService = calculatorService;
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

        // Build the context
        var reportContext = new ReserveStudyReportContext
        {
            Association = BuildAssociationInfo(study),
            Study = BuildStudyInfo(study, options),
            FinancialAssumptions = BuildFinancialAssumptions(study),
            Branding = BuildBrandingInfo(tenant),
            Signatories = BuildSignatories(study)
        };

        // Add photos if requested
        if (options.IncludePhotos)
        {
            reportContext.Photos = await BuildPhotosAsync(reserveStudyId, cancellationToken);
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
        }

        // Add info furnished items
        reportContext.InfoFurnished = BuildInfoFurnished(study);

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
        var fiscalStartMonth = financialInfo?.FiscalYearStartMonth ?? 1;

        return new StudyInfo
        {
            StudyType = proposal?.ServiceLevel, // Use ServiceLevel as study type indicator
            ReportTitle = options.ReportTitleOverride ?? $"Reserve Study for {study.Community?.Name ?? "Community"}",
            InspectionDate = proposal?.DateApproved, // Use approval date as proxy for inspection
            EffectiveDate = proposal?.ProposalDate,
            FiscalYearStart = GetFiscalYearStart(fiscalStartMonth),
            FiscalYearEnd = GetFiscalYearEnd(fiscalStartMonth),
            PreparedBy = study.Specialist?.FullName ?? study.Specialist?.Email,
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

    private BrandingInfo BuildBrandingInfo(Models.Tenant? tenant)
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

        return new BrandingInfo
        {
            CompanyName = tenant.Name,
            Phone = brandingData?.Phone,
            Email = brandingData?.Email,
            Website = brandingData?.Website,
            Address = brandingData?.Address,
            LogoUrl = brandingData?.LogoUrl,
            CoverImageUrl = brandingData?.CoverImageUrl,
            FooterText = brandingData?.FooterText,
            Tagline = brandingData?.Tagline
        };
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
            return photos.Select(p => new PhotoItem
            {
                Url = p.StorageUrl ?? string.Empty,
                ThumbnailUrl = p.ThumbnailUrl,
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
    /// </summary>
    private sealed class BrandingData
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? Address { get; set; }
        public string? LogoUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? FooterText { get; set; }
        public string? Tagline { get; set; }
    }
}
