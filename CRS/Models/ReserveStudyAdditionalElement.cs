using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class ReserveStudyAdditionalElement : BaseModel {
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
    }
}
