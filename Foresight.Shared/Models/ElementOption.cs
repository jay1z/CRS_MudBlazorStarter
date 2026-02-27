using System.ComponentModel.DataAnnotations;

namespace Horizon.Models {
    /// <summary>
    /// Phase 2: Consolidated element option model replacing ElementMeasurementOptions, 
    /// ElementRemainingLifeOptions, and ElementUsefulLifeOptions.
    /// Uses discriminator pattern for efficient storage.
    /// </summary>
    public class ElementOption : BaseModel {
        /// <summary>
        /// Type of element option (Measurement, RemainingLife, UsefulLife)
        /// </summary>
        [Required]
        public ElementOptionType OptionType { get; set; }

        /// <summary>
        /// Display text shown to users (e.g., "Square Feet", "1-5 Years")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string DisplayText { get; set; } = string.Empty;

        /// <summary>
        /// Unit abbreviation (e.g., "sq. ft.", "1-5")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Minimum value for range-based options
        /// </summary>
        public int? MinValue { get; set; }

        /// <summary>
        /// Optional: Maximum value for range-based options
        /// </summary>
        public int? MaxValue { get; set; }

        /// <summary>
        /// Display order
        /// </summary>
        public int ZOrder { get; set; }

        /// <summary>
        /// Whether this option is active and available for selection
        /// </summary>
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Discriminator enum for element option types
    /// </summary>
    public enum ElementOptionType {
        /// <summary>
        /// Measurement unit options (sq ft, linear ft, each)
        /// </summary>
        Measurement = 0,

        /// <summary>
        /// Remaining life range options (1-5 years, 6-10 years, etc.)
        /// </summary>
        RemainingLife = 1,

        /// <summary>
        /// Useful life range options (1-5 years, 6-10 years, etc.)
        /// </summary>
        UsefulLife = 2
    }
}
