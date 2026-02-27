using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Horizon.Data;
using Horizon.Services.Tenant;
using System.Threading.Tasks;
using System.Threading;

namespace Horizon.Services
{
    // Custom UserStore to handle duplicate emails across tenants
    public class CustomUserStore : UserStore<ApplicationUser, IdentityRole<Guid>, ApplicationDbContext, Guid>
    {
        private readonly ITenantContext _tenantContext;

        public CustomUserStore(ApplicationDbContext context, ITenantContext tenantContext, IdentityErrorDescriber describer = null)
            : base(context, describer)
        {
            _tenantContext = tenantContext;
        }

        // Override FindByEmailAsync to find by email and current tenant if set
        public override async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            if (_tenantContext.TenantId.HasValue)
            {
                // Find user with matching email and tenant
                return await Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.TenantId == _tenantContext.TenantId.Value, cancellationToken);
            }
            else
            {
                // Fallback to first user (for platform or no tenant context)
                return await Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
            }
        }
    }
}