using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Core.ReserveCalculator.Enums;
using CRS.Services.Tenant;

namespace CRS.Models.ReserveStudyCalculator;

/// <summary>
/// A reserve component within a calculation scenario.
/// Represents an asset or system that requires future funding.
/// </summary>
[Table("ReserveScenarioComponents")]
public class ReserveScenarioComponent : ITenantScoped
{
    [Key]
    public int Id { get; set; }

    public int TenantId { get; set; }

    /// <summary>
    /// The scenario this component belongs to.
    /// </summary>
    public int ScenarioId { get; set; }

    /// <summary>
    /// Name of the component (e.g., "Roof Replacement", "Pool Resurfacing").
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category for grouping (e.g., "Building Exterior", "Amenities").
    /// </summary>
    [MaxLength(100)]
    public string Category { get; set; } = "General";

    /// <summary>
    /// The calculation method for expenditure scheduling.
    /// </summary>
    public ComponentMethod Method { get; set; } = ComponentMethod.Replacement;

    /// <summary>
    /// Current estimated cost in today's dollars.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentCost { get; set; }

    /// <summary>
    /// Component-specific inflation rate override (null = use study default).
    /// </summary>
    [Column(TypeName = "decimal(8,6)")]
    public decimal? InflationRateOverride { get; set; }

    #region Replacement Method Fields

    /// <summary>
    /// Year this component was last serviced/replaced.
    /// </summary>
    public int? LastServiceYear { get; set; }

    /// <summary>
    /// Expected useful life in years.
    /// </summary>
    public int? UsefulLifeYears { get; set; }

    /// <summary>
    /// Override for remaining life calculation (if set, used instead of LastServiceYear).
    /// </summary>
    public int? RemainingLifeOverrideYears { get; set; }

    #endregion

    #region PRN Method Fields

    /// <summary>
    /// Cycle interval in years (1 = annual).
    /// </summary>
    public int? CycleYears { get; set; }

    /// <summary>
    /// Override for periodic cost (if different from CurrentCost).
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? AnnualCostOverride { get; set; }

    #endregion

    #region Optional Metadata

    /// <summary>
    /// Display order within the scenario.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Notes or description for this component.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Reference to an existing building element (optional link).
    /// Changed to Guid? to match BuildingElement.Id type.
    /// </summary>
    public Guid? LinkedBuildingElementId { get; set; }
    
    /// <summary>
    /// Reference to an existing common element (optional link).
    /// Changed to Guid? to match CommonElement.Id type.
    /// </summary>
    public Guid? LinkedCommonElementId { get; set; }

    /// <summary>
    /// When this component was created.
    /// </summary>
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this component was last modified.
    /// </summary>
    public DateTime DateModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Soft delete timestamp.
    /// </summary>
    public DateTime? DateDeleted { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// The parent scenario.
    /// </summary>
    [ForeignKey(nameof(ScenarioId))]
    public virtual ReserveStudyScenario? Scenario { get; set; }

    /// <summary>
    /// Optional link to building element.
    /// </summary>
    [ForeignKey(nameof(LinkedBuildingElementId))]
    public virtual BuildingElement? LinkedBuildingElement { get; set; }

    /// <summary>
    /// Optional link to common element.
    /// </summary>
    [ForeignKey(nameof(LinkedCommonElementId))]
    public virtual CommonElement? LinkedCommonElement { get; set; }

    #endregion
}
