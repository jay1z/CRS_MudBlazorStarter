using CRS.Services.Interfaces;
using CRS.Services;
using CRS.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CRS.Services {
    public class AppNotificationService : IAppNotificationService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public AppNotificationService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }
        public Task NotifyMessageReceivedAsync(Guid toUserId, Guid messageId) {
            // Placeholder for future SignalR push or email.
            return Task.CompletedTask;
        }
    }
}