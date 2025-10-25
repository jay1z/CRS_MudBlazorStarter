using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using CRS.Services.Tenant;

namespace CRS.Models {
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
    }
}