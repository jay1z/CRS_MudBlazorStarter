using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface ISignalRService {
        Task NotifyTaskUpdated(Guid reserveStudyId);
        Task NotifyTaskModified(Guid reserveStudyId, KanbanTask task);
    }
}
