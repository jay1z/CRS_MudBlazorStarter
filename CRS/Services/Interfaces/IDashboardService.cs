using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface IDashboardService {
        IKanbanService? KanbanService { get; }
        Task<List<KanbanTask>> GenerateTasksBasedOnStudyStatusAsync(Guid studyId);
        Task CreateTasksForStudyAsync(List<KanbanTask> tasks);
    }
}
