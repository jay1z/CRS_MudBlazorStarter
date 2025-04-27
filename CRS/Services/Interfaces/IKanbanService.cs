using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface IKanbanService {
        Task<List<KanbanTask>> GetTasksForReserveStudyAsync(Guid reserveStudyId);
        Task<KanbanTask> CreateTaskAsync(KanbanTask task);
        Task<KanbanTask> UpdateTaskAsync(KanbanTask task);
        Task DeleteTaskAsync(Guid taskId);
        Task UpdateTaskStatusAsync(Guid taskId, KanbanStatus newStatus);
        Task UpdateTaskAssignmentAsync(Guid taskId, Guid? assigneeId, string? assigneeName, KanbanStatus? newStatus = null);

    }
}
