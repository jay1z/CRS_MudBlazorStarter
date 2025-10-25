using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using CRS.Services.Tenant;

namespace CRS.Models {
 public class FinancialInfo : BaseModel, ITenantScoped {
 [ForeignKey("ReserveStudy")]
 public Guid ReserveStudyId { get; set; }
 public ReserveStudy? ReserveStudy { get; set; }

 // SaaS Refactor: scope data to tenant
 public int TenantId { get; set; }

 [Precision(18,2)]
 public decimal CurrentReserveFundBalance { get; set; }

 [Precision(18,2)]
 public decimal AnnualContribution { get; set; }

 [Precision(18,2)]
 public decimal? ProjectedAnnualExpenses { get; set; }

 public int FiscalYearStartMonth { get; set; } =1; // Default January

 public string? FinancialDocumentUrls { get; set; }

 public DateTime? DateSubmitted { get; set; }

 public DateTime? DateReviewed { get; set; }

 public string? ReviewedBy { get; set; }

 public bool IsComplete { get; set; }

 public string? Comments { get; set; }
 }
}