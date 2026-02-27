using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Horizon.Services.Tenant;

namespace Horizon.Models {
    public class FinancialInfo : BaseModel, ITenantScoped {
        [ForeignKey("ReserveStudy")]
        public Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        // SaaS Refactor: scope data to tenant
        public int TenantId { get; set; }

        // ========== Reserve Balance Information ==========/
        /// <summary>Current year Jan 1 reserve balance (include unexpended operation funds from last year rolled in)</summary>
        [Precision(18, 2)]
        public decimal? JanuaryFirstReserveBalance { get; set; }

        /// <summary>Expected Dec 31 reserve balance after completion of all current year reserve expenditures</summary>
        [Precision(18, 2)]
        public decimal? DecemberThirtyFirstReserveBalance { get; set; }

        // ========== Budgeted Reserve Contributions ==========/
        [Precision(18, 2)]
        public decimal? BudgetedContributionLastYear { get; set; }

        [Precision(18, 2)]
        public decimal? BudgetedContributionCurrentYear { get; set; }

        [Precision(18, 2)]
        public decimal? BudgetedContributionNextYear { get; set; }

        // ========== Operating Budget ==========/
        [Precision(18, 2)]
        public decimal? OperatingBudgetCurrentYear { get; set; }

        [Precision(18, 2)]
        public decimal? OperatingBudgetNextYear { get; set; }

        // ========== Community Information ==========/
        public int? TotalNumberOfUnits { get; set; }

        public int? AnnualMeetingMonth { get; set; }

        public DateTime? AnnualMeetingDate { get; set; }

        // ========== Loans and Special Assessments ==========/
        [Precision(18, 2)]
        public decimal? LoanAmount { get; set; }

        [Precision(18, 2)]
        public decimal? LoanBalanceRemaining { get; set; }

        public int? LoanExpectedYearComplete { get; set; }

        [Precision(18, 2)]
        public decimal? SpecialAssessmentAmount { get; set; }

        [Precision(18, 2)]
        public decimal? SpecialAssessmentBalanceRemaining { get; set; }

        public int? SpecialAssessmentExpectedYearComplete { get; set; }

        // ========== Planned Projects ==========/
        /// <summary>Planned projects for current or future years (JSON or text format: description, year, quoted cost)</summary>
        public string? PlannedProjects { get; set; }

        // ========== Insurance and Interest ==========/
        [Precision(18, 2)]
        public decimal? PropertyInsuranceDeductible { get; set; }

        [Precision(5, 2)]
        public decimal? InterestRateOnReserveFunds { get; set; }

        // ========== Building Component Information ==========/
        /// <summary>List of buildings with roof/siding replacement years (JSON format)</summary>
        public string? BuildingRoofSidingInfo { get; set; }

        /// <summary>Known last replacement dates for larger primary components (fencing, entry feature, roads, etc.)</summary>
        public string? ComponentReplacementDates { get; set; }

        // ========== Siding Calculation Preference ==========/
        /// <summary>1 = Replace all at once/phased (aesthetic unity), 2 = Replace as needed from reserve (cost effective)</summary>
        public int? SidingCalculationPreference { get; set; }

        // ========== Acknowledgment ==========/
        public bool AcknowledgementAccepted { get; set; }

        public string? CommunityNameOnAcknowledgment { get; set; }

        public string? PresidentSignature { get; set; }

        public DateTime? AcknowledgmentSignatureDate { get; set; }

        // ========== Legacy Fields (kept for backward compatibility) ==========/
        [Precision(18, 2)]
        public decimal CurrentReserveFundBalance { get; set; }

        [Precision(18, 2)]
        public decimal AnnualContribution { get; set; }

        [Precision(18, 2)]
        public decimal? ProjectedAnnualExpenses { get; set; }

        public int FiscalYearStartMonth { get; set; } = 1; // Default January

        public string? FinancialDocumentUrls { get; set; }

        public DateTime? DateSubmitted { get; set; }

        public DateTime? DateReviewed { get; set; }

        public string? ReviewedBy { get; set; }

        public bool IsComplete { get; set; }

        public string? Comments { get; set; }
    }
}