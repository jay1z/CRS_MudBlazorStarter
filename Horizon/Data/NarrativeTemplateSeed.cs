using Horizon.Models.NarrativeTemplates;

namespace Horizon.Data;

/// <summary>
/// Seed data for global narrative template sections, blocks, and inserts.
/// These serve as defaults that can be overridden per-tenant.
/// </summary>
public static class NarrativeTemplateSeed
{
    /// <summary>
    /// Seeds default narrative template data if not already present.
    /// </summary>
    public static void Seed(ApplicationDbContext context)
    {
        // Only seed if no sections exist
        if (context.NarrativeTemplateSections.Any(s => s.TenantId == 0))
            return;

        SeedSections(context);
        context.SaveChanges();

        SeedBlocks(context);
        context.SaveChanges();

        SeedInserts(context);
        context.SaveChanges();
    }

    private static void SeedSections(ApplicationDbContext context)
    {
        var sections = new List<NarrativeTemplateSection>
        {
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Cover,
                Title = "Cover Page",
                IsEnabled = true,
                SortOrder = 10,
                Description = "Report cover page with association name, logo, and date",
                PageBreakBefore = false,
                CssClass = "cover-page"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExhibitsIntro,
                Title = "Table of Contents",
                IsEnabled = true,
                SortOrder = 20,
                Description = "Table of contents and exhibits overview",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExecutiveSummary,
                Title = "Executive Summary",
                IsEnabled = true,
                SortOrder = 30,
                Description = "High-level overview for board members",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.FundingSummary,
                Title = "Funding Summary",
                IsEnabled = true,
                SortOrder = 40,
                Description = "Fund status and recommended contribution schedule",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.SignaturePage,
                Title = "Signature Page",
                IsEnabled = true,
                SortOrder = 50,
                Description = "Professional certifications and signatures",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeEvaluation,
                Title = "Evaluation",
                IsEnabled = true,
                SortOrder = 60,
                Description = "Property evaluation and methodology",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeAssumptions,
                Title = "Assumptions",
                IsEnabled = true,
                SortOrder = 70,
                Description = "Inflation, interest, and economic assumptions"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeOperatingVsReserves,
                Title = "Operating vs Reserves",
                IsEnabled = true,
                SortOrder = 80,
                Description = "Guidance on categorizing expenses"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeSources,
                Title = "Sources",
                IsEnabled = true,
                SortOrder = 90,
                Description = "Data sources and references"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeComponents,
                Title = "Reserve Components",
                IsEnabled = true,
                SortOrder = 100,
                Description = "Component analysis and 4-part test"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeCharts,
                Title = "Charts Explanation",
                IsEnabled = true,
                SortOrder = 110,
                Description = "How to interpret the charts and graphs"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeConclusion,
                Title = "Conclusion",
                IsEnabled = true,
                SortOrder = 120,
                Description = "Summary and recommendations"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.InfoFurnished,
                Title = "Information Furnished by Management",
                IsEnabled = true,
                SortOrder = 130,
                Description = "Data provided by the association",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Definitions,
                Title = "Definitions",
                IsEnabled = true,
                SortOrder = 140,
                Description = "Glossary of terms"
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExhibitPhotos,
                Title = "Photo Exhibits",
                IsEnabled = true,
                SortOrder = 200,
                Description = "Site visit photographs",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExhibitTables,
                Title = "Tables & Spreadsheets",
                IsEnabled = true,
                SortOrder = 210,
                Description = "Component schedules and projections",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExhibitCharts,
                Title = "Charts",
                IsEnabled = true,
                SortOrder = 220,
                Description = "Graphical representations",
                PageBreakBefore = true
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExhibitDisclosures,
                Title = "Disclosures",
                IsEnabled = true,
                SortOrder = 230,
                Description = "Legal disclosures, credentials, and laws",
                PageBreakBefore = true
            }
        };

