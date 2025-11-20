using Microsoft.EntityFrameworkCore;
using CRS.Data;
using CRS.Services.Tenant;

namespace CRS.MultiTenancy.Database
{
    // Resolves a connection string for the current tenant (or platform/global)
    public interface ITenantDatabaseResolver
    {
        string GetConnectionString(int? tenantId);
    }

    // Factory that creates a tenant-specific ApplicationDbContext with dynamic connection string
    public interface ITenantDbContextFactory
    {
        Task<ApplicationDbContext> CreateAsync(int? tenantId, CancellationToken ct = default);
    }

    // Handles ensuring database exists + migrations + seeding for a tenant-specific database
    public interface ITenantMigrationService
    {
        Task EnsureDatabaseAsync(int tenantId, CancellationToken ct = default);
        Task MigrateAsync(int tenantId, CancellationToken ct = default);
        Task SeedAsync(int tenantId, CancellationToken ct = default);
    }

    public class DefaultTenantDatabaseResolver : ITenantDatabaseResolver
    {
        private readonly IConfiguration _config;
        public DefaultTenantDatabaseResolver(IConfiguration config) { _config = config; }

        public string GetConnectionString(int? tenantId)
        {
            // Strategy: per-tenant DB name pattern: BaseName_T{tenantId}
            // If null or 0 => shared platform DB
            var baseConn = _config.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("DefaultConnection missing");
            if (!tenantId.HasValue || tenantId.Value == 0) return baseConn; // platform/shared

            // For SQL Server append database name override if using pattern; assume baseConn has placeholder {DBNAME}
            // If not, just return base for now (fallback)
            var dbNamePattern = _config["MultiTenancy:DatabaseNameFormat"] ?? "CRS_T{0}";
            var tenantDbName = string.Format(dbNamePattern, tenantId.Value);

            // If connection string contains Initial Catalog placeholder pattern {DBNAME}
            if (baseConn.Contains("{DBNAME}", StringComparison.Ordinal))
            {
                return baseConn.Replace("{DBNAME}", tenantDbName);
            }

            // Otherwise attempt to rewrite Initial Catalog if present
            var parts = baseConn.Split(';');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase) || parts[i].StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                {
                    parts[i] = "Initial Catalog=" + tenantDbName;
                }
            }
            return string.Join(';', parts);
        }
    }

    public class TenantDbContextFactory : ITenantDbContextFactory
    {
        private readonly ITenantDatabaseResolver _resolver;
        private readonly ITenantContext _tenantContext;
        private readonly IServiceProvider _services;

        public TenantDbContextFactory(ITenantDatabaseResolver resolver, ITenantContext tenantContext, IServiceProvider services)
        {
            _resolver = resolver; _tenantContext = tenantContext; _services = services;
        }

        public async Task<ApplicationDbContext> CreateAsync(int? tenantId, CancellationToken ct = default)
        {
            tenantId ??= _tenantContext.TenantId;
            var conn = _resolver.GetConnectionString(tenantId);
            var optsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optsBuilder.UseSqlServer(conn, sql => sql.EnableRetryOnFailure());
            // Resolve required scoped dependencies
            var http = _services.GetRequiredService<IHttpContextAccessor>();
            var ctx = _services.GetRequiredService<ITenantContext>();
            return new ApplicationDbContext(optsBuilder.Options, http, ctx);
        }
    }

    public class TenantMigrationService : ITenantMigrationService
    {
        private readonly ITenantDbContextFactory _factory;
        private readonly ILogger<TenantMigrationService> _logger;

        public TenantMigrationService(ITenantDbContextFactory factory, ILogger<TenantMigrationService> logger)
        { _factory = factory; _logger = logger; }

        public async Task EnsureDatabaseAsync(int tenantId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateAsync(tenantId, ct);
            _logger.LogInformation("Ensuring database for tenant {TenantId}", tenantId);
            await db.Database.EnsureCreatedAsync(ct);
        }

        public async Task MigrateAsync(int tenantId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateAsync(tenantId, ct);
            _logger.LogInformation("Migrating database for tenant {TenantId}", tenantId);
            await db.Database.MigrateAsync(ct);
        }

        public async Task SeedAsync(int tenantId, CancellationToken ct = default)
        {
            await using var db = await _factory.CreateAsync(tenantId, ct);
            _logger.LogInformation("Seeding database for tenant {TenantId}", tenantId);
            // Optionally seed tenant-specific baseline data (elements etc.)
            // For now rely on shared seeding methods executed in context constructor
            await Task.CompletedTask;
        }
    }
}
