using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    public class KanbanTask : BaseModel {
        public required string Title { get; set; }
        public string? Description { get; set; }
        public KanbanStatus Status { get; set; } = KanbanStatus.Todo;
        public Priority Priority { get; set; } = Priority.Medium;
        public DateTime? DueDate { get; set; }
        public Guid? AssigneeId { get; set; }
        public string? AssigneeName { get; set; }
        public Guid? ReserveStudyId { get; set; }
    }

    public enum KanbanStatus {
        Todo,
        InProgress,
        Review,
        Done
    }

    public enum Priority {
        Low,
        Medium,
        High,
        Critical
    }
}