        context.NarrativeTemplateSections.AddRange(sections);
    }

    private static void SeedBlocks(ApplicationDbContext context)
    {
        var blocks = new List<NarrativeTemplateBlock>
        {
            // ═══════════════════════════════════════════════════════════════
            // COVER PAGE
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Cover,
                BlockKey = NarrativeBlockKeys.CoverLogo,
                Title = "Company Logo",
                HtmlTemplate = @"<div class=""cover-logo"">
    <img src=""{LogoUrl}"" alt=""{CompanyName}"" class=""logo"" />
</div>",
                IsEnabled = true,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Cover,
                BlockKey = NarrativeBlockKeys.CoverTitle,
                Title = "Cover Title",
                HtmlTemplate = @"<div class=""cover-title"">
    <h1>{ReportTitle}</h1>
    <h2>{StudyType} Reserve Study</h2>
</div>",
                IsEnabled = true,
                SortOrder = 20
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Cover,
                BlockKey = NarrativeBlockKeys.CoverAssociationInfo,
                Title = "Association Information",
                HtmlTemplate = @"<div class=""cover-association"">
    <h2>{AssociationName}</h2>
    <p>{FullAddress}</p>
</div>",
                IsEnabled = true,
                SortOrder = 30
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Cover,
                BlockKey = NarrativeBlockKeys.CoverCompanyInfo,
                Title = "Company Information",
                HtmlTemplate = @"<div class=""cover-company"">
    <p>Prepared by</p>
    <p><strong>{CompanyName}</strong></p>
    <p>{CompanyAddress}</p>
    <p>{CompanyPhone} | {CompanyEmail}</p>
    <p>{CompanyWebsite}</p>
    <p class=""report-date"">{ReportDate}</p>
</div>",
                IsEnabled = true,
                SortOrder = 40
            },

            // ═══════════════════════════════════════════════════════════════
            // EXECUTIVE SUMMARY
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExecutiveSummary,
                BlockKey = NarrativeBlockKeys.ExecSummaryIntro,
                Title = "Executive Summary Introduction",
                HtmlTemplate = @"<h2>Executive Summary</h2>
<p>This {StudyType} Reserve Study was prepared for <strong>{AssociationName}</strong> located in {CityState}. The study was conducted in accordance with the Reserve Specialist (RS) and Professional Reserve Analyst (PRA) standards as set forth by the Community Associations Institute (CAI) and the Association of Professional Reserve Analysts (APRA).</p>

<p>The on-site inspection was conducted in {InspectionMonthYear}. This study projects reserve fund requirements for a {ProjectionYears}-year period beginning with fiscal year {FiscalYearLabel}.</p>",
                IsEnabled = true,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExecutiveSummary,
                BlockKey = NarrativeBlockKeys.ExecSummaryFindings,
                Title = "Key Findings",
                HtmlTemplate = @"<h3>Key Findings</h3>
<ul>
    <li><strong>Current Fund Status:</strong> {FundStatusLabel} ({PercentFunded} funded)</li>
    <li><strong>Starting Reserve Balance:</strong> {StartingReserveBalance}</li>
    <li><strong>Recommended First Year Contribution:</strong> {FirstYearContribution}</li>
    <li><strong>Funding Method:</strong> {FundingMethod}</li>
</ul>",
                IsEnabled = true,
                SortOrder = 20
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExecutiveSummary,
                BlockKey = NarrativeBlockKeys.ExecSummaryRecommendations,
                Title = "Recommendations",
                HtmlTemplate = @"<h3>Recommendations</h3>
<p>Based on our analysis, we recommend that {AssociationName} adopt the funding plan outlined in this study. The recommended contribution schedule is designed to maintain adequate reserves while avoiding special assessments or deferred maintenance.</p>",
                IsEnabled = true,
                SortOrder = 30
            },

            // ═══════════════════════════════════════════════════════════════
            // FUNDING SUMMARY
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.FundingSummary,
                BlockKey = NarrativeBlockKeys.FundStatusSummary,
                Title = "Fund Status Summary",
                HtmlTemplate = @"<h2>Funding Summary</h2>
<h3>Current Fund Status</h3>
<table class=""summary-table"">
    <tr><th>Starting Reserve Balance</th><td>{StartingReserveBalance}</td></tr>
    <tr><th>Ideal (Fully Funded) Balance</th><td>{IdealBalance}</td></tr>
    <tr><th>Percent Funded</th><td>{PercentFunded}</td></tr>
    <tr><th>Fund Status</th><td>{FundStatusLabel}</td></tr>
</table>

<h3>First Year Summary</h3>
<table class=""summary-table"">
    <tr><th>Beginning Balance</th><td>{StartingBalance}</td></tr>
    <tr><th>Annual Contribution</th><td>{FirstYearContribution}</td></tr>
    <tr><th>Interest Earned</th><td>{FirstYearInterest}</td></tr>
    <tr><th>Projected Expenditures</th><td>{FirstYearExpenditures}</td></tr>
    <tr><th>Ending Balance</th><td>{EndingBalance}</td></tr>
</table>",
                IsEnabled = true,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.FundingSummary,
                BlockKey = NarrativeBlockKeys.ContributionScheduleTable,
                Title = "Contribution Schedule",
                HtmlTemplate = @"<h3>Recommended Contribution Schedule</h3>
