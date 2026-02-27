using System;
using System.Collections.Generic;

namespace Horizon.Controllers.Requests {
    public record CreateReserveStudyRequest {
        public Guid? ApplicationUserId { get; init; }
        public Guid? SpecialistUserId { get; init; }
        public Guid? CommunityId { get; init; }
        public int TenantId { get; init; }
        public int PointOfContactType { get; init; }
        public Guid? ContactId { get; init; }
        public Guid? PropertyManagerId { get; init; }
        // Keep element lists optional - editor saves will handle complex structures separately
        public List<Guid>? BuildingElementIds { get; init; }
    }

    public record SendProposalRequest {
        public string ProposalScope { get; init; } = string.Empty;
        public decimal EstimatedCost { get; init; }
        public string? Comments { get; init; }
    }

    public record SubmitFinancialInfoRequest {
        public decimal CurrentReserveFundBalance { get; init; }
        public decimal AnnualContribution { get; init; }
        public decimal? ProjectedAnnualExpenses { get; init; }
        public int FiscalYearStartMonth { get; init; } = 1;
        public string? FinancialDocumentUrls { get; init; }
        public string? Comments { get; init; }
    }
}
