using Microsoft.AspNetCore.Components.Server.Circuits;
using Horizon.Services.Tenant;

namespace Horizon.Services.Tenant {
    // Ensures initial circuit gets tenant info resolved by HTTP middleware before prerender vs interactive mismatch.
    public class TenantCircuitHandler : CircuitHandler {
        private readonly ITenantContext _tenantContext;
        private readonly IHttpContextAccessor _httpAccessor;
        public TenantCircuitHandler(ITenantContext tenantContext, IHttpContextAccessor httpAccessor) {
            _tenantContext = tenantContext; _httpAccessor = httpAccessor; }

        public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken) {
            var ctx = _httpAccessor.HttpContext;
            if (ctx == null) return Task.CompletedTask;
            // Only set if not already set (avoid overwriting if something else populated earlier)
            if (_tenantContext.TenantId == null && ctx.Items.TryGetValue("tenant.id", out var idObj) && idObj is int id) {
                _tenantContext.TenantId = id;
                _tenantContext.TenantName = ctx.Items.TryGetValue("tenant.name", out var nameObj) ? nameObj as string : null;
                _tenantContext.Subdomain = ctx.Items.TryGetValue("tenant.sub", out var subObj) ? subObj as string : null;
                var platform = ctx.Items.TryGetValue("tenant.platform", out var platObj) && platObj is bool b ? b : false;
                _tenantContext.IsPlatformHost = platform;
            }
            return Task.CompletedTask;
        }
    }
}