[[TABLE:ContributionSchedule]]",
                IsEnabled = true,
                SortOrder = 20
            },

                        // ═══════════════════════════════════════════════════════════════
                        // SIGNATURE PAGE
                        // ═══════════════════════════════════════════════════════════════
                        new()
                        {
                            TenantId = 0,
                            SectionKey = NarrativeSectionKeys.SignaturePage,
                            BlockKey = NarrativeBlockKeys.SignatureIntro,
                            Title = "Signature Introduction",
                            HtmlTemplate = @"[[PAGE_BREAK]]
            <h2>Certification</h2>
            <p>We certify that the findings and recommendations presented in this Reserve Study were prepared in accordance with the standards and guidelines established by the Community Associations Institute (CAI) and applicable state regulations.</p>
            <p>This study represents our professional opinion based on the information provided and our on-site observations. Actual costs may vary from the estimates provided herein.</p>",
                            IsEnabled = true,
                            SortOrder = 10
                        },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.SignaturePage,
                BlockKey = NarrativeBlockKeys.SignatureBlocks,
                Title = "Signatures",
                HtmlTemplate = @"[[SIGNATURES]]",
                IsEnabled = true,
                SortOrder = 20
            },

            // ═══════════════════════════════════════════════════════════════
            // NARRATIVE - EVALUATION
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeEvaluation,
                BlockKey = NarrativeBlockKeys.EvaluationIntro,
                Title = "Evaluation Introduction",
                HtmlTemplate = @"<h2>Property Evaluation</h2>
<p>A physical analysis of the common areas and reserve components was conducted on {InspectionDate}. The evaluation included a visual inspection of all major building systems, grounds, and amenities that are the financial responsibility of {AssociationName}.</p>",
                IsEnabled = true,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeEvaluation,
                BlockKey = NarrativeBlockKeys.EvaluationDetails,
                Title = "Evaluation Details",
                HtmlTemplate = @"<h3>Property Overview</h3>
<table class=""property-table"">
    <tr><th>Community Name</th><td>{AssociationName}</td></tr>
    <tr><th>Location</th><td>{CityState}</td></tr>
    <tr><th>Community Type</th><td>{CommunityType}</td></tr>
    <tr><th>Number of Units</th><td>{UnitCount}</td></tr>
    <tr><th>Year Established</th><td>{EstablishedYear}</td></tr>
</table>",
                IsEnabled = true,
                SortOrder = 20
            },

            // ═══════════════════════════════════════════════════════════════
            // NARRATIVE - ASSUMPTIONS
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeAssumptions,
                BlockKey = NarrativeBlockKeys.AssumptionsInflation,
                Title = "Inflation Assumptions",
                HtmlTemplate = @"<h2>Financial Assumptions</h2>
<h3>Inflation</h3>
<p>This study uses an inflation rate of <strong>{InflationRate}</strong> for projecting future replacement costs. This rate is based on historical construction cost indices and current economic forecasts.</p>
<p>Actual inflation rates may vary from year to year. We recommend that this study be updated at least every 3-5 years to account for changes in economic conditions.</p>",
                IsEnabled = true,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeAssumptions,
                BlockKey = NarrativeBlockKeys.AssumptionsInterest,
                Title = "Interest Assumptions",
                HtmlTemplate = @"<h3>Interest on Reserve Funds</h3>
<p>This study assumes an interest rate of <strong>{InterestRate}</strong> on reserve fund balances. This rate represents a conservative estimate for funds invested in low-risk, liquid instruments appropriate for reserve funds.</p>
<p>Tax Treatment: {TaxTreatment}</p>",
                IsEnabled = true,
                SortOrder = 20
            },

            // ═══════════════════════════════════════════════════════════════
            // NARRATIVE - OPERATING VS RESERVES
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeOperatingVsReserves,
                BlockKey = NarrativeBlockKeys.OperatingVsReservesGuidance,
                Title = "Operating vs Reserves Guidance",
                HtmlTemplate = @"<h2>Operating Budget vs Reserve Fund</h2>
<p>Understanding the distinction between operating expenses and reserve expenditures is essential for proper financial planning:</p>
<ul>
    <li><strong>Operating Expenses:</strong> Routine, recurring costs for day-to-day operations (utilities, insurance, management, routine maintenance, etc.)</li>
    <li><strong>Reserve Expenditures:</strong> Major repair or replacement of common area components that have a predictable useful life and require significant funding.</li>
