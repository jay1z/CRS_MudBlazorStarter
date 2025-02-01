using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class BaseModel {
        public int Id { get; set; }
        [DataType(DataType.DateTime)] public DateTime? DateCreated { get; set; }
        [DataType(DataType.DateTime)] public DateTime? DateModified { get; set; }
        [DataType(DataType.DateTime)] public DateTime? DateDeleted { get; set; }
    }
}
