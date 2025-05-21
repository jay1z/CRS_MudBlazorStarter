using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRS.Models {
    public class Proposal : BaseModel {
        [ForeignKey("ReserveStudy")]
        public Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        [Required]
        public string ProposalScope { get; set; } = string.Empty;

        [Required]
        public decimal EstimatedCost { get; set; }

        public DateTime ProposalDate { get; set; } = DateTime.UtcNow;

        public DateTime? DateSent { get; set; }

        public DateTime? DateApproved { get; set; }

        public string? ApprovedBy { get; set; }

        public bool IsApproved { get; set; }

        public string? Comments { get; set; }
    }
}