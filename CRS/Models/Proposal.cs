using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using CRS.Services.Tenant;

namespace CRS.Models {
    /// <summary>
    /// Defines the payment schedule type for a proposal
    /// </summary>
    public enum PaymentScheduleType
    {
        /// <summary>Full payment due on invoice</summary>
        FullPayment = 0,

        /// <summary>50% deposit, 50% on completion</summary>
        FiftyFifty = 1,

        /// <summary>33% deposit, 33% on site visit, 34% on completion</summary>
        ThirdThirdThird = 2,

        /// <summary>25% deposit, 25% on site visit, 25% on draft, 25% on final</summary>
        QuartersPayment = 3,

        /// <summary>Custom payment schedule defined by DepositPercentage</summary>
        Custom = 99
    }

    /// <summary>
    /// Defines invoice milestone types that correspond to workflow stages
    /// </summary>
    public enum InvoiceMilestoneType
    {
        /// <summary>Initial deposit upon proposal acceptance</summary>
        Deposit = 0,

        /// <summary>Payment due when site visit is completed</summary>
        SiteVisitComplete = 1,

        /// <summary>Payment due when draft report is delivered</summary>
        DraftReportDelivery = 2,

        /// <summary>Final payment due on report completion</summary>
        FinalDelivery = 3,

        /// <summary>Full payment (single invoice)</summary>
        FullPayment = 10,

        /// <summary>Custom milestone</summary>
        Custom = 99
    }

    public class Proposal : BaseModel, ITenantScoped {
        [ForeignKey("ReserveStudy")]
        public Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        // SaaS Refactor: scope data to tenant
        public int TenantId { get; set; }

        [Required]
        public string ProposalScope { get; set; } = string.Empty;

        [Required]
        [Precision(18,2)]
        public decimal EstimatedCost { get; set; }

        public DateTime ProposalDate { get; set; } = DateTime.UtcNow;

        public DateTime? DateSent { get; set; }

        public DateTime? DateApproved { get; set; }

        public string? ApprovedBy { get; set; }

        public bool IsApproved { get; set; }

        public string? Comments { get; set; }
        
        // Service level and delivery options
        public string? ServiceLevel { get; set; }
        
        public string? DeliveryTimeframe { get; set; }
        
        public string? PaymentTerms { get; set; }

        /// <summary>
        /// The type of payment schedule for this proposal
        /// </summary>
        public PaymentScheduleType PaymentSchedule { get; set; } = PaymentScheduleType.FullPayment;

        /// <summary>
        /// Custom deposit percentage (used when PaymentSchedule is Custom)
        /// </summary>
        [Precision(5, 2)]
        public decimal? CustomDepositPercentage { get; set; }

        /// <summary>
        /// Prepayment discount percentage (e.g., 5 for 5%)
        /// </summary>
        [Precision(5, 2)]
        public decimal PrepaymentDiscountPercentage { get; set; } = 5.0m;

        // ============================================
        // PAYMENT TERMS CONFIGURATION
        // ============================================

        /// <summary>
        /// Number of days until payment is due (Net 30, Net 45, etc.)
        /// </summary>
        public int PaymentDueDays { get; set; } = 30;

        /// <summary>
        /// Early payment discount percentage (e.g., 2 for 2% - as in "2/10 Net 30")
        /// </summary>
        [Precision(5, 2)]
        public decimal EarlyPaymentDiscountPercentage { get; set; } = 0m;

        /// <summary>
        /// Number of days to qualify for early payment discount (e.g., 10 for "2/10 Net 30")
        /// </summary>
        public int EarlyPaymentDiscountDays { get; set; } = 10;

        /// <summary>
        /// Monthly interest rate for late payments (e.g., 1.5 for 1.5% per month)
        /// </summary>
        [Precision(5, 2)]
        public decimal LatePaymentInterestRate { get; set; } = 0m;

        /// <summary>
        /// Grace period days after due date before interest accrues
        /// </summary>
        public int LatePaymentGracePeriodDays { get; set; } = 0;

        /// <summary>
        /// Minimum deposit amount regardless of percentage
        /// </summary>
        [Precision(18, 2)]
        public decimal? MinimumDepositAmount { get; set; }

        /// <summary>
        /// Whether the deposit is non-refundable
        /// </summary>
        public bool IsDepositNonRefundable { get; set; } = false;

        // Additional service options
        public bool IncludePrepaymentDiscount { get; set; }
        
        public bool IncludeDigitalDelivery { get; set; }
        
        public bool IncludeComponentInventory { get; set; }
        
        public bool IncludeFundingPlans { get; set; }
        
        // Amendment tracking
        /// <summary>
        /// Indicates if this proposal is an amendment to a previous proposal
        /// </summary>
        public bool IsAmendment { get; set; }
        
        /// <summary>
        /// Reference to the original proposal this amendment is based on
        /// </summary>
        public Guid? OriginalProposalId { get; set; }
        
        [ForeignKey("OriginalProposalId")]
        public Proposal? OriginalProposal { get; set; }
        
        /// <summary>
        /// The amendment version number (1 = first amendment, 2 = second amendment, etc.)
        /// </summary>
        public int AmendmentNumber { get; set; }
        
        /// <summary>
        /// Reason for the amendment (e.g., scope change after site visit)
        /// </summary>
        public string? AmendmentReason { get; set; }
        
        /// <summary>
        /// Collection of amendments based on this proposal
        /// </summary>
        public virtual ICollection<Proposal>? Amendments { get; set; }
    }
}