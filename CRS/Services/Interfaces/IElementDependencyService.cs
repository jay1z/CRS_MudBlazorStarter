using Horizon.Models;

namespace Horizon.Services.Interfaces {
    /// <summary>
    /// Service interface for managing element dependencies and validating relationships.
    /// </summary>
    public interface IElementDependencyService {
        /// <summary>
        /// Gets all active element dependencies.
        /// </summary>
        Task<List<ElementDependency>> GetAllDependenciesAsync();

        /// <summary>
        /// Gets dependencies where the specified element is the dependent (child) element.
        /// These are the elements that this element requires.
        /// </summary>
        Task<List<ElementDependency>> GetDependenciesForElementAsync(Guid elementId, ElementType elementType);

        /// <summary>
        /// Gets dependencies where the specified element is the required (parent) element.
        /// These are the elements that depend on this element.
        /// </summary>
        Task<List<ElementDependency>> GetDependentsOfElementAsync(Guid elementId, ElementType elementType);

        /// <summary>
        /// Creates a new element dependency relationship.
        /// </summary>
        /// <param name="dependentElementId">The element that has a dependency (e.g., Basin)</param>
        /// <param name="dependentElementType">Type of the dependent element</param>
        /// <param name="requiredElementId">The element that is required (e.g., Road)</param>
        /// <param name="requiredElementType">Type of the required element</param>
        /// <param name="description">Optional description of the dependency</param>
        /// <param name="isHardDependency">If true, the dependent cannot be added without the required element</param>
        Task<ElementDependency> CreateDependencyAsync(
            Guid dependentElementId,
            ElementType dependentElementType,
            Guid requiredElementId,
            ElementType requiredElementType,
            string? description = null,
            bool isHardDependency = false);

        /// <summary>
        /// Updates an existing element dependency.
        /// </summary>
        Task<ElementDependency> UpdateDependencyAsync(Guid dependencyId, string? description, bool isHardDependency, bool isActive);

        /// <summary>
        /// Deletes (soft delete) an element dependency.
        /// </summary>
        Task DeleteDependencyAsync(Guid dependencyId);

        /// <summary>
        /// Validates if an element can be added to a reserve study by checking if all required dependencies are present.
        /// </summary>
        /// <param name="elementId">The element to validate</param>
        /// <param name="elementType">Type of the element</param>
        /// <param name="existingBuildingElementIds">IDs of building elements already in the study</param>
        /// <param name="existingCommonElementIds">IDs of common elements already in the study</param>
        /// <returns>Validation result with any missing dependencies</returns>
        Task<ElementDependencyValidationResult> ValidateElementDependenciesAsync(
            Guid elementId,
            ElementType elementType,
            IEnumerable<Guid> existingBuildingElementIds,
            IEnumerable<Guid> existingCommonElementIds);

        /// <summary>
        /// Validates all elements in a reserve study and returns any missing dependencies.
        /// </summary>
        /// <param name="buildingElementIds">IDs of building elements in the study</param>
        /// <param name="commonElementIds">IDs of common elements in the study</param>
        /// <returns>List of validation results for elements with missing dependencies</returns>
        Task<List<ElementDependencyValidationResult>> ValidateStudyElementsAsync(
            IEnumerable<Guid> buildingElementIds,
            IEnumerable<Guid> commonElementIds);

        /// <summary>
        /// Gets the name of an element by its ID and type.
        /// </summary>
        Task<string> GetElementNameAsync(Guid elementId, ElementType elementType);
    }
}
