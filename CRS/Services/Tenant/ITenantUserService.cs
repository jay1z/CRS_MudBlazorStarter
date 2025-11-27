using CRS.Data;
using CRS.Models;
using Microsoft.AspNetCore.Identity;

namespace CRS.Services.Tenant {
    public interface ITenantUserService {
        Task<ApplicationUser?> CreateTenantUserAsync(string email, string password, string firstName, string lastName, int tenantId, string roleName);
    }
}
