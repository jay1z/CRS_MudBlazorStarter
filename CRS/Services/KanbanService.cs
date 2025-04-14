using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class KanbanService : IKanbanService {
        private readonly ApplicationDbContext _context;

        public KanbanService(ApplicationDbContext context) {
            _context = context;
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
            return existingTask;
        }

        public async Task DeleteTaskAsync(Guid taskId) {
            var task = await _context.KanbanTasks.FindAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found");

            // Soft delete
            task.DateDeleted = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTaskStatusAsync(Guid taskId, KanbanStatus newStatus) {
            var task = await _context.KanbanTasks.FindAsync(taskId);
            if (task == null)
                throw new KeyNotFoundException($"Task with ID {taskId} not found");

            task.Status = newStatus;
            task.DateModified = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
