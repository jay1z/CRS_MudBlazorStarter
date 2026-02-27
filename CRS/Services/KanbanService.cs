using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services {
    public class KanbanService : IKanbanService {
        private readonly ApplicationDbContext _context;
        private readonly ISignalRService _signalRService;

        public KanbanService(ApplicationDbContext context, ISignalRService signalRService) {
            _context = context;
            _signalRService = signalRService;
        }

        public async Task<List<KanbanTask>> GetTasksForReserveStudyAsync(Guid reserveStudyId) {
            return await _context.KanbanTasks
                .Where(t => t.ReserveStudyId == reserveStudyId && !t.DateDeleted.HasValue)
                .OrderBy(t => t.Status)
                .ThenBy(t => t.Priority)
                .ToListAsync();
        }

        public async Task<KanbanTask> CreateTaskAsync(KanbanTask task) {
            task.DateCreated = DateTime.UtcNow;
            _context.KanbanTasks.Add(task);
            await _context.SaveChangesAsync();

            // Use the SignalRService instead of direct hub access
            if (task.ReserveStudyId.HasValue)
                await _signalRService.NotifyTaskUpdated(task.ReserveStudyId.Value);

            return task;
        }

        public async Task<KanbanTask> UpdateTaskAsync(KanbanTask task) {
            var existingTask = await _context.KanbanTasks
                .FirstOrDefaultAsync(t => t.Id == task.Id && !t.DateDeleted.HasValue);

            if (existingTask == null)
                throw new KeyNotFoundException($"Task with ID {task.Id} not found");

            existingTask.Title = task.Title;
            existingTask.Description = task.Description;
            existingTask.Status = task.Status;
            existingTask.Priority = task.Priority;
            existingTask.DueDate = task.DueDate;
            existingTask.AssigneeId = task.AssigneeId;
            existingTask.AssigneeName = task.AssigneeName;
            existingTask.DateModified = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (existingTask.ReserveStudyId.HasValue)
                await _signalRService.NotifyTaskModified(existingTask.ReserveStudyId.Value, existingTask);

            return existingTask;
        }

        public async Task DeleteTaskAsync(Guid taskId) {
            var task = await _context.KanbanTasks.FindAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found");

            // Get the study ID for notification before deleting
            var studyId = task.ReserveStudyId;

            // Soft delete
            task.DateDeleted = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (studyId.HasValue)
                await _signalRService.NotifyTaskModified(studyId.Value, task);
        }

        public async Task UpdateTaskStatusAsync(Guid taskId, KanbanStatus newStatus) {
            var task = await _context.KanbanTasks.FindAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found");

            task.Status = newStatus;
            task.DateModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (task.ReserveStudyId.HasValue)
                await _signalRService.NotifyTaskModified(task.ReserveStudyId.Value, task);
        }

        public async Task UpdateTaskAssignmentAsync(Guid taskId, Guid? assigneeId, string? assigneeName, KanbanStatus? newStatus = null) {
            var task = await _context.KanbanTasks.FindAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found");

            task.AssigneeId = assigneeId;
            task.AssigneeName = assigneeName;

            if (newStatus.HasValue) {
                task.Status = newStatus.Value;
            }

            task.DateModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            if (task.ReserveStudyId.HasValue)
                await _signalRService.NotifyTaskModified(task.ReserveStudyId.Value, task);
        }
    }
}
