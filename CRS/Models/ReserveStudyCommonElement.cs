using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static Horizon.Models.IReserveStudyElement;

namespace Horizon.Models {
    public class ReserveStudyCommonElement : BaseModel, IReserveStudyElement {
        public required Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required Guid CommonElementId { get; set; }
        public CommonElement? CommonElement { get; set; }
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

        [NotMapped]
        public ElementTypeEnum ElementType { get => ElementTypeEnum.Common; set => throw new NotImplementedException(); }

        [NotMapped]
        public string Name { get { return CommonElement?.Name ?? throw new ArgumentNullException(); } set { } }

        [NotMapped]
        public bool NeedsService { get { return CommonElement?.NeedsService ?? false; } set => throw new NotImplementedException();  }

        [NotMapped]
        public bool ShowDetails { get; set; } = false;
        
        // Additional fields for demo data (when CommonElement is null)
        public string? ElementName { get; set; }
        public string? Description { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public int? UsefulLife { get; set; }
        public int? RemainingLife { get; set; }
        public decimal? ReplacementCost { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
    }
}
