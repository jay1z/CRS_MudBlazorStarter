using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static CRS.Models.IReserveStudyElement;

namespace CRS.Models {
    public class ReserveStudyBuildingElement : BaseModel, IReserveStudyElement {
        public ReserveStudyBuildingElement() {
            ServiceContact ??= new ServiceContact();
        }
        public required Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required Guid BuildingElementId { get; set; }
        public BuildingElement? BuildingElement { get; set; }
        public int Count { get; set; } = 1;
        public ServiceContact? ServiceContact { get; set; }
        [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
        
        // Phase 2: Consolidated element options with foreign keys
        public Guid? MeasurementOptionId { get; set; }
        public ElementOption? MeasurementOption { get; set; }
        
        /// <summary>
        /// Remaining life in years (simple integer value)
        /// </summary>
        public int? RemainingLifeYears { get; set; }

        // Legacy: kept for migration compatibility
        public Guid? RemainingLifeOptionId { get; set; }
        public ElementOption? RemainingLifeOption { get; set; }

        /// <summary>
        /// Minimum useful life option
        /// </summary>
        public Guid? MinUsefulLifeOptionId { get; set; }
        public ElementOption? MinUsefulLifeOption { get; set; }

        /// <summary>
        /// Maximum useful life option
        /// </summary>
        public Guid? MaxUsefulLifeOptionId { get; set; }
        public ElementOption? MaxUsefulLifeOption { get; set; }

        // Legacy: kept for migration compatibility
        public Guid? UsefulLifeOptionId { get; set; }
        public ElementOption? UsefulLifeOption { get; set; }

        /// <summary>
        /// Current replacement cost for this element (used in funding plan calculations)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ReplacementCost { get; set; }

        [NotMapped]
        public ElementTypeEnum ElementType { get => ElementTypeEnum.Building; set => throw new NotImplementedException(); }

        [NotMapped]
        public string Name { get { return BuildingElement?.Name ?? throw new ArgumentNullException(); } set { } }

        [NotMapped]
        public bool NeedsService { get { return BuildingElement?.NeedsService ?? false; } set => throw new NotImplementedException();  }

        [NotMapped]
        public bool ShowDetails { get; set; } = false;
    }
}
