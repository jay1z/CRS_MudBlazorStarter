using Horizon.Models;

namespace Horizon.Services.Interfaces {
    public interface IMessageService {
        Task<List<Message>> GetInboxAsync(Guid userId);
        Task<List<Message>> GetConversationAsync(Guid userId, Guid otherUserId);
        Task SendMessageAsync(Message message);
        Task MarkAsReadAsync(Guid messageId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<Message>> GetSentAsync(Guid userId);
        Task<List<Message>> GetLatestConversationsAsync(Guid userId);
    }
}