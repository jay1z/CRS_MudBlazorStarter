using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

namespace CRS.Models {
    public class AuditLog : BaseModel {
        public ApplicationUser? User { get; set; }
        [ForeignKey(nameof(ApplicationUser))] public Guid? ApplicationUserId { get; set; }
        public string? Action { get; set; }
        public string? TableName { get; set; }
        public string? RecordId { get; set; }
        public string? ColumnName { get; set; }
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}