</ul>
<p>Components included in this reserve study generally meet the following criteria:</p>
<ol>
    <li>The association has the responsibility to maintain, repair, or replace the component</li>
    <li>The component has a limited useful life</li>
    <li>The remaining useful life can be reasonably estimated</li>
    <li>The replacement cost exceeds the minimum threshold of {MinimumComponentThreshold}</li>
</ol>",
                IsEnabled = true,
                SortOrder = 10
            },

            // ═══════════════════════════════════════════════════════════════
            // NARRATIVE - COMPONENTS
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeComponents,
                BlockKey = NarrativeBlockKeys.ComponentsIntro,
                Title = "Components Introduction",
                HtmlTemplate = @"<h2>Reserve Components</h2>
<p>Reserve components are the major common area elements that the association is responsible for maintaining, repairing, and replacing. Each component in this study has been evaluated based on the National Reserve Study Standards four-part test.</p>",
                IsEnabled = true,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeComponents,
                BlockKey = NarrativeBlockKeys.ComponentsFourPartTest,
                Title = "Four-Part Test",
                HtmlTemplate = @"<h3>The Four-Part Test</h3>
<p>Each component included in this reserve study meets all four of the following criteria:</p>
<ol>
    <li><strong>Association Responsibility:</strong> The component is a common area maintenance responsibility of the association.</li>
    <li><strong>Limited Useful Life:</strong> The component has a predictable remaining useful life, and will require repair or replacement at some point.</li>
    <li><strong>Predictable Remaining Life:</strong> The remaining useful life of the component can be reasonably estimated.</li>
    <li><strong>Above Threshold Cost:</strong> The cost to repair or replace the component is significant enough to warrant advance funding (above the minimum threshold).</li>
</ol>",
                IsEnabled = true,
                SortOrder = 20
            },

            // ═══════════════════════════════════════════════════════════════
            // NARRATIVE - CONCLUSION
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.NarrativeConclusion,
                BlockKey = NarrativeBlockKeys.ConclusionText,
                Title = "Conclusion",
                HtmlTemplate = @"<h2>Conclusion</h2>
<p>Based on our analysis, {AssociationName} is currently {FundStatusLabel}. The recommended funding plan presented in this study is designed to:</p>
<ul>
    <li>Maintain adequate reserves for anticipated major repairs and replacements</li>
    <li>Minimize the risk of special assessments or deferred maintenance</li>
    <li>Distribute the cost of common area components fairly among current and future homeowners</li>
</ul>
<p>We recommend that the Board of Directors review and adopt the funding recommendations contained in this study. The reserve fund should be reviewed annually and the study should be updated at least every three to five years, or sooner if significant changes occur.</p>
<p>Thank you for the opportunity to prepare this Reserve Study for {AssociationName}. Please contact us if you have any questions regarding the findings or recommendations contained herein.</p>",
                IsEnabled = true,
                SortOrder = 10
            },

            // ═══════════════════════════════════════════════════════════════
            // INFO FURNISHED
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.InfoFurnished,
                BlockKey = NarrativeBlockKeys.InfoFurnishedTable,
                Title = "Information Furnished Table",
                HtmlTemplate = @"<h2>Information Furnished by Management</h2>
<p>The following information was provided by the association's management and/or Board of Directors:</p>
[[TABLE:InfoFurnished]]",
                IsEnabled = true,
                SortOrder = 10
            },

            // ═══════════════════════════════════════════════════════════════
            // DEFINITIONS
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.Definitions,
                BlockKey = NarrativeBlockKeys.DefinitionsGlossary,
                Title = "Definitions Glossary",
                HtmlTemplate = @"<h2>Definitions</h2>
