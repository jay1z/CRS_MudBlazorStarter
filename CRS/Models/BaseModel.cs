using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRS.Models {
    public class BaseModel {
        public BaseModel() {
            Id = Guid.CreateVersion7();
            DateCreated = DateTime.Now;
        }
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [DataType(DataType.DateTime)] public DateTime? DateCreated { get; set; }
        [DataType(DataType.DateTime)] public DateTime? DateModified { get; set; }
        [DataType(DataType.DateTime)] public DateTime? DateDeleted { get; set; }
    }
}
