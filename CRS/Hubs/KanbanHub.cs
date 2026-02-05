using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace CRS.Hubs {
    public class KanbanHub : Hub {
        public async Task NotifyTaskUpdated(string reserveStudyId) {
            await Clients.All.SendAsync("ReceiveTaskUpdate", reserveStudyId);
        }

        public async Task NotifyTaskModified(string reserveStudyId, string taskJson) {
            await Clients.All.SendAsync("ReceiveTaskModified", reserveStudyId, taskJson);
        }

        /// <summary>
        /// Called when a client connects. Adds the user to their personal notification group.
        /// </summary>
        public override async Task OnConnectedAsync() {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId)) {
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects. Removes the user from their notification group.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception) {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId)) {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
