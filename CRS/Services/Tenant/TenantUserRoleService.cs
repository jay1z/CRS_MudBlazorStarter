using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using CRS.Services.Tenant;

namespace CRS.Services.Tenant {
    public interface ITenantUserRoleService {
        string? EffectivePrimaryRole { get; }
        Task InitializeAsync();
        bool IsInRole(string role);
        bool IsPlatformAdmin { get; }
        bool IsPlatformSupport { get; }
        bool IsTenantOwner { get; }
        bool IsTenantSpecialist { get; }
        bool IsTenantViewer { get; }
        bool IsHOAUser { get; }
        bool IsHOAAuditor { get; }
        IReadOnlyCollection<string> Roles { get; }
    }

    // Scoped service resolves current user's roles (already in claims) and exposes a normalized primary role for homepage selection
    public class TenantUserRoleService : ITenantUserRoleService {
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ITenantContext _tenantContext;
        private HashSet<string> _roles = new(StringComparer.OrdinalIgnoreCase);
        private bool _initialized;

        public TenantUserRoleService(AuthenticationStateProvider authStateProvider, ITenantContext tenantContext) {
            _authStateProvider = authStateProvider;
            _tenantContext = tenantContext;
        }

        public string? EffectivePrimaryRole { get; private set; }
        public bool IsPlatformAdmin => IsInRole("PlatformAdmin");
        public bool IsPlatformSupport => IsInRole("PlatformSupport");
        public bool IsTenantOwner => IsInRole("TenantOwner");
        public bool IsTenantSpecialist => IsInRole("TenantSpecialist");
        public bool IsTenantViewer => IsInRole("TenantViewer");
        public bool IsHOAUser => IsInRole("HOAUser");
        public bool IsHOAAuditor => IsInRole("HOAAuditor");
        public IReadOnlyCollection<string> Roles => _roles;

        public async Task InitializeAsync() {
            if (_initialized) return;
            var state = await _authStateProvider.GetAuthenticationStateAsync();
            var user = state.User;
            if (user.Identity?.IsAuthenticated == true) {
                _roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                EffectivePrimaryRole = DerivePrimaryRole(_roles);
            }
            _initialized = true;
        }

        public bool IsInRole(string role) => _roles.Contains(role);

        private static string? DerivePrimaryRole(HashSet<string> roles) {
            // Priority ordering for selection logic
            string[] priority = new[] {
                "PlatformAdmin","PlatformSupport","TenantOwner","TenantSpecialist","TenantViewer","HOAUser","HOAAuditor"
            };
            foreach (var p in priority) if (roles.Contains(p)) return p;
            return roles.FirstOrDefault();
        }
    }
}
