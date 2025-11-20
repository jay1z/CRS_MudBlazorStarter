using Microsoft.AspNetCore.Authorization;

namespace CRS.Authorization
{
    // Checks that a user has any of the specified roles within the current tenant
    public static class Policies
    {
        public const string RequirePlatformAdmin = "RequirePlatformAdmin";
        public const string RequireTenantOwner = "RequireTenantOwner";
        public const string RequireTenantStaff = "RequireTenantStaff";
        public const string RequireTenantViewer = "RequireTenantViewer";
        public const string RequireHOAUser = "RequireHOAUser";
    }
}
