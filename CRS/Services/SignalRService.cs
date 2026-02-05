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
        private readonly ILogger<SignalRService> _logger;

        public SignalRService(IHubContext<KanbanHub> hubContext, ILogger<SignalRService> logger) {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task NotifyTaskUpdated(Guid reserveStudyId) {
            try {
                await _hubContext.Clients.All.SendAsync("ReceiveTaskUpdate", reserveStudyId.ToString());
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error in SignalR task update notification");
            }
        }

        public async Task NotifyTaskModified(Guid reserveStudyId, KanbanTask task) {
            try {
                var taskJson = JsonSerializer.Serialize(task);
                await _hubContext.Clients.All.SendAsync("ReceiveTaskModified", reserveStudyId.ToString(), taskJson);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error in SignalR task modified notification");
                await NotifyTaskUpdated(reserveStudyId);
            }
        }

        public async Task SendNotificationToUserAsync(Guid userId, Notification notification) {
            try {
                // Send to the user's group (users are added to a group named after their user ID)
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveNotification", new {
                    id = notification.Id,
                    title = notification.Title,
                    message = notification.Message,
                    type = notification.Type.ToString(),
                    priority = notification.Priority.ToString(),
                    actionUrl = notification.ActionUrl,
                    createdAt = notification.DateCreated
                });

                _logger.LogDebug("Sent notification to user {UserId}: {Title}", userId, notification.Title);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error sending SignalR notification to user {UserId}", userId);
            }
        }

        public async Task NotifyUnreadCountAsync(Guid userId, int unreadCount) {
            try {
                await _hubContext.Clients.Group(userId.ToString()).SendAsync("ReceiveUnreadCount", unreadCount);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Error sending unread count to user {UserId}", userId);
            }
        }
    }
}
