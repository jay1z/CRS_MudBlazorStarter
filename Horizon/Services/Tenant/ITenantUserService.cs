using Horizon.Data;
using Horizon.Models;
using Microsoft.AspNetCore.Identity;

namespace Horizon.Services.Tenant {
    public interface ITenantUserService {
        Task<ApplicationUser?> CreateTenantUserAsync(string email, string password, string firstName, string lastName, int tenantId, string roleName);
        Task<IdentityResult> UpdateUserAsync(ApplicationUser user);
    }
}
