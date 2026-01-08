using System.Reflection.Emit;
using System.Security.Claims;

using CRS.Models;
using CRS.Models.Security; // add
using CRS.Models.Workflow; // add
using CRS.Models.ReserveStudyCalculator; // Reserve Study Calculator entities
using CRS.Services.Tenant;
using CRS.Models.Billing; // added for StripeEventLog

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CRS.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> {
        private const string DefaultSchema = "crs";
        private readonly IHttpContextAccessor? _httpContextAccessor;
        // SaaS Refactor: Inject tenant context
        private readonly ITenantContext? _tenantContext;
        private readonly string? _explicitConnection; // multi-tenant override

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor? httpContextAccessor = null, ITenantContext? tenantContext = null, string? explicitConnection = null)
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

            //builder.HasDefaultSchema(DefaultSchema);

            ConfigureEntities(builder);

            // Phase 1: Performance Indexes - Single column indexes for tenant queries
            builder.Entity<Community>().HasIndex(e => e.TenantId);
            builder.Entity<Contact>().HasIndex(e => e.TenantId);

            // Community address FK relationships
            builder.Entity<Community>()
                .HasOne(c => c.PhysicalAddress)
                .WithMany()
                .HasForeignKey(c => c.PhysicalAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Community>()
                .HasOne(c => c.MailingAddress)
                .WithMany()
                .HasForeignKey(c => c.MailingAddressId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);

            builder.Entity<PropertyManager>().HasIndex(e => e.TenantId);
            builder.Entity<ReserveStudy>().HasIndex(e => e.TenantId);
            
            // BuildingElement and CommonElement tenant indexes for tenant-specific elements
            builder.Entity<BuildingElement>()
                .HasIndex(be => new { be.TenantId, be.IsActive })
                .HasDatabaseName("IX_BuildingElement_Tenant_Active");
            builder.Entity<CommonElement>()
                .HasIndex(ce => new { ce.TenantId, ce.IsActive })
                .HasDatabaseName("IX_CommonElement_Tenant_Active");
            builder.Entity<FinancialInfo>().HasIndex(e => e.TenantId);
            builder.Entity<Proposal>().HasIndex(e => e.TenantId);
            builder.Entity<StudyRequest>().HasIndex(e => e.TenantId);
            builder.Entity<StudyRequest>().HasIndex(e => new { e.TenantId, e.CurrentStatus });
            builder.Entity<StudyStatusHistory>().HasIndex(e => e.TenantId);
            builder.Entity<StudyStatusHistory>().HasIndex(e => new { e.TenantId, e.RequestId, e.ChangedAt });
            builder.Entity<StudyStatusHistory>().HasIndex(e => new { e.TenantId, e.ToStatus, e.ChangedAt });
            builder.Entity<ApplicationUser>().HasIndex(u => u.TenantId);

            // Phase 1: Composite Indexes for Common Query Patterns
            // ReserveStudy composite indexes for frequently used queries
            builder.Entity<ReserveStudy>()
                .HasIndex(rs => new { rs.TenantId, rs.ApplicationUserId, rs.CommunityId })
                .HasDatabaseName("IX_ReserveStudy_Tenant_User_Community");
            
            builder.Entity<ReserveStudy>()
                .HasIndex(rs => new { rs.TenantId, rs.SpecialistUserId, rs.IsActive })
                .HasDatabaseName("IX_ReserveStudy_Tenant_Specialist_Active");
            
            builder.Entity<ReserveStudy>()
                .HasIndex(rs => new { rs.TenantId, rs.IsComplete, rs.IsActive })
                .HasDatabaseName("IX_ReserveStudy_Tenant_Status_Active");
            
            builder.Entity<ReserveStudy>()
                .HasIndex(rs => new { rs.TenantId, rs.IsActive, rs.DateCreated })
                .IncludeProperties(rs => new { rs.CommunityId, rs.ApplicationUserId, rs.SpecialistUserId, rs.IsComplete })
                .HasDatabaseName("IX_ReserveStudy_Tenant_Active_Created_Covering");

            // Community composite indexes
            builder.Entity<Community>()
                .HasIndex(c => new { c.TenantId, c.IsActive })
                .HasDatabaseName("IX_Community_Tenant_Active");

            // Contact filtered index for soft deletes
            builder.Entity<Contact>()
                .HasIndex(c => new { c.TenantId, c.ApplicationUserId })
                .HasFilter("[DateDeleted] IS NULL")
                .HasDatabaseName("IX_Contact_Tenant_User_NotDeleted");

            // FinancialInfo composite index
            builder.Entity<FinancialInfo>()
                .HasIndex(f => new { f.TenantId, f.ReserveStudyId })
                .HasFilter("[DateDeleted] IS NULL")
                .HasDatabaseName("IX_FinancialInfo_Tenant_Study_NotDeleted");

            // Proposal composite index
            builder.Entity<Proposal>()
                .HasIndex(p => new { p.TenantId, p.ReserveStudyId, p.IsApproved })
                .HasFilter("[DateDeleted] IS NULL")
                .HasDatabaseName("IX_Proposal_Tenant_Study_Approved_NotDeleted");

            // Scope comparison indexes
            builder.Entity<ScopeComparison>()
                .HasIndex(sc => new { sc.TenantId, sc.ReserveStudyId })
                .HasDatabaseName("IX_ScopeComparison_Tenant_Study");
            
            builder.Entity<ScopeComparison>()
                .HasIndex(sc => new { sc.TenantId, sc.Status })
                .HasDatabaseName("IX_ScopeComparison_Tenant_Status");

            // Element indexes for service contact queries
            builder.Entity<ReserveStudyBuildingElement>()
                .HasIndex(e => new { e.ReserveStudyId, e.BuildingElementId })
                .HasDatabaseName("IX_RSBuildingElement_Study_Element");
            
            builder.Entity<ReserveStudyCommonElement>()
                .HasIndex(e => new { e.ReserveStudyId, e.CommonElementId })
                .HasDatabaseName("IX_RSCommonElement_Study_Element");

            // AuditLog performance index
            builder.Entity<AuditLog>()
                .HasIndex(a => new { a.TableName, a.CreatedAt })
                .IncludeProperties(a => new { a.RecordId, a.ColumnName, a.Action })
                .HasDatabaseName("IX_AuditLog_Table_Created_Covering");

            // SaaS Refactor: Tenant-aware query filters on principals
            // Apply a global tenant query filter to all entities implementing ITenantScoped
            ApplyTenantQueryFilters(builder);

            // Matching filters on dependents to avoid EF warnings with required relationships
            builder.Entity<ReserveStudyBuildingElement>().HasQueryFilter(e => _tenantContext == null || _tenantContext.TenantId == null || (e.ReserveStudy != null && e.ReserveStudy.TenantId == _tenantContext.TenantId));
            builder.Entity<ReserveStudyCommonElement>().HasQueryFilter(e => _tenantContext == null || _tenantContext.TenantId == null || (e.ReserveStudy != null && e.ReserveStudy.TenantId == _tenantContext.TenantId));
            builder.Entity<ReserveStudyAdditionalElement>().HasQueryFilter(e => _tenantContext == null || _tenantContext.TenantId == null || (e.ReserveStudy != null && e.ReserveStudy.TenantId == _tenantContext.TenantId));
            builder.Entity<ContactXContactGroup>().HasQueryFilter(e => _tenantContext == null || _tenantContext.TenantId == null || (e.Contact != null && e.Contact.TenantId == _tenantContext.TenantId));

            // Configure 1:many relationship for ReserveStudy to Proposals (to support amendments)
            builder.Entity<ReserveStudy>()
                .HasMany(r => r.Proposals)
                .WithOne(p => p.ReserveStudy)
                .HasForeignKey(p => p.ReserveStudyId)
                .OnDelete(DeleteBehavior.Cascade);
            
            // Configure the CurrentProposal reference (optional, no cascade to avoid cycles)
            builder.Entity<ReserveStudy>()
                .HasOne(r => r.CurrentProposal)
                .WithMany()
                .HasForeignKey(r => r.CurrentProposalId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<ReserveStudy>()
                .HasOne(r => r.FinancialInfo)
                .WithOne(f => f.ReserveStudy)
                .HasForeignKey<FinancialInfo>(f => f.ReserveStudyId);

            // Shared PK1:1 ReserveStudy <-> StudyRequest
            builder.Entity<StudyRequest>()
                .HasOne(r => r.ReserveStudy)
                .WithOne(s => s.StudyRequest)
                .HasForeignKey<StudyRequest>(r => r.Id);

            // Configure optional relationships for Contact and PropertyManager
            builder.Entity<ReserveStudy>()
                .HasOne(r => r.Contact)
                .WithMany()
                .HasForeignKey(r => r.ContactId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReserveStudy>()
                .HasOne(r => r.PropertyManager)
                .WithMany()
                .HasForeignKey(r => r.PropertyManagerId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // AcceptanceTermsTemplate entity configuration
            builder.Entity<AcceptanceTermsTemplate>(entity => {
                entity.ToTable("AcceptanceTermsTemplates");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.Type, e.IsDefault })
                    .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant_Type_Default");
                entity.HasIndex(e => new { e.TenantId, e.Version })
                    .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant_Version");
                entity.HasIndex(e => new { e.TenantId, e.IsActive, e.EffectiveDate })
                    .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant_Active_Effective");
            });

            // Reserve Study Calculator entities
            builder.Entity<TenantReserveSettings>(entity => {
                entity.ToTable("TenantReserveSettings");
                entity.HasKey(e => e.TenantId);
                entity.HasOne(e => e.Tenant)
                    .WithOne()
                    .HasForeignKey<TenantReserveSettings>(e => e.TenantId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ReserveStudyScenario>(entity => {
                entity.ToTable("ReserveStudyScenarios");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_ReserveStudyScenario_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId })
                    .HasDatabaseName("IX_ReserveStudyScenario_Tenant_Study");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.Status })
                    .HasFilter("[DateDeleted] IS NULL")
                    .HasDatabaseName("IX_ReserveStudyScenario_Tenant_Study_Status");
                entity.HasOne(e => e.ReserveStudy)
                    .WithMany()
                    .HasForeignKey(e => e.ReserveStudyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ReserveScenarioComponent>(entity => {
                entity.ToTable("ReserveScenarioComponents");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_ReserveScenarioComponent_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ScenarioId })
                    .HasDatabaseName("IX_ReserveScenarioComponent_Tenant_Scenario");
                entity.HasIndex(e => new { e.TenantId, e.ScenarioId, e.Category })
                    .HasFilter("[DateDeleted] IS NULL")
                    .HasDatabaseName("IX_ReserveScenarioComponent_Tenant_Scenario_Category");
                entity.HasOne(e => e.Scenario)
                    .WithMany(s => s.Components)
                    .HasForeignKey(e => e.ScenarioId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(e => e.LinkedBuildingElement)
                    .WithMany()
                    .HasForeignKey(e => e.LinkedBuildingElementId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(e => e.LinkedCommonElement)
                    .WithMany()
                    .HasForeignKey(e => e.LinkedCommonElementId)
                    .OnDelete(DeleteBehavior.SetNull);
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
            // When _tenantContext is null (e.g., during startup), no tenant filter is applied
            builder.Entity<TEntity>().HasQueryFilter(e => _tenantContext == null || _tenantContext.TenantId == null || e.TenantId == _tenantContext.TenantId);
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

            // Phase 2: ElementOption consolidated configuration
            builder.Entity<ElementOption>(entity => {
                entity.ToTable("ElementOptions");
                entity.HasIndex(e => new { e.OptionType, e.IsActive, e.ZOrder })
                    .HasDatabaseName("IX_ElementOption_Type_Active_Order");
                entity.Property(e => e.DisplayText).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Unit).IsRequired().HasMaxLength(50);
            });

            // TenantElementOrder configuration for tenant-specific element ordering
            builder.Entity<TenantElementOrder>(entity => {
                entity.ToTable("TenantElementOrders");
                entity.HasIndex(e => new { e.TenantId, e.ElementType, e.ElementId })
                    .IsUnique()
                    .HasDatabaseName("IX_TenantElementOrder_Tenant_Type_Element");
                entity.HasIndex(e => new { e.TenantId, e.ElementType, e.ZOrder })
                    .HasDatabaseName("IX_TenantElementOrder_Tenant_Type_Order");
                entity.Property(e => e.CustomName).HasMaxLength(200);
            });

            // Document entity configuration
            builder.Entity<Document>(entity => {
                entity.ToTable("Documents");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_Document_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId })
                    .HasFilter("[DateDeleted] IS NULL")
                    .HasDatabaseName("IX_Document_Tenant_Study_NotDeleted");
                entity.HasIndex(e => new { e.TenantId, e.CommunityId })
                    .HasFilter("[DateDeleted] IS NULL")
                    .HasDatabaseName("IX_Document_Tenant_Community_NotDeleted");
                entity.HasIndex(e => new { e.TenantId, e.Type, e.IsPublic })
                    .HasDatabaseName("IX_Document_Tenant_Type_Public");
            });

            // EmailLog entity configuration
            builder.Entity<EmailLog>(entity => {
                entity.ToTable("EmailLogs");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_EmailLog_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ToEmail, e.SentAt })
                    .HasDatabaseName("IX_EmailLog_Tenant_Email_Sent");
                entity.HasIndex(e => new { e.TenantId, e.Status, e.SentAt })
                    .HasDatabaseName("IX_EmailLog_Tenant_Status_Sent");
                entity.HasIndex(e => e.ExternalMessageId)
                    .HasDatabaseName("IX_EmailLog_ExternalMessageId");
            });

            // Notification entity configuration (updated model)
            builder.Entity<Notification>(entity => {
                entity.ToTable("Notifications");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_Notification_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.UserId, e.IsRead })
                    .HasDatabaseName("IX_Notification_Tenant_User_Read");
                entity.HasIndex(e => new { e.TenantId, e.UserId, e.DateCreated })
                    .HasDatabaseName("IX_Notification_Tenant_User_Created");
                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // CalendarEvent entity configuration (schema fix - now tenant-scoped)
            builder.Entity<CalendarEvent>(entity => {
                entity.ToTable("CalendarEvents");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_CalendarEvent_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.Start, e.End })
                    .HasDatabaseName("IX_CalendarEvent_Tenant_DateRange");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId })
                    .HasFilter("[ReserveStudyId] IS NOT NULL")
                    .HasDatabaseName("IX_CalendarEvent_Tenant_Study");
                entity.HasIndex(e => new { e.TenantId, e.EventType })
                    .HasDatabaseName("IX_CalendarEvent_Tenant_Type");
                entity.HasOne(e => e.ReserveStudy)
                    .WithMany()
                    .HasForeignKey(e => e.ReserveStudyId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ServiceContact entity configuration (schema fix - now tenant-scoped)
            builder.Entity<ServiceContact>(entity => {
                entity.ToTable("ServiceContacts");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_ServiceContact_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ContactType, e.IsActive })
                    .HasDatabaseName("IX_ServiceContact_Tenant_Type_Active");
                entity.HasIndex(e => new { e.TenantId, e.CompanyName })
                    .HasFilter("[DateDeleted] IS NULL")
                    .HasDatabaseName("IX_ServiceContact_Tenant_Company_NotDeleted");
            });

            // SupportTicket entity configuration (schema fix - enhanced)
            builder.Entity<SupportTicket>(entity => {
                entity.ToTable("SupportTickets");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_SupportTicket_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.Status, e.Priority })
                    .HasDatabaseName("IX_SupportTicket_Tenant_Status_Priority");
                entity.HasIndex(e => new { e.TenantId, e.AssignedToUserId, e.Status })
                    .HasFilter("[AssignedToUserId] IS NOT NULL")
                    .HasDatabaseName("IX_SupportTicket_Tenant_Assigned_Status");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId })
                    .HasFilter("[ReserveStudyId] IS NOT NULL")
                    .HasDatabaseName("IX_SupportTicket_Tenant_Study");
                entity.HasOne(t => t.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(t => t.CreatedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.AssignedToUser)
                    .WithMany()
                    .HasForeignKey(t => t.AssignedToUserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(t => t.ReserveStudy)
                    .WithMany()
                    .HasForeignKey(t => t.ReserveStudyId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // TicketComment entity configuration
            builder.Entity<TicketComment>(entity => {
                entity.ToTable("TicketComments");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_TicketComment_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.TicketId, e.PostedAt })
                    .HasDatabaseName("IX_TicketComment_Tenant_Ticket_Posted");
                entity.HasIndex(e => new { e.TenantId, e.TicketId, e.Visibility })
                    .HasDatabaseName("IX_TicketComment_Tenant_Ticket_Visibility");
                entity.HasOne(c => c.Ticket)
                    .WithMany()
                    .HasForeignKey(c => c.TicketId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(c => c.Author)
                    .WithMany()
                    .HasForeignKey(c => c.AuthorUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // SiteVisitPhoto entity configuration (medium priority)
            builder.Entity<SiteVisitPhoto>(entity => {
                entity.ToTable("SiteVisitPhotos");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_SiteVisitPhoto_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.SortOrder })
                    .HasDatabaseName("IX_SiteVisitPhoto_Tenant_Study_Order");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.Category })
                    .HasDatabaseName("IX_SiteVisitPhoto_Tenant_Study_Category");
                entity.HasIndex(e => new { e.TenantId, e.ElementId, e.ElementType })
                    .HasFilter("[ElementId] IS NOT NULL")
                    .HasDatabaseName("IX_SiteVisitPhoto_Tenant_Element");
                entity.HasOne(p => p.ReserveStudy)
                    .WithMany()
                    .HasForeignKey(p => p.ReserveStudyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.TakenBy)
                    .WithMany()
                    .HasForeignKey(p => p.TakenByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StudyNote entity configuration (medium priority)
            builder.Entity<StudyNote>(entity => {
                entity.ToTable("StudyNotes");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_StudyNote_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.Visibility })
                    .HasDatabaseName("IX_StudyNote_Tenant_Study_Visibility");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.IsPinned })
                    .HasDatabaseName("IX_StudyNote_Tenant_Study_Pinned");
                entity.HasIndex(e => new { e.TenantId, e.AuthorUserId })
                    .HasDatabaseName("IX_StudyNote_Tenant_Author");
                entity.HasOne(n => n.ReserveStudy)
                    .WithMany()
                    .HasForeignKey(n => n.ReserveStudyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(n => n.Author)
                    .WithMany()
                    .HasForeignKey(n => n.AuthorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(n => n.ResolvedBy)
                    .WithMany()
                    .HasForeignKey(n => n.ResolvedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(n => n.ParentNote)
                    .WithMany(n => n.Replies)
                    .HasForeignKey(n => n.ParentNoteId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // GeneratedReport entity configuration (medium priority)
            builder.Entity<GeneratedReport>(entity => {
                entity.ToTable("GeneratedReports");
                entity.HasIndex(e => e.TenantId)
                    .HasDatabaseName("IX_GeneratedReport_Tenant");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.Type })
                    .HasDatabaseName("IX_GeneratedReport_Tenant_Study_Type");
                entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.Status })
                    .HasDatabaseName("IX_GeneratedReport_Tenant_Study_Status");
                entity.HasIndex(e => new { e.TenantId, e.IsPublishedToClient, e.PublishedAt })
                    .HasDatabaseName("IX_GeneratedReport_Tenant_Published");
                entity.HasOne(r => r.ReserveStudy)
                    .WithMany()
                    .HasForeignKey(r => r.ReserveStudyId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(r => r.GeneratedBy)
                    .WithMany()
                    .HasForeignKey(r => r.GeneratedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(r => r.PublishedBy)
                    .WithMany()
                    .HasForeignKey(r => r.PublishedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(r => r.ReviewedBy)
                    .WithMany()
                    .HasForeignKey(r => r.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
                entity.HasOne(r => r.SupersedesReport)
                            .WithMany()
                            .HasForeignKey(r => r.SupersedesReportId)
                            .OnDelete(DeleteBehavior.NoAction);
                    });

                    // ProposalAcceptance entity configuration (click-wrap agreements)
                    builder.Entity<ProposalAcceptance>(entity => {
                        entity.ToTable("ProposalAcceptances");
                        entity.HasIndex(e => e.TenantId)
                            .HasDatabaseName("IX_ProposalAcceptance_Tenant");
                        entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId })
                            .HasDatabaseName("IX_ProposalAcceptance_Tenant_Study");
                        entity.HasIndex(e => new { e.TenantId, e.AcceptedByUserId, e.AcceptedAt })
                            .HasDatabaseName("IX_ProposalAcceptance_Tenant_User_Date");
                        entity.HasIndex(e => new { e.TenantId, e.IsValid })
                            .HasDatabaseName("IX_ProposalAcceptance_Tenant_Valid");
                        entity.HasOne(a => a.ReserveStudy)
                            .WithMany()
                            .HasForeignKey(a => a.ReserveStudyId)
                            .OnDelete(DeleteBehavior.Cascade);
                        entity.HasOne(a => a.Proposal)
                            .WithMany()
                            .HasForeignKey(a => a.ProposalId)
                            .OnDelete(DeleteBehavior.NoAction);
                        entity.HasOne(a => a.AcceptedByUser)
                            .WithMany()
                            .HasForeignKey(a => a.AcceptedByUserId)
                            .OnDelete(DeleteBehavior.NoAction);
                        entity.HasOne(a => a.AcceptanceTermsTemplate)
                            .WithMany(t => t.Acceptances)
                            .HasForeignKey(a => a.AcceptanceTermsTemplateId)
                            .OnDelete(DeleteBehavior.NoAction);
                    });

                    // AcceptanceTermsTemplate entity configuration
                    builder.Entity<AcceptanceTermsTemplate>(entity => {
                        entity.ToTable("AcceptanceTermsTemplates");
                        entity.HasIndex(e => e.TenantId)
                            .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant");
                        entity.HasIndex(e => new { e.TenantId, e.Type, e.IsDefault })
                            .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant_Type_Default");
                        entity.HasIndex(e => new { e.TenantId, e.Version })
                            .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant_Version");
                        entity.HasIndex(e => new { e.TenantId, e.IsActive, e.EffectiveDate })
                            .HasDatabaseName("IX_AcceptanceTermsTemplate_Tenant_Active_Effective");
                    });

                    // Reserve Study Calculator entities
                    builder.Entity<TenantReserveSettings>(entity => {
                        entity.ToTable("TenantReserveSettings");
                        entity.HasKey(e => e.TenantId);
                        entity.HasOne(e => e.Tenant)
                            .WithOne()
                            .HasForeignKey<TenantReserveSettings>(e => e.TenantId)
                            .OnDelete(DeleteBehavior.Cascade);
                    });

                    builder.Entity<ReserveStudyScenario>(entity => {
                        entity.ToTable("ReserveStudyScenarios");
                        entity.HasIndex(e => e.TenantId)
                            .HasDatabaseName("IX_ReserveStudyScenario_Tenant");
                        entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId })
                            .HasDatabaseName("IX_ReserveStudyScenario_Tenant_Study");
                        entity.HasIndex(e => new { e.TenantId, e.ReserveStudyId, e.Status })
                            .HasFilter("[DateDeleted] IS NULL")
                            .HasDatabaseName("IX_ReserveStudyScenario_Tenant_Study_Status");
                        entity.HasOne(e => e.ReserveStudy)
                            .WithMany()
                            .HasForeignKey(e => e.ReserveStudyId)
                            .OnDelete(DeleteBehavior.Cascade);
                    });

                    builder.Entity<ReserveScenarioComponent>(entity => {
                        entity.ToTable("ReserveScenarioComponents");
                        entity.HasIndex(e => e.TenantId)
                            .HasDatabaseName("IX_ReserveScenarioComponent_Tenant");
                        entity.HasIndex(e => new { e.TenantId, e.ScenarioId })
                            .HasDatabaseName("IX_ReserveScenarioComponent_Tenant_Scenario");
                        entity.HasIndex(e => new { e.TenantId, e.ScenarioId, e.Category })
                            .HasFilter("[DateDeleted] IS NULL")
                            .HasDatabaseName("IX_ReserveScenarioComponent_Tenant_Scenario_Category");
                        entity.HasOne(e => e.Scenario)
                            .WithMany(s => s.Components)
                            .HasForeignKey(e => e.ScenarioId)
                            .OnDelete(DeleteBehavior.Cascade);
                        entity.HasOne(e => e.LinkedBuildingElement)
                            .WithMany()
                            .HasForeignKey(e => e.LinkedBuildingElementId)
                            .OnDelete(DeleteBehavior.SetNull);
                        entity.HasOne(e => e.LinkedCommonElement)
                            .WithMany()
                            .HasForeignKey(e => e.LinkedCommonElementId)
                            .OnDelete(DeleteBehavior.SetNull);
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
            var userIdString = _httpContextAccessor?.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
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
            var tenantId = _tenantContext?.TenantId; // no default, new site may have no tenant context
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
        public DbSet<AuditLogArchive> AuditLogArchives { get; set; } // Phase 1: Archive table
        public DbSet<BuildingElement> BuildingElements { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }
        public DbSet<CommonElement> CommonElements { get; set; }
        public DbSet<Community> Communities { get; set; }
        public DbSet<Contact> Contacts { get; set; }
        public DbSet<ContactGroup> ContactGroups { get; set; }
        public DbSet<ContactXContactGroup> ContactXContactGroups { get; set; }
        
        // Phase 2: Consolidated element options (replaces 3 separate tables)
        public DbSet<ElementOption> ElementOptions { get; set; }
        
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
        public DbSet<CRS.Models.TicketComment> TicketComments { get; set; }
        
        // System-wide settings
        public DbSet<CRS.Models.SystemSettings> SystemSettings { get; set; }
        
        // Demo environment
        public DbSet<CRS.Models.Demo.DemoSession> DemoSessions { get; set; }
        
        // Tenant-specific element ordering
        public DbSet<TenantElementOrder> TenantElementOrders { get; set; }
        
        // High Priority Schema Enhancements
        public DbSet<Document> Documents { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }
        
        // Medium Priority Schema Enhancements
        public DbSet<SiteVisitPhoto> SiteVisitPhotos { get; set; }
        public DbSet<StudyNote> StudyNotes { get; set; }
        public DbSet<GeneratedReport> GeneratedReports { get; set; }
        
        // Click-Wrap Agreements (formerly E-Signatures)
        public DbSet<ProposalAcceptance> ProposalAcceptances { get; set; }
        public DbSet<AcceptanceTermsTemplate> AcceptanceTermsTemplates { get; set; }
        
        // Reserve Study Calculator
        public DbSet<TenantReserveSettings> TenantReserveSettings { get; set; }
        public DbSet<ReserveStudyScenario> ReserveStudyScenarios { get; set; }
        public DbSet<ReserveScenarioComponent> ReserveScenarioComponents { get; set; }
        
        // Scope Change Management
        public DbSet<TenantScopeChangeSettings> TenantScopeChangeSettings { get; set; }
        public DbSet<ScopeComparison> ScopeComparisons { get; set; }
        #endregion

        #region Seed Data
        private void SeedData(DbContext context) {
            GenerateBuildingElements(context);
            context.SaveChanges();
            GenerateCommonElements(context);
            context.SaveChanges();
            
            // Phase 2: Use consolidated ElementOptions instead of separate tables
            GenerateElementOptions(context);
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

        private static void GenerateElementOptions(DbContext context) {
            // Phase 2: Consolidated element options seeding
            List<ElementOption> options = new();

            // Measurement options
            options.Add(new ElementOption { OptionType = ElementOptionType.Measurement, DisplayText = "Square Feet", Unit = "sq. ft.", ZOrder = 0, IsActive = true });
            options.Add(new ElementOption { OptionType = ElementOptionType.Measurement, DisplayText = "Linear Feet", Unit = "LF.", ZOrder = 1, IsActive = true });
            options.Add(new ElementOption { OptionType = ElementOptionType.Measurement, DisplayText = "Each", Unit = "ea.", ZOrder = 2, IsActive = true });

            // Remaining Life options
            var remainingLifeRanges = new[] {
                ("1-5 Years", "1-5", 0, 1, 5),
                ("6-10 Years", "6-10", 1, 6, 10),
                ("11-15 Years", "11-15", 2, 11, 15),
                ("16-20 Years", "16-20", 3, 16, 20),
                ("21-25 Years", "21-25", 4, 21, 25),
                ("26-30 Years", "26-30", 5, 26, 30),
                ("31-35 Years", "31-35", 6, 31, 35),
                ("36-40 Years", "36-40", 7, 36, 40),
                ("41-45 Years", "41-45", 8, 41, 45),
                ("46-50 Years", "46-50", 9, 46, 50),
                ("51-55 Years", "51-55", 10, 51, 55),
                ("56-60 Years", "56-60", 11, 56, 60),
                ("61-65 Years", "61-65", 12, 61, 65),
                ("66-70 Years", "66-70", 13, 66, 70),
                ("71-75 Years", "71-75", 14, 71, 75),
                ("76-80 Years", "76-80", 15, 76, 80)
            };

            foreach (var (display, unit, order, min, max) in remainingLifeRanges) {
                options.Add(new ElementOption {
                    OptionType = ElementOptionType.RemainingLife,
                    DisplayText = display,
                    Unit = unit,
                    MinValue = min,
                    MaxValue = max,
                    ZOrder = order,
                    IsActive = true
                });
            }

            // Useful Life options (same ranges as remaining life)
            foreach (var (display, unit, order, min, max) in remainingLifeRanges) {
                options.Add(new ElementOption {
                    OptionType = ElementOptionType.UsefulLife,
                    DisplayText = display,
                    Unit = unit,
                    MinValue = min,
                    MaxValue = max,
                    ZOrder = order,
                    IsActive = true
                });
            }

            // Only add options that don't already exist
            foreach (var option in options) {
                var exists = context.Set<ElementOption>().Any(e =>
                    e.OptionType == option.OptionType &&
                    e.DisplayText == option.DisplayText);

                if (!exists) {
                    context.Set<ElementOption>().Add(option);
                }
            }
        }
        #endregion
    }
}

