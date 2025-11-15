using System.Threading.Tasks;

namespace CRS.Services.Interfaces {
    public interface IAppNotificationService {
        Task NotifyMessageReceivedAsync(Guid toUserId, Guid messageId);
    }
}