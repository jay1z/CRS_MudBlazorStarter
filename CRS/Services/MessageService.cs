using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Horizon.Services {
    public class MessageService : IMessageService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public MessageService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }

        public async Task<List<Message>> GetInboxAsync(Guid userId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var results = await db.Messages
                .IgnoreQueryFilters()
                .Where(m => m.ToUserId == userId)
                .OrderByDescending(m => m.DateSent)
                .AsNoTracking()
                .ToListAsync();
            
            return results ?? new List<Message>();
        }

        public async Task<List<Message>> GetConversationAsync(Guid userId, Guid otherUserId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var results = await db.Messages
                .IgnoreQueryFilters()
                .Where(m => (m.ToUserId == userId && m.FromUserId == otherUserId) || (m.ToUserId == otherUserId && m.FromUserId == userId))
                .OrderBy(m => m.DateSent)
                .AsNoTracking()
                .ToListAsync();
            
            return results ?? new List<Message>();
        }

        public async Task<List<Message>> GetSentAsync(Guid userId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var results = await db.Messages
                .IgnoreQueryFilters()
                .Where(m => m.FromUserId == userId)
                .OrderByDescending(m => m.DateSent)
                .AsNoTracking()
                .ToListAsync();
            
            return results ?? new List<Message>();
        }

        public async Task<List<Message>> GetLatestConversationsAsync(Guid userId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            try {
                // Grouped subquery to get latest DateSent per conversation partner (OtherId)
                var latestPerOther =
                    from mm in db.Messages.IgnoreQueryFilters()
                    where mm.ToUserId == userId || mm.FromUserId == userId
                    group mm by (mm.FromUserId == userId ? mm.ToUserId : mm.FromUserId) into grp
                    select new { OtherId = grp.Key, LastDate = grp.Max(x => x.DateSent) };

                // Join back to messages to fetch the full message rows corresponding to the latest dates
                var latest =
                    from m in db.Messages.AsNoTracking().IgnoreQueryFilters()
                    where m.ToUserId == userId || m.FromUserId == userId
                    join g in latestPerOther
                        on new { OtherId = (m.FromUserId == userId ? m.ToUserId : m.FromUserId), DateSent = m.DateSent }
                        equals new { g.OtherId, DateSent = g.LastDate }
                    select m;

                var results = await latest.OrderByDescending(m => m.DateSent).ToListAsync();
                return results ?? new List<Message>();
            }
            catch (Exception) {
                // Log the exception if you have a logger
                return new List<Message>();
            }
        }

        public async Task SendMessageAsync(Message message) {
            if (message == null) throw new ArgumentNullException(nameof(message));
            
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            db.Messages.Add(message);
            await db.SaveChangesAsync();
        }

        public async Task MarkAsReadAsync(Guid messageId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var m = await db.Messages.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == messageId);
            if (m != null && !m.IsRead) { 
                m.IsRead = true; 
                await db.SaveChangesAsync(); 
            }
        }

        public async Task<int> GetUnreadCountAsync(Guid userId) {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            return await db.Messages
                .IgnoreQueryFilters()
                .Where(m => m.ToUserId == userId && !m.IsRead)
                .CountAsync();
        }
    }
}
