using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

namespace CRS.Models {
    public class ReserveStudy : BaseModel, CRS.Services.Tenant.ITenantScoped {
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

        //1:1 relationships managed via dependent FKs
        public Proposal? Proposal { get; set; }
        public FinancialInfo? FinancialInfo { get; set; }

        // Shared PK1:1 to workflow StudyRequest (Id)
        public CRS.Models.Workflow.StudyRequest? StudyRequest { get; set; }

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
                } else {
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
            ProposalUpdated = 3,
            ProposalApproved = 4,
            ProposalSent = 5,
            ProposalAccepted = 6,
            FinancialInfoRequested = 7,
            ServiceContactsRequested = 8,
            FinancialInfoCreated = 9,
            FinancialInfoSubmitted = 10,
            FinancialInfoReviewed = 11,
            FinancialInfoReceived = 12,
            SiteVisit = 13,
            SiteVisitScheduled = 14,
            SiteVisitCompleted = 15,
            SiteVisitDataEntered = 16,
            FundingPlanReady = 17,
            FundingPlanInProcess = 18,
            FundingPlanComplete = 19,
            NarrativeReady = 20,
            NarrativeInProcess = 21,
            NarrativeComplete = 22,
            NarrativePrintReady = 23,
            NarrativePackaged = 24,
            NarrativeSent = 25,
            ReportReady = 26,
            ReportInProcess = 27,
            ReportComplete = 28,
            RequestCompleted = 29,
            RequestCancelled = 30,
            RequestArchived = 31,
        }

        [DataType(DataType.DateTime)]
        public DateTime? DateApproved { get; set; }

        public DateTime? LastModified { get; set; }

        public enum PointOfContactTypeEnum {
            Contact = 0,
            PropertyManager = 1
        }

        // SaaS Refactor: Tenant scope
        public int TenantId { get; set; }

        // Concurrency token
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}
