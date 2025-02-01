using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CRS.Models;

namespace CRS.Data {
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options) {
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);

            builder.HasDefaultSchema("crs_mvc");


            builder.Entity<BuildingElement>().HasData(
                new BuildingElement { Id = 1, Name = "Pitched Roof", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new BuildingElement { Id = 2, Name = "Flat Roof", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 3, Name = "Siding", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new BuildingElement { Id = 4, Name = "Gutters/Downspouts", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 5, Name = "Attached Lighting", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 6, Name = "Shutters", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 7, Name = "Decks", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 8, Name = "Flooring", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 9, Name = "Lighting", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 10, Name = "Intercom", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 11, Name = "Security System", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 12, Name = "Elevator(s)", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new BuildingElement { Id = 13, Name = "AC Unit(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 14, Name = "Heating Unit(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 15, Name = "Water Unit(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 16, Name = "Kitchen", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 17, Name = "Bathroom(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 18, Name = "Doors", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 19, Name = "Windows", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new BuildingElement { Id = 20, Name = "Balconies", IsActive = true, NeedsService = false, DateCreated = DateTime.Now }
            );
            builder.Entity<CommonElement>().HasData(
                new CommonElement { Id = 1, Name = "Clubhouse", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 2, Name = "Pool", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 3, Name = "Playground", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 4, Name = "Tennis/Ball Courts", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 5, Name = "Property Fencing", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 6, Name = "Pool(s)/Lake(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 7, Name = "Gazebos(s)/Pavilion(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 8, Name = "Entry Signage/Structure(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 9, Name = "Street Signage", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 10, Name = "Roads", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new CommonElement { Id = 11, Name = "Catch Basins", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 12, Name = "Parking", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new CommonElement { Id = 13, Name = "Sidewalks", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new CommonElement { Id = 14, Name = "Driveways", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new CommonElement { Id = 15, Name = "Patios", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 16, Name = "Porches", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 17, Name = "Privacy Fencing", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 18, Name = "Garage(s)", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 19, Name = "Pump Station", IsActive = true, NeedsService = true, DateCreated = DateTime.Now },
                new CommonElement { Id = 20, Name = "Retaining Walls", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 21, Name = "Fountains", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 22, Name = "Property Lighting", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 23, Name = "Street Lighting", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 24, Name = "Paved Trails/Paths", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 25, Name = "Mail Huts/Boxes", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 26, Name = "Fire Hydrants", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 27, Name = "Sports Fields", IsActive = true, NeedsService = false, DateCreated = DateTime.Now },
                new CommonElement { Id = 28, Name = "Shed(s)/Storage", IsActive = true, NeedsService = false, DateCreated = DateTime.Now }
            );
            builder.Entity<ElementMeasurementOptions>().HasData(
                new ElementMeasurementOptions { Id = 1, DisplayText = "Square Feet", Unit = "sq. ft.", ZOrder = 0, DateCreated = DateTime.Now },
                new ElementMeasurementOptions { Id = 2, DisplayText = "Linear Feet", Unit = "LF.", ZOrder = 0, DateCreated = DateTime.Now },
                new ElementMeasurementOptions { Id = 3, DisplayText = "Each", Unit = "ea.", ZOrder = 0, DateCreated = DateTime.Now }
            );
            builder.Entity<ElementRemainingLifeOptions>().HasData(
                new ElementRemainingLifeOptions { Id = 1, DisplayText = "1-5 Years", Unit = "1-5", ZOrder = 0, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 2, DisplayText = "6-10 Years", Unit = "6-10", ZOrder = 1, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 3, DisplayText = "11-15 Years", Unit = "11-15", ZOrder = 2, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 4, DisplayText = "16-20 Years", Unit = "16-20", ZOrder = 3, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 5, DisplayText = "21-25 Years", Unit = "21-25", ZOrder = 4, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 6, DisplayText = "26-30 Years", Unit = "26-30", ZOrder = 5, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 7, DisplayText = "31-35 Years", Unit = "31-35", ZOrder = 6, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 8, DisplayText = "36-40 Years", Unit = "36-40", ZOrder = 7, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 9, DisplayText = "41-45 Years", Unit = "41-45", ZOrder = 8, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 10, DisplayText = "46-50 Years", Unit = "46-50", ZOrder = 9, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 11, DisplayText = "51-55 Years", Unit = "51-55", ZOrder = 10, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 12, DisplayText = "56-60 Years", Unit = "56-60", ZOrder = 11, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 13, DisplayText = "61-65 Years", Unit = "61-65", ZOrder = 12, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 14, DisplayText = "66-70 Years", Unit = "66-70", ZOrder = 13, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 15, DisplayText = "71-75 Years", Unit = "71-75", ZOrder = 14, DateCreated = DateTime.Now },
                new ElementRemainingLifeOptions { Id = 16, DisplayText = "76-80 Years", Unit = "76-80", ZOrder = 15, DateCreated = DateTime.Now }
            );
            builder.Entity<ElementUsefulLifeOptions>().HasData(
                new ElementUsefulLifeOptions { Id = 1, DisplayText = "1-5 Years", Unit = "1-5", ZOrder = 0, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 2, DisplayText = "6-10 Years", Unit = "6-10", ZOrder = 1, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 3, DisplayText = "11-15 Years", Unit = "11-15", ZOrder = 2, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 4, DisplayText = "16-20 Years", Unit = "16-20", ZOrder = 3, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 5, DisplayText = "21-25 Years", Unit = "21-25", ZOrder = 4, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 6, DisplayText = "26-30 Years", Unit = "26-30", ZOrder = 5, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 7, DisplayText = "31-35 Years", Unit = "31-35", ZOrder = 6, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 8, DisplayText = "36-40 Years", Unit = "36-40", ZOrder = 7, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 9, DisplayText = "41-45 Years", Unit = "41-45", ZOrder = 8, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 10, DisplayText = "46-50 Years", Unit = "46-50", ZOrder = 9, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 11, DisplayText = "51-55 Years", Unit = "51-55", ZOrder = 10, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 12, DisplayText = "56-60 Years", Unit = "56-60", ZOrder = 11, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 13, DisplayText = "61-65 Years", Unit = "61-65", ZOrder = 12, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 14, DisplayText = "66-70 Years", Unit = "66-70", ZOrder = 13, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 15, DisplayText = "71-75 Years", Unit = "71-75", ZOrder = 14, DateCreated = DateTime.Now },
                new ElementUsefulLifeOptions { Id = 16, DisplayText = "76-80 Years", Unit = "76-80", ZOrder = 15, DateCreated = DateTime.Now }
            );

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
    }
}
