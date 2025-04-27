using Microsoft.AspNetCore.SignalR;
using CRS.Hubs;
using CRS.Services.Interfaces;
using CRS.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace CRS.Services {
    public class SignalRService : ISignalRService {
        private readonly IHubContext<KanbanHub> _hubContext;

        public SignalRService(IHubContext<KanbanHub> hubContext) {
            _hubContext = hubContext;
        }

        public async Task NotifyTaskUpdated(Guid reserveStudyId) {
            try {
                await _hubContext.Clients.All.SendAsync("ReceiveTaskUpdate", reserveStudyId.ToString());
            }
            catch (Exception ex) {
                // Log but don't throw to prevent cascading failures
                Console.WriteLine($"Error in SignalR notification: {ex.Message}");
            }
        }

        public async Task NotifyTaskModified(Guid reserveStudyId, KanbanTask task) {
            try {
                // Serialize the task to JSON
                var taskJson = JsonSerializer.Serialize(task);
                await _hubContext.Clients.All.SendAsync("ReceiveTaskModified", reserveStudyId.ToString(), taskJson);
            }
            catch (Exception ex) {
                Console.WriteLine($"Error in SignalR task notification: {ex.Message}");
                // Fall back to standard notification
                await NotifyTaskUpdated(reserveStudyId);
            }
        }
    }
}
