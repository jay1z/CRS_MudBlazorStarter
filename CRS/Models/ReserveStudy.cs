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

        /// <summary>
        /// The HOA user/customer who requested or owns this study.
        /// For HOA users creating their own requests, this equals ApplicationUserId.
        /// For staff creating on behalf of HOA, this is the HOA user's ID.
        /// </summary>
        [ForeignKey("RequestedByUser")]
        public Guid? RequestedByUserId { get; set; }
        public ApplicationUser? RequestedByUser { get; set; }

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

        //1:many relationship - a study can have multiple proposals (original + amendments)
        public List<Proposal>? Proposals { get; set; }
        
        /// <summary>
        /// The currently active/latest proposal for this study.
        /// When amendments are created, this points to the new amendment.
        /// </summary>
        [ForeignKey("CurrentProposal")]
        public Guid? CurrentProposalId { get; set; }
        public Proposal? CurrentProposal { get; set; }
        
        /// <summary>
        /// Convenience property that returns the current active proposal.
        /// For backward compatibility with code that expects study.Proposal.
        /// </summary>
        [NotMapped]
        public Proposal? Proposal => CurrentProposal ?? Proposals?.OrderByDescending(p => p.AmendmentNumber).ThenByDescending(p => p.DateCreated).FirstOrDefault();

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

        /// <summary>
        /// Gets the current workflow status from the associated StudyRequest.
        /// This is the single source of truth for workflow status.
        /// </summary>
        [NotMapped]
        public CRS.Models.Workflow.StudyStatus CurrentStatus => 
            StudyRequest?.CurrentStatus ?? CRS.Models.Workflow.StudyStatus.RequestCreated;

        [DataType(DataType.DateTime)]
        public DateTime? DateApproved { get; set; }

        /// <summary>
        /// The scheduled date for the site visit.
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime? SiteVisitDate { get; set; }

        public enum PointOfContactTypeEnum {
            Contact = 0,
            PropertyManager = 1
        }

        // SaaS Refactor: Tenant scope
        public int TenantId { get; set; }

        // Concurrency token
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        
        // Demo Mode
        public bool IsDemo { get; set; } = false;
        
        // Additional fields for demo data
        public string? Name { get; set; }
        public DateTime? StudyDate { get; set; }
        public DateTime? FiscalYearEnd { get; set; }
        public decimal? CurrentReserveFunds { get; set; }
        public decimal? MonthlyReserveContribution { get; set; }
        public decimal? AnnualInflationRate { get; set; }
        public decimal? AnnualInterestRate { get; set; }
        public string? StudyType { get; set; }
        public string? PreparedBy { get; set; }
        public string? Notes { get; set; }
    }
}
