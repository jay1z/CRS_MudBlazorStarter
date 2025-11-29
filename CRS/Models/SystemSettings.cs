using System;
using System.ComponentModel.DataAnnotations;

namespace CRS.Models
{
    /// <summary>
    /// System-wide settings that can be configured by platform administrators.
    /// These settings apply globally across all tenants or to the platform itself.
    /// </summary>
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        // Global Banner Settings
        public bool ShowGlobalBanner { get; set; } = false;
        public string? GlobalBannerMessage { get; set; }
        public BannerSeverity GlobalBannerSeverity { get; set; } = BannerSeverity.Info;
        public DateTime? GlobalBannerStartDate { get; set; }
        public DateTime? GlobalBannerEndDate { get; set; }

        // Maintenance Mode
        public bool MaintenanceModeEnabled { get; set; } = false;
        public string? MaintenanceMessage { get; set; }
        public DateTime? MaintenanceStartTime { get; set; }
        public DateTime? MaintenanceEndTime { get; set; }

        // System-wide Feature Flags
        public bool AllowNewTenantSignups { get; set; } = true;
        public bool AllowNewUserRegistrations { get; set; } = true;
        public bool EnableEmailNotifications { get; set; } = true;
        public bool EnableSmsNotifications { get; set; } = false;

        // Platform Limits
        public int MaxTenantsAllowed { get; set; } = 1000;
        public int MaxUsersPerTenant { get; set; } = 100;
        public int MaxCommunitiesPerTenant { get; set; } = 50;

        // Support & Contact
        public string? SupportEmail { get; set; }
        public string? SupportPhone { get; set; }
        public string? SupportUrl { get; set; }
        
        // System Information
        public string? SystemVersion { get; set; }
        public string? AnnouncementUrl { get; set; }
        public string? DocumentationUrl { get; set; }
        public string? StatusPageUrl { get; set; }

        // Announcement/News Settings
        public bool ShowAnnouncementBanner { get; set; } = false;
        public string? AnnouncementTitle { get; set; }
        public string? AnnouncementMessage { get; set; }
        public DateTime? AnnouncementStartDate { get; set; }
        public DateTime? AnnouncementEndDate { get; set; }

        // Default Tenant Limits (for new signups)
        public int DefaultMaxCommunities { get; set; } = 10;
        public int DefaultMaxUsers { get; set; } = 5;
        public int DefaultTrialDays { get; set; } = 14;
        public bool RequirePaymentForSignup { get; set; } = false;

        // Session Settings
        public int SessionTimeoutMinutes { get; set; } = 480; // 8 hours default
        public int InactivityWarningMinutes { get; set; } = 5;
        public bool RequireReauthenticationForSensitiveActions { get; set; } = true;

        // API & Rate Limiting
        public bool EnableApiRateLimiting { get; set; } = true;
        public int ApiRequestsPerMinute { get; set; } = 60;
        public int ApiRequestsPerHour { get; set; } = 1000;

        // Backup & Data Retention
        public bool EnableAutomatedBackups { get; set; } = true;
        public int BackupIntervalHours { get; set; } = 24;
        public int BackupRetentionDays { get; set; } = 30;
        public int AuditLogRetentionDays { get; set; } = 90;
        public int DeletedDataRetentionDays { get; set; } = 30;

        // Compliance & Privacy
        public bool EnableGdprCompliance { get; set; } = true;
        public bool AllowDataExport { get; set; } = true;
        public bool AllowAccountDeletion { get; set; } = true;
        public string? PrivacyPolicyUrl { get; set; }
        public string? TermsOfServiceUrl { get; set; }
        public int DataRetentionYears { get; set; } = 7;

        // Reporting Settings
        public bool EnableAutomatedReports { get; set; } = true;
        public string? DefaultReportFormat { get; set; } = "PDF";
        public string? DefaultReportLogoUrl { get; set; }
        public bool IncludeWatermarkOnReports { get; set; } = false;

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? LastUpdatedBy { get; set; }
    }

    /// <summary>
    /// Severity levels for the global banner
    /// </summary>
    public enum BannerSeverity
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3
    }
}
