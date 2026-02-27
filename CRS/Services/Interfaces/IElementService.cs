using Horizon.Models;

namespace Horizon.Services.Interfaces {
    /// <summary>
    /// Service interface for retrieving building and common elements with tenant-specific ordering and visibility.
    /// </summary>
    public interface IElementService {
        /// <summary>
        /// Gets building elements for the current tenant with proper ordering.
        /// </summary>
        Task<List<BuildingElement>> GetBuildingElementsAsync();

        /// <summary>
        /// Gets common elements for the current tenant with proper ordering.
        /// </summary>
        Task<List<CommonElement>> GetCommonElementsAsync();

        /// <summary>
        /// Gets the display name for an element, considering tenant-specific custom names.
        /// </summary>
        Task<string> GetElementDisplayNameAsync(Guid elementId, ElementType elementType);

        /// <summary>
        /// Saves a tenant-specific element ordering override.
        /// </summary>
        Task SaveElementOrderAsync(Guid elementId, ElementType elementType, int zOrder, bool isHidden = false, string? customName = null);

        /// <summary>
        /// Creates a new tenant-specific building element.
        /// </summary>
        Task<BuildingElement> CreateTenantBuildingElementAsync(string name, bool needsService, int zOrder);

        /// <summary>
        /// Creates a new tenant-specific common element.
        /// </summary>
        Task<CommonElement> CreateTenantCommonElementAsync(string name, bool needsService, int zOrder);
    }
}
