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
    }
}
