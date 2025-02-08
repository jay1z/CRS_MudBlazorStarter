using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRS.Models {
    public class ReserveStudyCommonElement : BaseModel {
        public required int ReserveStudyId { get; set; }
        public ReserveStudy? ReserveStudy { get; set; }

        public required int CommonElementId { get; set; }
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
        public bool ShowDetails { get; set; } = false;
    }
}
