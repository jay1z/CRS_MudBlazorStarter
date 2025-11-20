using System.ComponentModel.DataAnnotations;
using CRS.Data;

namespace CRS.Models.Security
{
    // Custom role entity supporting scope (platform/tenant/external)
    public class Role
    {
        [Key]
        public Guid Id { get; set; } = Guid.CreateVersion7();

        [Required]
        [MaxLength(128)]
        public string Name { get; set; } = default!;

        [Required]
        public RoleScope Scope { get; set; }

        // Navigation
        public ICollection<UserRoleAssignment> Assignments { get; set; } = new List<UserRoleAssignment>();
    }
}
