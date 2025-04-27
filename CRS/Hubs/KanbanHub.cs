using Microsoft.AspNetCore.SignalR;

namespace CRS.Hubs {
    public class KanbanHub : Hub {
        public async Task NotifyTaskUpdated(string reserveStudyId) {
            await Clients.All.SendAsync("ReceiveTaskUpdate", reserveStudyId);
        }

        public async Task NotifyTaskModified(string reserveStudyId, string taskJson) {
            await Clients.All.SendAsync("ReceiveTaskModified", reserveStudyId, taskJson);
        }
    }
}
