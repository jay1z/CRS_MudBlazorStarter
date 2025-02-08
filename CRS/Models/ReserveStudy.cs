using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

using CRS.Services;

using static CRS.Services.HashingService;

namespace CRS.Models {
    public class ReserveStudy : BaseModel {
        [ForeignKey("User")]
        public string ApplicationUserId { get; set; }
        public ApplicationUser? User { get; set; }

        [ForeignKey("Specialist")]
        public string? SpecialistUserId { get; set; }
        public ApplicationUser? Specialist { get; set; }

        public Community Community { get; set; }
        public PointOfContactTypeEnum PointOfContactType { get; set; }

        public Contact Contact { get; set; }
        public PropertyManager? PropertyManager { get; set; }

        public List<ReserveStudyBuildingElement>? ReserveStudyBuildingElements { get; set; }
        public List<ReserveStudyCommonElement>? ReserveStudyCommonElements { get; set; }
        public List<ReserveStudyAdditionalElement>? ReserveStudyAdditionalElements { get; set; }

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
        [DataType(DataType.DateTime)] public DateTime? DateApproved { get; set; }

        public string HashedId => new HashingService().HashId(Id, HashType.ReserveStudy);

        public enum PointOfContactTypeEnum {
            Contact,
            PropertyManager
        }
    }
}
