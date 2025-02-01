using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class Notification : BaseModel {
        [DataType(DataType.Text)] public required string Message { get; set; }
        public required int Type { get; set; }
        public required string Status { get; set; }
    }
}
