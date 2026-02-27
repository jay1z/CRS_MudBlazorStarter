using Horizon.Data;
using Horizon.Models;
using Horizon.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Horizon.Services
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SystemSettingsService> _logger;
        private const string CacheKey = "SystemSettings";
        private readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public SystemSettingsService(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            IMemoryCache cache,
            ILogger<SystemSettingsService> logger)
        {
            _dbFactory = dbFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<SystemSettings> GetSettingsAsync()
        {
            // Try to get from cache first
            if (_cache.TryGetValue(CacheKey, out SystemSettings? cachedSettings) && cachedSettings != null)
            {
                return cachedSettings;
            }

            await using var db = await _dbFactory.CreateDbContextAsync();
            var settings = await db.SystemSettings.FirstOrDefaultAsync();

            if (settings == null)
            {
                // Create default settings if none exist
                settings = new SystemSettings
                {
                    SupportEmail = "support@alxreservecloud.com",
                    DocumentationUrl = "https://alxreservecloud.com/docs",
                    StatusPageUrl = "https://alxreservecloud.com/status",
                    SystemVersion = "1.0.0"
                };

                db.SystemSettings.Add(settings);
                await db.SaveChangesAsync();
                _logger.LogInformation("Created default system settings");
            }

            // Cache the settings
            _cache.Set(CacheKey, settings, CacheDuration);

            return settings;
        }

        public async Task<SystemSettings> UpdateSettingsAsync(SystemSettings settings)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            
            var existing = await db.SystemSettings.FirstOrDefaultAsync();
            
            if (existing == null)
            {
                settings.CreatedAt = DateTime.UtcNow;
                db.SystemSettings.Add(settings);
            }
            else
            {
                // Update Global Banner
                existing.ShowGlobalBanner = settings.ShowGlobalBanner;
                existing.GlobalBannerMessage = settings.GlobalBannerMessage;
                existing.GlobalBannerSeverity = settings.GlobalBannerSeverity;
                existing.GlobalBannerStartDate = settings.GlobalBannerStartDate;
                existing.GlobalBannerEndDate = settings.GlobalBannerEndDate;
                
                // Update Maintenance Mode
                existing.MaintenanceModeEnabled = settings.MaintenanceModeEnabled;
                existing.MaintenanceMessage = settings.MaintenanceMessage;
                existing.MaintenanceStartTime = settings.MaintenanceStartTime;
                existing.MaintenanceEndTime = settings.MaintenanceEndTime;
                
                // Update Feature Flags
                existing.AllowNewTenantSignups = settings.AllowNewTenantSignups;
                existing.AllowNewUserRegistrations = settings.AllowNewUserRegistrations;
                existing.EnableEmailNotifications = settings.EnableEmailNotifications;
                existing.EnableSmsNotifications = settings.EnableSmsNotifications;
                
                // Update Platform Limits
                existing.MaxTenantsAllowed = settings.MaxTenantsAllowed;
                existing.MaxUsersPerTenant = settings.MaxUsersPerTenant;
                existing.MaxCommunitiesPerTenant = settings.MaxCommunitiesPerTenant;
                
                // Update Support & Contact
                existing.SupportEmail = settings.SupportEmail;
                existing.SupportPhone = settings.SupportPhone;
                existing.SupportUrl = settings.SupportUrl;
                existing.SystemVersion = settings.SystemVersion;
                existing.AnnouncementUrl = settings.AnnouncementUrl;
                existing.DocumentationUrl = settings.DocumentationUrl;
                existing.StatusPageUrl = settings.StatusPageUrl;
                
                // Update Announcement/News
                existing.ShowAnnouncementBanner = settings.ShowAnnouncementBanner;
                existing.AnnouncementTitle = settings.AnnouncementTitle;
                existing.AnnouncementMessage = settings.AnnouncementMessage;
                existing.AnnouncementStartDate = settings.AnnouncementStartDate;
                existing.AnnouncementEndDate = settings.AnnouncementEndDate;
                
                // Update Default Tenant Limits
                existing.DefaultMaxCommunities = settings.DefaultMaxCommunities;
                existing.DefaultMaxUsers = settings.DefaultMaxUsers;
                existing.DefaultTrialDays = settings.DefaultTrialDays;
                existing.RequirePaymentForSignup = settings.RequirePaymentForSignup;
                
                // Update Session Settings
                existing.SessionTimeoutMinutes = settings.SessionTimeoutMinutes;
                existing.InactivityWarningMinutes = settings.InactivityWarningMinutes;
                existing.RequireReauthenticationForSensitiveActions = settings.RequireReauthenticationForSensitiveActions;
                
                // Update API & Rate Limiting
                existing.EnableApiRateLimiting = settings.EnableApiRateLimiting;
                existing.ApiRequestsPerMinute = settings.ApiRequestsPerMinute;
                existing.ApiRequestsPerHour = settings.ApiRequestsPerHour;
                
                // Update Backup & Data Retention
                existing.EnableAutomatedBackups = settings.EnableAutomatedBackups;
                existing.BackupIntervalHours = settings.BackupIntervalHours;
                existing.BackupRetentionDays = settings.BackupRetentionDays;
                existing.AuditLogRetentionDays = settings.AuditLogRetentionDays;
                existing.DeletedDataRetentionDays = settings.DeletedDataRetentionDays;
                
                // Update Compliance & Privacy
                existing.EnableGdprCompliance = settings.EnableGdprCompliance;
                existing.AllowDataExport = settings.AllowDataExport;
                existing.AllowAccountDeletion = settings.AllowAccountDeletion;
                existing.PrivacyPolicyUrl = settings.PrivacyPolicyUrl;
                existing.TermsOfServiceUrl = settings.TermsOfServiceUrl;
                existing.DataRetentionYears = settings.DataRetentionYears;
                
                // Update Reporting Settings
                existing.EnableAutomatedReports = settings.EnableAutomatedReports;
                existing.DefaultReportFormat = settings.DefaultReportFormat;
                existing.DefaultReportLogoUrl = settings.DefaultReportLogoUrl;
                existing.IncludeWatermarkOnReports = settings.IncludeWatermarkOnReports;
                
                existing.UpdatedAt = DateTime.UtcNow;
                existing.LastUpdatedBy = settings.LastUpdatedBy;
            }

            await db.SaveChangesAsync();
            
            // Clear cache
            _cache.Remove(CacheKey);
            
            _logger.LogInformation("System settings updated by {UpdatedBy}", settings.LastUpdatedBy);
            
            return await GetSettingsAsync();
        }

        public async Task<bool> ShouldShowGlobalBannerAsync()
        {
            var settings = await GetSettingsAsync();
            
            if (!settings.ShowGlobalBanner || string.IsNullOrWhiteSpace(settings.GlobalBannerMessage))
            {
                return false;
            }

            var now = DateTime.UtcNow;

            // Check if banner is within the active date range
            if (settings.GlobalBannerStartDate.HasValue && now < settings.GlobalBannerStartDate.Value)
            {
                return false;
            }

            if (settings.GlobalBannerEndDate.HasValue && now > settings.GlobalBannerEndDate.Value)
            {
                return false;
            }

            return true;
        }

        public async Task<GlobalBannerInfo?> GetGlobalBannerInfoAsync()
        {
            if (!await ShouldShowGlobalBannerAsync())
            {
                return null;
            }

            var settings = await GetSettingsAsync();

            return new GlobalBannerInfo
            {
                Message = settings.GlobalBannerMessage!,
                Severity = settings.GlobalBannerSeverity,
                StartDate = settings.GlobalBannerStartDate,
                EndDate = settings.GlobalBannerEndDate
            };
        }

        public async Task<bool> IsMaintenanceModeAsync()
        {
            var settings = await GetSettingsAsync();
            
            if (!settings.MaintenanceModeEnabled)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            // Check if maintenance is within the scheduled time window
            if (settings.MaintenanceStartTime.HasValue && now < settings.MaintenanceStartTime.Value)
            {
                return false;
            }

            if (settings.MaintenanceEndTime.HasValue && now > settings.MaintenanceEndTime.Value)
            {
                return false;
            }

            return true;
        }
    }
}
