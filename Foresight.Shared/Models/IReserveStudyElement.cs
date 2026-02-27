using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Horizon.Models {
public interface IReserveStudyElement {
    public int Count { get; set; }
    public ElementTypeEnum ElementType { get; set; }
        
    // Phase 2: Consolidated element options
    public ElementOption? MeasurementOption { get; set; }
    public ElementOption? RemainingLifeOption { get; set; }
    public ElementOption? UsefulLifeOption { get; set; }
        
    [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
    public ServiceContact? ServiceContact { get; set; }

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
