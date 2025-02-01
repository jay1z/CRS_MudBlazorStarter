namespace CRS.Models {
    public class ElementMeasurementOptions : BaseModel {
        public required string Unit { get; set; }
        public required string DisplayText { get; set; }
        public required int ZOrder { get; set; }
    }
}
