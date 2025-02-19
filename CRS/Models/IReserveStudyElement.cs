using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRS.Models {
    public interface IReserveStudyElement {
        public int Count { get; set; }
        public ElementTypeEnum ElementType { get; set; }
        public ElementMeasurementOptions ElementMeasurementOptions { get; set; }
        public ElementRemainingLifeOptions ElementRemainingLifeOptions { get; set; }
        public ElementUsefulLifeOptions ElementUsefulLifeOptions { get; set; }
        [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
        public ServiceContact ServiceContact { get; set; }

        string Name { get; set; }
        bool NeedsService { get; set; }
        bool ShowDetails { get; set; }

        public enum ElementTypeEnum {
            Additional,
            Building,
            Common
        }
    }
}