[[GLOSSARY]]",
                IsEnabled = true,
                SortOrder = 10
            },

                        // ═══════════════════════════════════════════════════════════════
                        // EXHIBIT - PHOTOS
                        // ═══════════════════════════════════════════════════════════════
                        new()
                        {
                            TenantId = 0,
                            SectionKey = NarrativeSectionKeys.ExhibitPhotos,
                            BlockKey = NarrativeBlockKeys.PhotoGallery,
                            Title = "Photo Gallery",
                            HtmlTemplate = @"[[PAGE_BREAK]]
            <h2>Site Visit Photographs</h2>
            <p>The following photographs were taken during our on-site inspection on {InspectionDate}:</p>
            [[PHOTOS]]",
                            IsEnabled = true,
                            SortOrder = 10
                        },

            // ═══════════════════════════════════════════════════════════════
            // EXHIBIT - DISCLOSURES
            // ═══════════════════════════════════════════════════════════════
            new()
            {
                TenantId = 0,
                SectionKey = NarrativeSectionKeys.ExhibitDisclosures,
                BlockKey = NarrativeBlockKeys.VendorCredentials,
                Title = "Vendors & Credentials",
                HtmlTemplate = @"<h2>Disclosures & Credentials</h2>
[[VENDORS]]",
                IsEnabled = true,
                SortOrder = 10
            }
        };

        context.NarrativeTemplateBlocks.AddRange(blocks);
    }

    private static void SeedInserts(ApplicationDbContext context)
    {
        var inserts = new List<NarrativeInsert>
        {
            new()
            {
                TenantId = 0,
                InsertKey = NarrativeInsertKeys.UnderfundedWarning,
                Title = "Underfunded Warning",
                HtmlTemplate = @"<div class=""warning-box"">
<h4>Important Notice: Reserve Fund Status</h4>
<p>The current reserve fund balance is below the recommended level. This condition may increase the risk of special assessments or deferred maintenance in the future. The Board should carefully consider the funding recommendations in this study.</p>
</div>",
                IsEnabled = false,
                Description = "Warning shown when reserves are underfunded",
                TargetSectionKey = NarrativeSectionKeys.FundingSummary,
                InsertAfterBlockKey = NarrativeBlockKeys.FundStatusSummary,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                InsertKey = NarrativeInsertKeys.SpecialAssessmentWarning,
                Title = "Special Assessment Warning",
                HtmlTemplate = @"<div class=""warning-box"">
<h4>Special Assessment History</h4>
<p>The association has previously levied special assessments. Maintaining adequate reserves can help minimize the need for future special assessments.</p>
</div>",
                IsEnabled = false,
                Description = "Warning about special assessment history",
                TargetSectionKey = NarrativeSectionKeys.ExecutiveSummary,
                InsertAfterBlockKey = NarrativeBlockKeys.ExecSummaryFindings,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                InsertKey = NarrativeInsertKeys.LoanDisclosure,
                Title = "Loan Disclosure",
                HtmlTemplate = @"<div class=""info-box"">
<h4>Outstanding Loan</h4>
<p>The association currently has an outstanding loan that is being repaid from operating funds. This loan has been considered in the reserve fund projections.</p>
</div>",
                IsEnabled = false,
                Description = "Disclosure when association has an active loan",
                TargetSectionKey = NarrativeSectionKeys.FundingSummary,
                InsertAfterBlockKey = NarrativeBlockKeys.ContributionScheduleTable,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                InsertKey = NarrativeInsertKeys.InflationDisclaimer,
                Title = "Inflation Disclaimer",
                HtmlTemplate = @"<p class=""disclaimer""><em>Note: Inflation projections are estimates based on historical data and current economic forecasts. Actual inflation may differ significantly from the assumed rate, which could affect future reserve fund requirements.</em></p>",
                IsEnabled = false,
                Description = "Disclaimer about inflation assumptions",
                TargetSectionKey = NarrativeSectionKeys.NarrativeAssumptions,
                InsertAfterBlockKey = NarrativeBlockKeys.AssumptionsInflation,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                InsertKey = NarrativeInsertKeys.LimitationsDisclaimer,
                Title = "Limitations Disclaimer",
                HtmlTemplate = @"<div class=""disclaimer-box"">
<h4>Limitations</h4>
<p>This Reserve Study is based on a visual, non-invasive inspection of accessible areas. Hidden conditions, latent defects, or conditions requiring specialized testing may exist and are beyond the scope of this study. This study should not be relied upon as a property condition assessment or home inspection.</p>
</div>",
                IsEnabled = false,
                Description = "Standard limitations disclaimer",
                TargetSectionKey = NarrativeSectionKeys.NarrativeEvaluation,
                InsertAfterBlockKey = NarrativeBlockKeys.EvaluationDetails,
                SortOrder = 10
            },
            new()
            {
                TenantId = 0,
                InsertKey = NarrativeInsertKeys.DeferredMaintenanceWarning,
                Title = "Deferred Maintenance Warning",
                HtmlTemplate = @"<div class=""warning-box"">
<h4>Deferred Maintenance Observed</h4>
<p>During our site inspection, we observed evidence of deferred maintenance on certain components. Addressing these items promptly may help prevent accelerated deterioration and higher replacement costs.</p>
</div>",
                IsEnabled = false,
                Description = "Warning when deferred maintenance is observed",
                TargetSectionKey = NarrativeSectionKeys.NarrativeEvaluation,
                InsertAfterBlockKey = NarrativeBlockKeys.EvaluationDetails,
                SortOrder = 20
            }
        };

        context.NarrativeInserts.AddRange(inserts);
    }
}
