using CRS.Data;

namespace CRS.Models {
    public class AuditLog : BaseModel {
        public required ApplicationUser User { get; set; }
        public required string ApplicationUserId { get; set; }
        public required string Action { get; set; }
        public required string TableName { get; set; }
        public required string RecordId { get; set; }
        public required string ColumnName { get; set; }
        public required string OldValue { get; set; }
        public required string NewValue { get; set; }
    }
}
