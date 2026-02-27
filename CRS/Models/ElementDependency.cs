using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Horizon.Models {
    /// <summary>
    /// Defines a dependency relationship between elements.
    /// For example: A Basin (dependent) requires a Road (required).
    /// When a dependent element is added, the system will flag if the required element is missing.
    /// </summary>
    public class ElementDependency : BaseModel {
        /// <summary>
        /// The type of element that has a dependency (the child/dependent element).
        /// </summary>
        [Required]
        public ElementType DependentElementType { get; set; }

        /// <summary>
        /// The ID of the element that has a dependency (e.g., Basin).
        /// </summary>
        [Required]
        public Guid DependentElementId { get; set; }

        /// <summary>
        /// The type of element that is required (the parent/prerequisite element).
        /// </summary>
        [Required]
        public ElementType RequiredElementType { get; set; }

        /// <summary>
        /// The ID of the element that is required (e.g., Road).
        /// </summary>
        [Required]
        public Guid RequiredElementId { get; set; }

        /// <summary>
        /// Optional description of why this dependency exists.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// If true, this is a hard dependency - the dependent element cannot be added without the required element.
        /// If false, this is a soft dependency - a warning will be shown but the element can still be added.
        /// </summary>
        public bool IsHardDependency { get; set; } = false;

        /// <summary>
        /// Whether this dependency rule is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Result of a dependency validation check.
    /// </summary>
    public class ElementDependencyValidationResult {
        /// <summary>
        /// The element being validated.
        /// </summary>
        public required Guid ElementId { get; set; }
        
        /// <summary>
        /// The element type being validated.
        /// </summary>
        public required ElementType ElementType { get; set; }
        
        /// <summary>
        /// Name of the element being validated.
        /// </summary>
        public required string ElementName { get; set; }

        /// <summary>
        /// List of missing required elements.
        /// </summary>
        public List<MissingDependency> MissingDependencies { get; set; } = new();

        /// <summary>
        /// True if there are any missing hard dependencies.
        /// </summary>
        public bool HasMissingHardDependencies => MissingDependencies.Any(d => d.IsHardDependency);

        /// <summary>
        /// True if there are any missing soft dependencies (warnings).
        /// </summary>
        public bool HasWarnings => MissingDependencies.Any(d => !d.IsHardDependency);

        /// <summary>
        /// True if validation passed (no missing hard dependencies).
        /// </summary>
        public bool IsValid => !HasMissingHardDependencies;
    }

    /// <summary>
    /// Represents a missing dependency.
    /// </summary>
    public class MissingDependency {
        /// <summary>
        /// ID of the required element that is missing.
        /// </summary>
        public required Guid RequiredElementId { get; set; }

        /// <summary>
        /// Type of the required element.
        /// </summary>
        public required ElementType RequiredElementType { get; set; }

        /// <summary>
        /// Name of the required element.
        /// </summary>
        public required string RequiredElementName { get; set; }

        /// <summary>
        /// Whether this is a hard dependency.
        /// </summary>
        public bool IsHardDependency { get; set; }

        /// <summary>
        /// Description of the dependency relationship.
        /// </summary>
        public string? Description { get; set; }
    }
}
