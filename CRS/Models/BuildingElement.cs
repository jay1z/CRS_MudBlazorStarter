using System.ComponentModel.DataAnnotations;
using CRS.Services.Tenant;

namespace CRS.Models {
    public class BuildingElement : BaseModel, ITenantScoped {
        public List<ReserveStudyBuildingElement>? ReserveStudyBuildingElements { get; set; }

        public string Name { get; set; }
        public bool NeedsService { get; set; }
        [DataType(DataType.Date)] public DateTime? LastServiced { get; set; }
        public bool IsActive { get; set; }
        public int ZOrder { get; set; }

        /// <summary>
        /// TenantId for tenant-specific elements. Null = global element available to all tenants.
        /// </summary>
        public int? TenantId { get; set; }

        // Explicit interface implementation for ITenantScoped (which expects non-nullable int)
        int ITenantScoped.TenantId {
            get => TenantId ?? 0;
            set => TenantId = value;
        }
    }
}
