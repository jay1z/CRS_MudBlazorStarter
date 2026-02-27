using System;
using System.Threading.Tasks;

using Horizon.Data;

using Microsoft.EntityFrameworkCore;

namespace Horizon.Services {
    public interface IUserSettingsService {
        Task<string?> GetSettingAsync(Guid userId, string key, CancellationToken ct = default);
        Task SetSettingAsync(Guid userId, string key, string? value, CancellationToken ct = default);
    }

    public class UserSettingsService : IUserSettingsService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        public UserSettingsService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }

        public async Task<string?> GetSettingAsync(Guid userId, string key, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var s = await db.Settings.FirstOrDefaultAsync(x => x.ApplicationUserId == userId && x.Key == key, ct);
            return s?.Value;
        }

        public async Task SetSettingAsync(Guid userId, string key, string? value, CancellationToken ct = default) {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var setting = await db.Settings.FirstOrDefaultAsync(x => x.ApplicationUserId == userId && x.Key == key, ct);
            if (setting == null) {
                setting = new Models.Settings {
                    ApplicationUserId = userId,
                    Class = "UserSettings",
                    Context = "TenantSelection",
                    Key = key,
                    Type = "string",
                    Value = value ?? string.Empty
                };
                db.Settings.Add(setting);
            } else {
                setting.Value = value ?? string.Empty;
                db.Settings.Update(setting);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
