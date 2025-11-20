using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CRS.Data;
using CRS.Models;
using CRS.Services.Tenant;

namespace CRS.Models.Security
{
    // Many-to-many user role assignments with optional tenant scope
    public class UserRoleAssignment : ITenantScoped
    {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid RoleId { get; set; }

        // If null => platform-level role
        public int TenantId { get; set; } // 0 means platform/global

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [ForeignKey(nameof(RoleId))]
        public Role? Role { get; set; }

        [ForeignKey(nameof(TenantId))]
        public Tenant? Tenant { get; set; }
    }
}
