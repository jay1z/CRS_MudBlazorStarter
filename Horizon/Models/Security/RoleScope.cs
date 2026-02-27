using System.ComponentModel.DataAnnotations;

namespace Horizon.Models.Security
{
    // Defines the scope for a role
    public enum RoleScope
    {
        Platform = 0,
        Tenant = 1,
        External = 2
    }
}
