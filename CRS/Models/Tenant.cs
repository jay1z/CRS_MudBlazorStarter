using System;
using System.ComponentModel.DataAnnotations;

namespace CRS.Models {
    // SaaS Refactor: Tenant entity for multi-tenant support
    public class Tenant {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Subdomain { get; set; }
        public bool IsActive { get; set; } = true;
        public string? BrandingJson { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // New: time-ordered public identifier (UUIDv7) - use runtime-provided GUID v7
        public Guid PublicId { get; set; } = Guid.CreateVersion7();

        // Provisioning lifecycle
        public TenantProvisioningStatus ProvisioningStatus { get; set; } = TenantProvisioningStatus.Pending; // initial
        public DateTime? ProvisionedAt { get; set; }
        public string? ProvisioningError { get; set; }
    }

    public enum TenantProvisioningStatus {
        Pending = 0,
        Provisioning = 1,
        Active = 2,
        Failed = 3,
        Disabled = 4
    }
}
