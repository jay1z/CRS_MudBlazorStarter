using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

namespace CRS.Models {
    public class ReserveStudy : BaseModel {
        [ForeignKey(nameof(ApplicationUser))] public Guid? ApplicationUserId { get; set; }
        public ApplicationUser? User { get; set; }

        [ForeignKey("Specialist")]
        public Guid? SpecialistUserId { get; set; }
        public ApplicationUser? Specialist { get; set; }

        [ForeignKey("Community")]
        public Guid? CommunityId { get; set; }
        public Community? Community { get; set; }
        public PointOfContactTypeEnum PointOfContactType { get; set; }

        [ForeignKey("Contact")]
        public Guid? ContactId { get; set; }
        public Contact? Contact { get; set; }

        [ForeignKey("PropertyManager")]
        public Guid? PropertyManagerId { get; set; }
        public PropertyManager? PropertyManager { get; set; }

        [ForeignKey("Proposal")]
        public Guid? ProposalId { get; set; }
        public Proposal? Proposal { get; set; }

        [ForeignKey("FinancialInfo")]
        public Guid? FinancialInfoId { get; set; }
        public FinancialInfo? FinancialInfo { get; set; }

        public List<ReserveStudyBuildingElement>? ReserveStudyBuildingElements { get; set; }
        public List<ReserveStudyCommonElement>? ReserveStudyCommonElements { get; set; }
        public List<ReserveStudyAdditionalElement>? ReserveStudyAdditionalElements { get; set; }

        [NotMapped]
        public List<IReserveStudyElement> ReserveStudyElements {
            get {
                List<IReserveStudyElement>? elements = new List<IReserveStudyElement>();
                if (ReserveStudyBuildingElements != null && ReserveStudyBuildingElements.Count > 0) {
                    elements.AddRange(ReserveStudyBuildingElements);
                }
                if (ReserveStudyCommonElements != null && ReserveStudyCommonElements.Count > 0) {
                    elements.AddRange(ReserveStudyCommonElements);
                }
                if (ReserveStudyAdditionalElements != null && ReserveStudyAdditionalElements.Count > 0) {
                    elements.AddRange(ReserveStudyAdditionalElements);
                }
                return elements;
            }
        }

        [NotMapped]
        public IContact? PointOfContact {
            get {
                if (PointOfContactType == PointOfContactTypeEnum.Contact) {
                    return Contact;
                }
                else {
                    return PropertyManager;
                }
            }
        }

        public bool IsActive { get; set; }
        public bool IsApproved { get; set; }
        public bool IsComplete { get; set; }

        public WorkflowStatus Status { get; set; } = WorkflowStatus.RequestCreated;
        public enum WorkflowStatus {
            RequestCreated = 0,
            ProposalCreated = 1,
            ProposalReviewed = 2,
            ProposalUpdated = 6,
            ProposalApproved = 3,
            ProposalSent = 4,
            ProposalAccepted = 5,
            FinancialInfoRequested = 7,
            ServiceContactsRequested,
            FinancialInfoCreated = 8,
            FinancialInfoSubmitted = 9,
            FinancialInfoReviewed = 10,
            FinancialInfoReceived = 11,
            SiteVisitReady,
            SiteVisitScheduled = 12,
            SiteVisitCompleted,
            SiteVisitDataEntered,
            FundingPlanReady,
            FundingPlanInProcess,
            FundingPlanComplete,
            NarrativeReady,
            NarrativeInProcess,
            NarrativeComplete,
            NarrativePrintReady,
            NarrativePackaged,
            NarrativeSent,
            ReportReady,
            ReportInProcess,
            ReportComplete,
            RequestCompleted = 14,
            RequestCancelled = 15,
            RequestArchived = 16,
        }

        [DataType(DataType.DateTime)]
        public DateTime? DateApproved { get; set; }

        public DateTime? LastModified { get; set; }

        public enum PointOfContactTypeEnum {
            Contact = 0,
            PropertyManager = 1
        }
    }
}
