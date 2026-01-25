using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static CRS.Models.IReserveStudyElement;
using static MudBlazor.CategoryTypes;

namespace CRS.Models {
    public class ReserveStudyAdditionalElement : BaseModel, IReserveStudyElement {
        public ReserveStudyAdditionalElement() {
            ServiceContact ??= new ServiceContact();
        }
        public Guid? ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required string Name { get; set; }
        public int Count { get; set; }
        public bool NeedsService { get; set; }
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

        [NotMapped]
        public ElementTypeEnum ElementType { get => ElementTypeEnum.Additional; set => throw new NotImplementedException(); }

        [NotMapped]
        public bool ShowDetails { get; set; } = false;

    }
}
