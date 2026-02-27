using Horizon.Models;

namespace Horizon.Services.Interfaces
{
    /// <summary>
    /// Service for managing system-wide settings
    /// </summary>
    public interface ISystemSettingsService
    {
        /// <summary>
        /// Gets the current system settings. Creates default settings if none exist.
        /// </summary>
        Task<SystemSettings> GetSettingsAsync();

        /// <summary>
        /// Updates system settings
        /// </summary>
        Task<SystemSettings> UpdateSettingsAsync(SystemSettings settings);

        /// <summary>
        /// Checks if the global banner should be displayed based on current settings
        /// </summary>
        Task<bool> ShouldShowGlobalBannerAsync();

        /// <summary>
        /// Gets the global banner configuration for display
        /// </summary>
        Task<GlobalBannerInfo?> GetGlobalBannerInfoAsync();

        /// <summary>
        /// Checks if the system is in maintenance mode
        /// </summary>
        Task<bool> IsMaintenanceModeAsync();
    }

    /// <summary>
    /// DTO for global banner display information
    /// </summary>
    public class GlobalBannerInfo
    {
        public string Message { get; set; } = default!;
        public BannerSeverity Severity { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
