using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRS.Models {
    public class FinancialInfo : BaseModel {
        [ForeignKey("ReserveStudy")]
        public Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public decimal CurrentReserveFundBalance { get; set; }

        public decimal AnnualContribution { get; set; }

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