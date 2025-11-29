using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static CRS.Models.IReserveStudyElement;

namespace CRS.Models {
    public class ReserveStudyCommonElement : BaseModel, IReserveStudyElement {
        public ReserveStudyCommonElement() {
            ServiceContact ??= new ServiceContact();
        }
        public required Guid ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required Guid CommonElementId { get; set; }
        public CommonElement? CommonElement { get; set; }
        public int Count { get; set; }
        public ServiceContact? ServiceContact { get; set; }
        [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
        public ElementMeasurementOptions? ElementMeasurementOptions { get; set; }
        public ElementRemainingLifeOptions? ElementRemainingLifeOptions { get; set; }
        public ElementUsefulLifeOptions? ElementUsefulLifeOptions { get; set; }

        [NotMapped]
        public List<ElementMeasurementOptions> ElementMeasurementOptionsList { get; set; } = new();
        [NotMapped]
        public List<ElementUsefulLifeOptions> ElementUsefulLifeOptionsList { get; set; } = new();
        [NotMapped]
        public List<ElementRemainingLifeOptions> ElementRemainingLifeOptionsList { get; set; } = new();

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
