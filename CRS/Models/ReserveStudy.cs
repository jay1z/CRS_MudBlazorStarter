﻿using System.ComponentModel.DataAnnotations;
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

        [DataType(DataType.DateTime)]
        public DateTime? DateApproved { get; set; }

        // Add this property for optimistic concurrency control
        [Timestamp]
        public byte[] RowVersion { get; set; }

        public DateTime? LastModified { get; set; }

        public enum PointOfContactTypeEnum {
            Contact = 0,
            PropertyManager = 1
        }
    }
}
