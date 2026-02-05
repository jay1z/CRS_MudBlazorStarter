using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class DashboardService : IDashboardService {
        private readonly ApplicationDbContext _context;
        private readonly ISignalRService _signalRService;

        public IKanbanService? KanbanService { get; }

        public DashboardService(ApplicationDbContext context, IKanbanService? kanbanService = null, ISignalRService? signalRService = null) {
            _context = context;
            KanbanService = kanbanService;
            _signalRService = signalRService ?? new NullSignalRService();
        }

        public async Task<List<KanbanTask>> GenerateTasksBasedOnStudyStatusAsync(Guid studyId) {
            var newTasks = new List<KanbanTask>();
            var existingTasks = KanbanService != null ?
                await KanbanService.GetTasksForReserveStudyAsync(studyId) :
                new List<KanbanTask>();

            // Get the current study for detailed information
            var study = await _context.ReserveStudies
                .Include(s => s.Specialist)
                .Include(s => s.ReserveStudyBuildingElements)
                .Include(s => s.ReserveStudyCommonElements)
                .Include(s => s.ReserveStudyAdditionalElements)
                .Include(s => s.Community)
                .FirstOrDefaultAsync(s => s.Id == studyId);

            if (study == null) {
                throw new Exception("Reserve study not found");
            }

            // Get a specialist to assign tasks to (if available)
            ApplicationUser? specialist = null;
            if (study.SpecialistUserId.HasValue) {
                specialist = await _context.Users.FirstOrDefaultAsync(u => u.Id == study.SpecialistUserId.Value);
            }

            // Define standard tasks based on study status
            var tasksToGenerate = new List<(string Title, string Description, KanbanStatus Status, Priority Priority, int DueDays)>();

            // Common tasks needed for any study
            tasksToGenerate.Add(("Initial Study Review", "Review all submitted documents and property information", KanbanStatus.Todo, Priority.High, 7));

            // Add tasks based on study status
            if (!study.IsApproved) {
                // Tasks for studies in progress
                tasksToGenerate.Add(("Site Inspection", "Visit the property to inspect all components", KanbanStatus.Todo, Priority.High, 14));
                tasksToGenerate.Add(("Financial Analysis", "Analyze current reserve fund and anticipated expenditures", KanbanStatus.Todo, Priority.Medium, 21));

                // Generate element-specific tasks based on study elements
                if (study.ReserveStudyBuildingElements?.Any() == true) {
                    tasksToGenerate.Add(("Building Elements Assessment",
                        $"Evaluate condition of {study.ReserveStudyBuildingElements.Count} building elements",
                        KanbanStatus.Todo, Priority.Medium, 10));
                }

                if (study.ReserveStudyCommonElements?.Any() == true) {
                    tasksToGenerate.Add(("Common Elements Assessment",
                        $"Evaluate condition of {study.ReserveStudyCommonElements.Count} common elements",
                        KanbanStatus.Todo, Priority.Medium, 12));
                }

                // Check for elements that need service
                var elementsNeedingService = study.ReserveStudyElements?.Where(e => e.NeedsService).ToList();
                if (elementsNeedingService?.Any() == true) {
                    tasksToGenerate.Add(("Service Request Follow-up",
                        $"Follow up on {elementsNeedingService.Count()} elements needing service",
                        KanbanStatus.Todo, Priority.High, 5));
                }
            } else if (study.IsApproved && !study.IsComplete) {
                // Tasks for approved but not completed studies
                tasksToGenerate.Add(("Prepare Final Report", "Compile all findings into final reserve study report", KanbanStatus.Todo, Priority.Critical, 7));
                tasksToGenerate.Add(("Client Review Meeting", "Schedule meeting to review findings with client", KanbanStatus.Todo, Priority.High, 14));
            }

            // Check for community-specific tasks
            if (study.Community != null) {
                tasksToGenerate.Add(($"Review {study.Community.Name} Specific Requirements",
                    "Check for any special requirements or local regulations",
                    KanbanStatus.Todo, Priority.Medium, 5));
            }

            // Create tasks that don't already exist
            foreach (var taskDefinition in tasksToGenerate) {
                // Check if this task already exists
                bool taskExists = existingTasks.Any(t =>
                    t.Title == taskDefinition.Title &&
                    t.Description == taskDefinition.Description &&
                    !t.DateDeleted.HasValue); // Ensure we're not counting deleted tasks

                if (!taskExists) {
                    newTasks.Add(new KanbanTask {
                        Title = taskDefinition.Title,
                        Description = taskDefinition.Description,
                        Status = taskDefinition.Status,
                        Priority = taskDefinition.Priority,
                        DueDate = DateTime.Now.AddDays(taskDefinition.DueDays),
                        AssigneeId = specialist?.Id,
                        AssigneeName = specialist?.UserName,
                        ReserveStudyId = studyId
                    });
                }
            }

            return newTasks;
        }

        public async Task CreateTasksForStudyAsync(List<KanbanTask> tasks) {
            if (KanbanService == null)
                throw new InvalidOperationException("KanbanService is not available");

            foreach (var task in tasks) {
                await KanbanService.CreateTaskAsync(task);
            }
        }

        private class NullSignalRService : ISignalRService {
            public Task NotifyTaskUpdated(Guid studyId) => Task.CompletedTask;
            public Task NotifyTaskModified(Guid studyId, KanbanTask task) => Task.CompletedTask;
            public Task SendNotificationToUserAsync(Guid userId, Notification notification) => Task.CompletedTask;
            public Task NotifyUnreadCountAsync(Guid userId, int unreadCount) => Task.CompletedTask;
        }
    }
}