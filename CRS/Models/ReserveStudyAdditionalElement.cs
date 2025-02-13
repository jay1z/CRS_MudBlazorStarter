using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using static CRS.Models.IReserveStudyElement;

namespace CRS.Models {
    public class ReserveStudyAdditionalElement : BaseModel, IReserveStudyElement {
        public int? ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required string Name { get; set; }
        public int Count { get; set; }
        public bool NeedsService { get; set; }
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
        public ElementTypeEnum ElementType { get => ElementTypeEnum.Additional; set => throw new NotImplementedException(); }

        [NotMapped]
        public bool ShowDetails { get; set; } = false;

    }
}
