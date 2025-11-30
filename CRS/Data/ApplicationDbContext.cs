using System.Reflection.Emit;
using System.Security.Claims;

using CRS.Models;
using CRS.Models.Security; // add
using CRS.Models.Workflow; // add
using CRS.Services.Tenant;
using CRS.Models.Billing; // added for StripeEventLog

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CRS.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> {
        private const string DefaultSchema = "crs";
        private readonly IHttpContextAccessor _httpContextAccessor;
        // SaaS Refactor: Inject tenant context
        private readonly ITenantContext _tenantContext;
        private readonly string? _explicitConnection; // multi-tenant override

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor, ITenantContext tenantContext, string? explicitConnection = null)
            : base(options) {
            _httpContextAccessor = httpContextAccessor;
            _tenantContext = tenantContext;
            _explicitConnection = explicitConnection;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);

            // If explicit connection provided (per-tenant DB), ensure it's applied when options not yet configured
            if (!string.IsNullOrEmpty(_explicitConnection) && !optionsBuilder.IsConfigured) {
                optionsBuilder.UseSqlServer(_explicitConnection, sql => sql.EnableRetryOnFailure());
            }

            // Register BOTH sync and async seeding delegates with correct signatures
            optionsBuilder
                .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
                .UseSeeding((context, _) => { SeedData(context); })
                .UseAsyncSeeding((context, _, ct) => { SeedData(context); return Task.CompletedTask; });
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.HasDefaultSchema(DefaultSchema);

            ConfigureEntities(builder);

            // Indexes to improve tenant queries
            builder.Entity<Community>().HasIndex(e => e.TenantId);
            builder.Entity<Contact>().HasIndex(e => e.TenantId);
            builder.Entity<PropertyManager>().HasIndex(e => e.TenantId);
            builder.Entity<ReserveStudy>().HasIndex(e => e.TenantId);
            builder.Entity<FinancialInfo>().HasIndex(e => e.TenantId);
            builder.Entity<Proposal>().HasIndex(e => e.TenantId);
            builder.Entity<StudyRequest>().HasIndex(e => e.TenantId); // add
            builder.Entity<StudyRequest>().HasIndex(e => new { e.TenantId, e.CurrentStatus }); // add
            builder.Entity<StudyStatusHistory>().HasIndex(e => e.TenantId); // add
            builder.Entity<StudyStatusHistory>().HasIndex(e => new { e.TenantId, e.RequestId, e.ChangedAt }); // add
            builder.Entity<StudyStatusHistory>().HasIndex(e => new { e.TenantId, e.ToStatus, e.ChangedAt }); // add

            // Ensure an index on AspNetUsers.TenantId for tenant-scoped user lookups
            builder.Entity<ApplicationUser>().HasIndex(u => u.TenantId);

            // SaaS Refactor: Tenant-aware query filters on principals
            // Apply a global tenant query filter to all entities implementing ITenantScoped
            ApplyTenantQueryFilters(builder);

            // Matching filters on dependents to avoid EF warnings with required relationships
            builder.Entity<ReserveStudyBuildingElement>().HasQueryFilter(e => _tenantContext.TenantId == null || (e.ReserveStudy != null && e.ReserveStudy.TenantId == _tenantContext.TenantId));
            builder.Entity<ReserveStudyCommonElement>().HasQueryFilter(e => _tenantContext.TenantId == null || (e.ReserveStudy != null && e.ReserveStudy.TenantId == _tenantContext.TenantId));
            builder.Entity<ReserveStudyAdditionalElement>().HasQueryFilter(e => _tenantContext.TenantId == null || (e.ReserveStudy != null && e.ReserveStudy.TenantId == _tenantContext.TenantId));
            builder.Entity<ContactXContactGroup>().HasQueryFilter(e => _tenantContext.TenantId == null || (e.Contact != null && e.Contact.TenantId == _tenantContext.TenantId));

            // Fix1: Configure1:1 relationships using dependent FKs
            builder.Entity<ReserveStudy>()
                .HasOne(r => r.Proposal)
                .WithOne(p => p.ReserveStudy)
                .HasForeignKey<Proposal>(p => p.ReserveStudyId);

            builder.Entity<ReserveStudy>()
                .HasOne(r => r.FinancialInfo)
                .WithOne(f => f.ReserveStudy)
                .HasForeignKey<FinancialInfo>(f => f.ReserveStudyId);

            // Shared PK1:1 ReserveStudy <-> StudyRequest
            builder.Entity<StudyRequest>()
                .HasOne(r => r.ReserveStudy)
                .WithOne(s => s.StudyRequest)
                .HasForeignKey<StudyRequest>(r => r.Id);

            // Security entities
            builder.Entity<Role>(entity => {
                entity.ToTable("Roles");
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).IsRequired().HasMaxLength(128);
                entity.HasIndex(r => r.Name).IsUnique();
                entity.Property(r => r.Scope).IsRequired();
            });

            builder.Entity<UserRoleAssignment>(entity => {
                entity.ToTable("UserRoleAssignments");
                entity.HasKey(a => a.Id);
                entity.HasIndex(a => a.UserId);
                entity.HasIndex(a => a.TenantId);
                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Role)
                      .WithMany(r => r.Assignments)
                      .HasForeignKey(a => a.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(a => a.Tenant)
                      .WithMany()
                      .HasForeignKey(a => a.TenantId)
                      .OnDelete(DeleteBehavior.NoAction)
                      .IsRequired(false);
            });
        }

        // Apply tenant query filters to all ITenantScoped entities
        private void ApplyTenantQueryFilters(ModelBuilder builder) {
            var tenantScopedTypes = builder.Model.GetEntityTypes()
                .Where(t => typeof(ITenantScoped).IsAssignableFrom(t.ClrType))
                .Select(t => t.ClrType)
                .ToList();

            foreach (var type in tenantScopedTypes) {
                // Skip Tenants, Settings, AccessToken (global tables) if they implement the interface by accident
                if (type == typeof(Tenant) || type == typeof(Settings) || type == typeof(AccessToken) || type == typeof(UserRoleAssignment)) continue;

                var method = typeof(ApplicationDbContext).GetMethod(nameof(ApplyTenantFilterGeneric), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(type);
                method?.Invoke(this, new object[] { builder });
            }
        }

        private void ApplyTenantFilterGeneric<TEntity>(ModelBuilder builder) where TEntity : class, ITenantScoped {
            builder.Entity<TEntity>().HasQueryFilter(e => _tenantContext.TenantId == null || e.TenantId == _tenantContext.TenantId);
        }

        private void ConfigureEntities(ModelBuilder builder) {
            builder.Entity<AccessToken>(entity => {
                entity.HasIndex(at => at.Token).IsUnique();
                entity.Property(at => at.Expiration).IsRequired();
            });

            // SaaS Refactor: Tenant entity
            builder.Entity<Tenant>(entity => {
                entity.ToTable("Tenants");
                entity.HasIndex(t => t.Subdomain).IsUnique();
                entity.Property(t => t.Name).IsRequired();
                entity.Property(t => t.Subdomain).IsRequired();

                // PublicId is a time-ordered UUIDv7 used for public/opaque identifiers
                // Map as uniqueidentifier with a DB default to NEWSEQUENTIALID() to reduce index fragmentation.
                entity.Property(t => t.PublicId)
                    .HasColumnType("uniqueidentifier")
                    .HasDefaultValueSql("NEWSEQUENTIALID()");
                entity.HasIndex(t => t.PublicId).IsUnique();
            });

            builder.Entity<ReserveStudyBuildingElement>()
                .HasKey(rsbe => new { rsbe.ReserveStudyId, rsbe.BuildingElementId });

            builder.Entity<ReserveStudyBuildingElement>()
                .HasOne(rsbe => rsbe.ReserveStudy)
                .WithMany(rs => rs.ReserveStudyBuildingElements)
                .HasForeignKey(rsbe => rsbe.ReserveStudyId);

            builder.Entity<ReserveStudyBuildingElement>()
                .HasOne(rsbe => rsbe.BuildingElement)
                .WithMany(be => be.ReserveStudyBuildingElements)
                .HasForeignKey(rsbe => rsbe.BuildingElementId);

            builder.Entity<ReserveStudyCommonElement>()
                .HasKey(rsbe => new { rsbe.ReserveStudyId, rsbe.CommonElementId });

            builder.Entity<ReserveStudyCommonElement>()
                .HasOne(rsbe => rsbe.ReserveStudy)
                .WithMany(rs => rs.ReserveStudyCommonElements)
                .HasForeignKey(rsbe => rsbe.ReserveStudyId);

            builder.Entity<ReserveStudyCommonElement>()
                .HasOne(rsbe => rsbe.CommonElement)
                .WithMany(ce => ce.ReserveStudyCommonElements)
                .HasForeignKey(rsbe => rsbe.CommonElementId);

            // Configure decimal precision for ReserveStudy
            builder.Entity<ReserveStudy>(entity => {
                entity.Property(rs => rs.CurrentReserveFunds).HasPrecision(18, 2);
                entity.Property(rs => rs.MonthlyReserveContribution).HasPrecision(18, 2);
                entity.Property(rs => rs.AnnualInflationRate).HasPrecision(18, 2);
                entity.Property(rs => rs.AnnualInterestRate).HasPrecision(18, 2);
            });

            // Configure decimal precision for ReserveStudyCommonElement
            builder.Entity<ReserveStudyCommonElement>(entity => {
                entity.Property(rsce => rsce.Quantity).HasPrecision(18, 2);
                entity.Property(rsce => rsce.ReplacementCost).HasPrecision(18, 2);
            });

            // TenantHomepage entity configuration
            builder.Entity<TenantHomepage>(entity => {
                entity.ToTable("TenantHomepages");
                // Ensure one homepage row per tenant
                entity.HasIndex(h => h.TenantId).IsUnique();
                entity.HasIndex(h => new { h.TenantId, h.IsPublished });
                entity.Property(h => h.DraftJson).HasColumnType("nvarchar(max)");
                entity.Property(h => h.PublishedJson).HasColumnType("nvarchar(max)");
                entity.Property(h => h.DraftHtml).HasColumnType("nvarchar(max)");
                entity.Property(h => h.PublishedHtml).HasColumnType("nvarchar(max)");
            });

            // CustomerAccount
            builder.Entity<CRS.Models.CustomerAccount>(entity => {
                entity.ToTable("CustomerAccounts");
                entity.HasIndex(e => e.TenantId);
                entity.Property(e => e.Name).HasMaxLength(256);
                entity.Property(e => e.Email).HasMaxLength(256);
            });

            // SupportTicket
            builder.Entity<CRS.Models.SupportTicket>(entity => {
                entity.ToTable("SupportTickets");
                entity.HasIndex(e => e.TenantId);
                entity.Property(e => e.Title).HasMaxLength(256);
                entity.Property(e => e.Status).HasMaxLength(64);
            });
        }

        // Apply owned type configurations
        private void ApplyOwnedConfigurations(ModelBuilder builder) {
            // Omit Id properties from tenant-scoped owned types (to use owner PK)
            var ownedTypes = builder.Model.GetEntityTypes()
                .SelectMany(t => t.GetForeignKeys())
                .Where(fk => fk.PrincipalEntityType.ClrType == typeof(Tenant))
                .Select(fk => fk.DependentToPrincipal?.DeclaringEntityType)
                .Where(et => et != null)
                .Select(et => et!.ClrType)
                .ToList();

            foreach (var type in ownedTypes) {
                var method = typeof(ApplicationDbContext).GetMethod(nameof(ConfigOwnsMany), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.MakeGenericMethod(type);
                method?.Invoke(this, new object[] { builder });
            }
        }

        private void ConfigOwnsMany<TEntity>(ModelBuilder builder) where TEntity : class {
            builder.Entity<TEntity>()
                .Property("Id")
                .HasColumnName("OwnerId")
                .IsRequired()
                .ValueGeneratedNever();
        }

        public override int SaveChanges() {
            AddAuditLogs();
            AddTenantIds();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            AddAuditLogs();
            AddTenantIds();
            return base.SaveChangesAsync(cancellationToken);
        }

        private Guid? GetCurrentUserId() {
            var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return userIdString != null && Guid.TryParse(userIdString, out Guid userId) ? userId : null;
        }

        private void AddAuditLogs() {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in entries) {
                var entityType = entry.Entity.GetType();
                if (entityType == typeof(AuditLog)) continue; // Skip audit logs themselves

                var primaryKey = entry.Properties
                    .First(p => p.Metadata.IsPrimaryKey())
                    .CurrentValue?.ToString() ?? "Unknown";

                foreach (var property in entry.Properties.Where(p => p.IsModified)) {
                    AuditLogs.Add(new AuditLog {
                        ApplicationUserId = GetCurrentUserId(),
                        Action = "Update",
                        TableName = entityType.Name,
                        RecordId = primaryKey,
                        ColumnName = property.Metadata.Name,
                        OldValue = property.OriginalValue?.ToString() ?? "",
                        NewValue = property.CurrentValue?.ToString() ?? ""
                    });
                }
            }
        }

        // SaaS Refactor: Ensure TenantId is set on inserted entities
        private void AddTenantIds() {
            var tenantId = _tenantContext.TenantId; // no default, new site may have no tenant context
            foreach (var entry in ChangeTracker.Entries().Where(e => e.State == EntityState.Added)) {
                if (entry.Entity is ITenantScoped scoped) {
                    if (scoped.TenantId == 0 && tenantId.HasValue) scoped.TenantId = tenantId.Value;
                }
                if (entry.Entity is ApplicationUser user) {
                    if (user.TenantId == 0 && tenantId.HasValue) user.TenantId = tenantId.Value;
                }
            }
        }

        #region DbSet Properties
        public DbSet<AccessToken> AccessTokens { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<BuildingElement> BuildingElements { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<CommonElement> CommonElements { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactGroup> ContactGroups { get; set; }
        public DbSet<ContactXContactGroup> ContactXContactGroups { get; set; }
        public DbSet<ElementMeasurementOptions> ElementMeasurementOptions { get; set; }
        public DbSet<ElementRemainingLifeOptions> ElementRemainingLifeOptions { get; set; }
        public DbSet<ElementUsefulLifeOptions> ElementUsefulLifeOptions { get; set; }
        public DbSet<FinancialInfo> FinancialInfos { get; set; }
        public DbSet<KanbanTask> KanbanTasks { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<PropertyManager> PropertyManagers { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<ReserveStudy> ReserveStudies { get; set; }
        public DbSet<ReserveStudyAdditionalElement> ReserveStudyAdditionalElements { get; set; }
        public DbSet<ReserveStudyBuildingElement> ReserveStudyBuildingElements { get; set; }
        public DbSet<ReserveStudyCommonElement> ReserveStudyCommonElements { get; set; }
        public DbSet<ServiceContact> ServiceContacts { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<Tenant> Tenants { get; set; } // SaaS Refactor
        public DbSet<TenantHomepage> TenantHomepages { get; set; } // SaaS Refactor: Tenant homepage
        public DbSet<StudyRequest> StudyRequests { get; set; } // add
        public DbSet<StudyStatusHistory> StudyStatusHistories { get; set; } // add
        public DbSet<CRS.Models.Message> Messages { get; set; }
        // New security sets
        public DbSet<Role> Roles2 { get; set; }
        public DbSet<UserRoleAssignment> UserRoleAssignments { get; set; }
        // Billing log
        public DbSet<StripeEventLog> StripeEventLogs { get; set; }

        // New tenant-scoped entities
        public DbSet<CRS.Models.CustomerAccount> CustomerAccounts { get; set; }
        public DbSet<CRS.Models.SupportTicket> SupportTickets { get; set; }
        
        // System-wide settings
        public DbSet<CRS.Models.SystemSettings> SystemSettings { get; set; }
        
        // Demo environment
        public DbSet<CRS.Models.Demo.DemoSession> DemoSessions { get; set; }
        #endregion

        #region Seed Data
        private void SeedData(DbContext context) {
            GenerateBuildingElements(context);
            context.SaveChanges();
            GenerateCommonElements(context);
            context.SaveChanges();
            GenerateElementMeasurementOptions(context);
            context.SaveChanges();
            GenerateElementRemainingLifeOptions(context);
            context.SaveChanges();
            GenerateElementUsefulLifeOptions(context);
            context.SaveChanges();

            // Removed default tenant seeding for new multi-tenant site; tenants are created via signup
        }

        private static void GenerateBuildingElements(DbContext context) {
            IEnumerable<BuildingElement> buildingElements =
            [
                new BuildingElement {  Name = "Pitched Roof", IsActive = true, NeedsService = true,ZOrder=0 },
                new BuildingElement {  Name = "Flat Roof", IsActive = true, NeedsService = false , ZOrder = 1},
                new BuildingElement {  Name = "Siding", IsActive = true, NeedsService = true , ZOrder = 2},
                new BuildingElement {  Name = "Gutters/Downspouts", IsActive = true, NeedsService = false , ZOrder = 3},
                new BuildingElement {  Name = "Attached Lighting", IsActive = true, NeedsService = false , ZOrder = 4},
                new BuildingElement {  Name = "Shutters", IsActive = true, NeedsService = false , ZOrder = 5},
                new BuildingElement {  Name = "Decks", IsActive = true, NeedsService = false , ZOrder = 6},
                new BuildingElement {  Name = "Flooring", IsActive = true, NeedsService = false , ZOrder = 7},
                new BuildingElement {  Name = "Lighting", IsActive = true, NeedsService = false , ZOrder = 8},
                new BuildingElement {  Name = "Intercom", IsActive = true, NeedsService = false , ZOrder = 9},
                new BuildingElement {  Name = "Security System", IsActive = true, NeedsService = false, ZOrder = 10 },
                new BuildingElement {  Name = "Elevator(s)", IsActive = true, NeedsService = true, ZOrder = 11 },
                new BuildingElement {  Name = "AC Unit(s)", IsActive = true, NeedsService = false, ZOrder = 12 },
                new BuildingElement {  Name = "Heating Unit(s)", IsActive = true, NeedsService = false, ZOrder = 13},
                new BuildingElement {  Name = "Water Unit(s)", IsActive = true, NeedsService = false, ZOrder = 14 },
                new BuildingElement {  Name = "Kitchen", IsActive = true, NeedsService = false, ZOrder = 15 },
                new BuildingElement {  Name = "Bathroom(s)", IsActive = true, NeedsService = false, ZOrder = 16 },
                new BuildingElement {  Name = "Doors", IsActive = true, NeedsService = false, ZOrder = 17 },
                new BuildingElement {  Name = "Windows", IsActive = true, NeedsService = false, ZOrder = 18 },
                new BuildingElement {  Name = "Balconies", IsActive = true, NeedsService = false, ZOrder = 19 }
            ];

            foreach (BuildingElement buildingElement in buildingElements) {
                BuildingElement element = context.Set<BuildingElement>().FirstOrDefault(e => e.Name == buildingElement.Name)!;
                if (element is null) {
                    context.Set<BuildingElement>().Add(buildingElement);
                }
            }
        }

        private static void GenerateCommonElements(DbContext context) {
            IEnumerable<CommonElement> commonElements =
            [
                new CommonElement {  Name = "Clubhouse", IsActive = true, NeedsService = false, ZOrder = 0 },
                new CommonElement {  Name = "Pool", IsActive = true, NeedsService = false, ZOrder = 1},
                new CommonElement {  Name = "Playground", IsActive = true, NeedsService = false, ZOrder = 2 },
                new CommonElement {  Name = "Tennis/Ball Courts", IsActive = true, NeedsService = false, ZOrder = 3 },
                new CommonElement {  Name = "Property Fencing", IsActive = true, NeedsService = false, ZOrder = 4 },
                new CommonElement {  Name = "Pool(s)/Lake(s)", IsActive = true, NeedsService = false, ZOrder = 5 },
                new CommonElement {  Name = "Gazebos(s)/Pavilion(s)", IsActive = true, NeedsService = false, ZOrder = 6},
                new CommonElement {  Name = "Entry Signage/Structure(s)", IsActive = true, NeedsService = false , ZOrder = 7},
                new CommonElement {  Name = "Street Signage", IsActive = true, NeedsService = false , ZOrder = 8},
                new CommonElement {  Name = "Roads", IsActive = true, NeedsService = true , ZOrder = 9},
                new CommonElement {  Name = "Catch Basins", IsActive = true, NeedsService = false , ZOrder = 10},
                new CommonElement {  Name = "Parking", IsActive = true, NeedsService = true , ZOrder = 11},
                new CommonElement {  Name = "Sidewalks", IsActive = true, NeedsService = true , ZOrder = 12},
                new CommonElement {  Name = "Driveways", IsActive = true, NeedsService = true , ZOrder = 13},
                new CommonElement {  Name = "Patios", IsActive = true, NeedsService = false , ZOrder = 14},
                new CommonElement {  Name = "Porches", IsActive = true, NeedsService = false , ZOrder = 15},
                new CommonElement {  Name = "Privacy Fencing", IsActive = true, NeedsService = false , ZOrder = 16},
                new CommonElement {  Name = "Garage(s)", IsActive = true, NeedsService = false , ZOrder = 17},
                new CommonElement {  Name = "Pump Station", IsActive = true, NeedsService = true , ZOrder = 18},
                new CommonElement {  Name = "Retaining Walls", IsActive = true, NeedsService = false , ZOrder = 19},
                new CommonElement {  Name = "Fountains", IsActive = true, NeedsService = false , ZOrder = 20},
                new CommonElement {  Name = "Property Lighting", IsActive = true, NeedsService = false, ZOrder = 21 },
                new CommonElement {  Name = "Street Lighting", IsActive = true, NeedsService = false , ZOrder = 22},
                new CommonElement {  Name = "Paved Trails/Paths", IsActive = true, NeedsService = false , ZOrder = 23},
                new CommonElement {  Name = "Mail Huts/Boxes", IsActive = true, NeedsService = false , ZOrder = 24},
                new CommonElement {  Name = "Fire Hydrants", IsActive = true, NeedsService = false , ZOrder = 25},
                new CommonElement {  Name = "Sports Fields", IsActive = true, NeedsService = false , ZOrder = 26},
                new CommonElement {  Name = "Shed(s)/Storage", IsActive = true, NeedsService = false, ZOrder = 27}
            ];
            foreach (CommonElement commonElement in commonElements) {
                CommonElement element = context.Set<CommonElement>().FirstOrDefault(e => e.Name == commonElement.Name)!;
                if (element is null) {
                    context.Set<CommonElement>().Add(commonElement);
                }
            }
        }

        private static void GenerateElementMeasurementOptions(DbContext context) {
            IEnumerable<ElementMeasurementOptions> elementMeasurementOptions =
            [
                new ElementMeasurementOptions {  DisplayText = "Square Feet", Unit = "sq. ft.", ZOrder = 0 },
                new ElementMeasurementOptions {  DisplayText = "Linear Feet", Unit = "LF.", ZOrder = 1 },
                new ElementMeasurementOptions {  DisplayText = "Each", Unit = "ea.", ZOrder = 2 }
            ];
            foreach (ElementMeasurementOptions elementMeasurementOption in elementMeasurementOptions) {
                ElementMeasurementOptions option = context.Set<ElementMeasurementOptions>().FirstOrDefault(e => e.DisplayText == elementMeasurementOption.DisplayText)!;
                if (option is null) {
                    context.Set<ElementMeasurementOptions>().Add(elementMeasurementOption);
                }
            }
        }

        private static void GenerateElementRemainingLifeOptions(DbContext context) {
            IEnumerable<ElementRemainingLifeOptions> elementRemainingLifeOptions =
            [
                new ElementRemainingLifeOptions {  DisplayText = "1-5 Years", Unit = "1-5", ZOrder = 0 },
                new ElementRemainingLifeOptions {  DisplayText = "6-10 Years", Unit = "6-10", ZOrder = 1 },
                new ElementRemainingLifeOptions {  DisplayText = "11-15 Years", Unit = "11-15", ZOrder = 2 },
                new ElementRemainingLifeOptions {  DisplayText = "16-20 Years", Unit = "16-20", ZOrder = 3 },
                new ElementRemainingLifeOptions {  DisplayText = "21-25 Years", Unit = "21-25", ZOrder = 4 },
                new ElementRemainingLifeOptions {  DisplayText = "26-30 Years", Unit = "26-30", ZOrder = 5 },
                new ElementRemainingLifeOptions {  DisplayText = "31-35 Years", Unit = "31-35", ZOrder = 6 },
                new ElementRemainingLifeOptions {  DisplayText = "36-40 Years", Unit = "36-40", ZOrder = 7 },
                new ElementRemainingLifeOptions {  DisplayText = "41-45 Years", Unit = "41-45", ZOrder = 8 },
                new ElementRemainingLifeOptions {  DisplayText = "46-50 Years", Unit = "46-50", ZOrder = 9 },
                new ElementRemainingLifeOptions {  DisplayText = "51-55 Years", Unit = "51-55", ZOrder = 10 },
                new ElementRemainingLifeOptions {  DisplayText = "61-65 Years", Unit = "61-65", ZOrder = 12 },
                new ElementRemainingLifeOptions {  DisplayText = "66-70 Years", Unit = "66-70", ZOrder = 13 },
                new ElementRemainingLifeOptions {  DisplayText = "71-75 Years", Unit = "71-75", ZOrder = 14 },
                new ElementRemainingLifeOptions {  DisplayText = "76-80 Years", Unit = "76-80", ZOrder = 15 }
            ];
            foreach (ElementRemainingLifeOptions elementRemainingLifeOption in elementRemainingLifeOptions) {
                ElementRemainingLifeOptions option = context.Set<ElementRemainingLifeOptions>().FirstOrDefault(e => e.DisplayText == elementRemainingLifeOption.DisplayText)!;
                if (option is null) {
                    context.Set<ElementRemainingLifeOptions>().Add(elementRemainingLifeOption);
                }
            }
        }

        private static void GenerateElementUsefulLifeOptions(DbContext context) {
            IEnumerable<ElementUsefulLifeOptions> elementUsefulLifeOptions =
            [
                new ElementUsefulLifeOptions {  DisplayText = "1-5 Years", Unit = "1-5", ZOrder = 0 },
                new ElementUsefulLifeOptions {  DisplayText = "6-10 Years", Unit = "6-10", ZOrder = 1 },
                new ElementUsefulLifeOptions {  DisplayText = "11-15 Years", Unit = "11-15", ZOrder = 2 },
                new ElementUsefulLifeOptions {  DisplayText = "16-20 Years", Unit = "16-20", ZOrder = 3 },
                new ElementUsefulLifeOptions {  DisplayText = "21-25 Years", Unit = "21-25", ZOrder = 4 },
                new ElementUsefulLifeOptions {  DisplayText = "26-30 Years", Unit = "26-30", ZOrder = 5 },
                new ElementUsefulLifeOptions {  DisplayText = "31-35 Years", Unit = "31-35", ZOrder = 6 },
                new ElementUsefulLifeOptions {  DisplayText = "36-40 Years", Unit = "36-40", ZOrder = 7 },
                new ElementUsefulLifeOptions {  DisplayText = "41-45 Years", Unit = "41-45", ZOrder = 8 },
                new ElementUsefulLifeOptions {  DisplayText = "46-50 Years", Unit = "46-50", ZOrder = 9 },
                new ElementUsefulLifeOptions {  DisplayText = "51-55 Years", Unit = "51-55", ZOrder = 10 },
                new ElementUsefulLifeOptions {  DisplayText = "56-60 Years", Unit = "56-60", ZOrder = 11 },
                new ElementUsefulLifeOptions {  DisplayText = "61-65 Years", Unit = "61-65", ZOrder = 12 },
                new ElementUsefulLifeOptions {  DisplayText = "66-70 Years", Unit = "66-70", ZOrder = 13 },
                new ElementUsefulLifeOptions {  DisplayText = "71-75 Years", Unit = "71-75", ZOrder = 14 },
                new ElementUsefulLifeOptions {  DisplayText = "76-80 Years", Unit = "76-80", ZOrder = 15 }
            ];
            foreach (ElementUsefulLifeOptions elementUsefulLifeOption in elementUsefulLifeOptions) {
                ElementUsefulLifeOptions option = context.Set<ElementUsefulLifeOptions>().FirstOrDefault(e => e.DisplayText == elementUsefulLifeOption.DisplayText)!;
                if (option is null) {
                    context.Set<ElementUsefulLifeOptions>().Add(elementUsefulLifeOption);
                }
            }
        }
        #endregion
    }
}

