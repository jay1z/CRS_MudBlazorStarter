﻿using System.Security.Claims;

using CRS.Models;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CRS.Data {
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid> {
        private const string DefaultSchema = "crs";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
            : base(options) {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder
                .ConfigureWarnings(warnings => warnings.Log(RelationalEventId.PendingModelChangesWarning))
                //.ConfigureWarnings(w => w.Throw(SqlServerEventId.SavepointsDisabledBecauseOfMARS))
                .UseSeeding((context, _) => {
                    SeedData(context);
                });
        }

        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.HasDefaultSchema(DefaultSchema);

            ConfigureEntities(builder);
        }

        private void ConfigureEntities(ModelBuilder builder) {
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
        }

        public override int SaveChanges() {
            AddAuditLogs();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) {
            AddAuditLogs();
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


        #region DbSet Properties
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
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<PropertyManager> PropertyManagers { get; set; }
        public DbSet<ReserveStudyAdditionalElement> ReserveStudyAdditionalElements { get; set; }
        public DbSet<ReserveStudyBuildingElement> ReserveStudyBuildingElements { get; set; }
        public DbSet<ReserveStudyCommonElement> ReserveStudyCommonElements { get; set; }
        public DbSet<ReserveStudy> ReserveStudies { get; set; }
        public DbSet<ServiceContact> ServiceContacts { get; set; }
        public DbSet<Settings> Settings { get; set; }
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
        }

        private void GenerateBuildingElements(DbContext context) {
            IEnumerable<BuildingElement> buildingElements =
            [
                new BuildingElement {  Name = "Pitched Roof", IsActive = true, NeedsService = true },
                new BuildingElement {  Name = "Flat Roof", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Siding", IsActive = true, NeedsService = true },
                new BuildingElement {  Name = "Gutters/Downspouts", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Attached Lighting", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Shutters", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Decks", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Flooring", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Lighting", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Intercom", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Security System", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Elevator(s)", IsActive = true, NeedsService = true },
                new BuildingElement {  Name = "AC Unit(s)", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Heating Unit(s)", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Water Unit(s)", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Kitchen", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Bathroom(s)", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Doors", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Windows", IsActive = true, NeedsService = false },
                new BuildingElement {  Name = "Balconies", IsActive = true, NeedsService = false }
            ];

            foreach (BuildingElement buildingElement in buildingElements) {
                BuildingElement element = context.Set<BuildingElement>().FirstOrDefault(e => e.Name == buildingElement.Name);
                if (element is null) {
                    context.Set<BuildingElement>().Add(buildingElement);
                }
            }
        }

        private void GenerateCommonElements(DbContext context) {
            IEnumerable<CommonElement> commonElements =
            [
                new CommonElement {  Name = "Clubhouse", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Pool", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Playground", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Tennis/Ball Courts", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Property Fencing", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Pool(s)/Lake(s)", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Gazebos(s)/Pavilion(s)", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Entry Signage/Structure(s)", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Street Signage", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Roads", IsActive = true, NeedsService = true },
                new CommonElement {  Name = "Catch Basins", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Parking", IsActive = true, NeedsService = true },
                new CommonElement {  Name = "Sidewalks", IsActive = true, NeedsService = true },
                new CommonElement {  Name = "Driveways", IsActive = true, NeedsService = true },
                new CommonElement {  Name = "Patios", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Porches", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Privacy Fencing", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Garage(s)", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Pump Station", IsActive = true, NeedsService = true },
                new CommonElement {  Name = "Retaining Walls", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Fountains", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Property Lighting", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Street Lighting", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Paved Trails/Paths", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Mail Huts/Boxes", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Fire Hydrants", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Sports Fields", IsActive = true, NeedsService = false },
                new CommonElement {  Name = "Shed(s)/Storage", IsActive = true, NeedsService = false }
            ];
            foreach (CommonElement commonElement in commonElements) {
                CommonElement element = context.Set<CommonElement>().FirstOrDefault(e => e.Name == commonElement.Name);
                if (element is null) {
                    context.Set<CommonElement>().Add(commonElement);
                }
            }
        }

        private void GenerateElementMeasurementOptions(DbContext context) {
            IEnumerable<ElementMeasurementOptions> elementMeasurementOptions =
            [
                new ElementMeasurementOptions {  DisplayText = "Square Feet", Unit = "sq. ft.", ZOrder = 0 },
                new ElementMeasurementOptions {  DisplayText = "Linear Feet", Unit = "LF.", ZOrder = 0 },
                new ElementMeasurementOptions {  DisplayText = "Each", Unit = "ea.", ZOrder = 0 }
            ];
            foreach (ElementMeasurementOptions elementMeasurementOption in elementMeasurementOptions) {
                ElementMeasurementOptions option = context.Set<ElementMeasurementOptions>().FirstOrDefault(e => e.DisplayText == elementMeasurementOption.DisplayText);
                if (option is null) {
                    context.Set<ElementMeasurementOptions>().Add(elementMeasurementOption);
                }
            }
        }

        private void GenerateElementRemainingLifeOptions(DbContext context) {
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
                ElementRemainingLifeOptions option = context.Set<ElementRemainingLifeOptions>().FirstOrDefault(e => e.DisplayText == elementRemainingLifeOption.DisplayText);
                if (option is null) {
                    context.Set<ElementRemainingLifeOptions>().Add(elementRemainingLifeOption);
                }
            }
        }

        private void GenerateElementUsefulLifeOptions(DbContext context) {
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
                ElementUsefulLifeOptions option = context.Set<ElementUsefulLifeOptions>().FirstOrDefault(e => e.DisplayText == elementUsefulLifeOption.DisplayText);
                if (option is null) {
                    context.Set<ElementUsefulLifeOptions>().Add(elementUsefulLifeOption);
                }
            }
        }
        #endregion
    }
}

