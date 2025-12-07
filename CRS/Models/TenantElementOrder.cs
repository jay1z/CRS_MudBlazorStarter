using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Services.Tenant;

namespace CRS.Models {
    /// <summary>
    /// Stores tenant-specific ordering and visibility overrides for global elements.
    /// </summary>
    public class TenantElementOrder : BaseModel, ITenantScoped {
        /// <summary>
        /// The tenant this ordering applies to.
        /// </summary>
        [Required]
        public int TenantId { get; set; }

        /// <summary>
        /// The type of element (Building or Common).
        /// </summary>
        [Required]
        public ElementType ElementType { get; set; }

        /// <summary>
        /// The ID of the element being customized.
        /// </summary>
        [Required]
        public Guid ElementId { get; set; }

        /// <summary>
        /// Tenant-specific sort order. Lower values appear first.
        /// </summary>
        public int ZOrder { get; set; }

        /// <summary>
        /// If true, this element is hidden for this tenant.
        /// </summary>
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// Optional custom name override for this tenant.
        /// </summary>
        public string? CustomName { get; set; }

        /// <summary>
        /// Navigation property to the Tenant.
        /// </summary>
        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
    }

    /// <summary>
    /// Enum to identify the type of element.
    /// </summary>
    public enum ElementType {
        Building = 0,
        Common = 1
    }
}
