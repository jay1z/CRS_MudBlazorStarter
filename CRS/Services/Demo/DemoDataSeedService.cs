using Microsoft.EntityFrameworkCore;
using CRS.Data;
using CRS.Models;

namespace CRS.Services.Demo
{
    public interface IDemoDataSeedService
    {
        Task SeedDemoDataAsync(Guid tenantId);
    }
    
    public class DemoDataSeedService : IDemoDataSeedService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DemoDataSeedService> _logger;
        
        public DemoDataSeedService(
            ApplicationDbContext context,
            ILogger<DemoDataSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }
        
        public async Task SeedDemoDataAsync(Guid tenantId)
        {
            try
            {
                _logger.LogInformation("Seeding demo data for tenant: {TenantId}", tenantId);
                
                // Create 3 sample properties
                var properties = new List<Community>
                {
                    CreateSampleProperty(tenantId, "Sunset Village HOA", 45, "Small"),
                    CreateSampleProperty(tenantId, "Oakwood Condominiums", 120, "Medium"),
                    CreateSampleProperty(tenantId, "Harbor View Estates", 280, "Large")
                };
                
                _context.Communities.AddRange(properties);
                await _context.SaveChangesAsync();
                
                // Create sample reserve studies for each property
                foreach (var property in properties)
                {
                    var study = CreateSampleReserveStudy(property);
                    _context.ReserveStudies.Add(study);
                    
                    // Add common elements to the study
                    var elements = CreateSampleElements(study.Id);
                    _context.ReserveStudyCommonElements.AddRange(elements);
                }
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Demo data seeded successfully for tenant: {TenantId}", tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding demo data for tenant: {TenantId}", tenantId);
                throw;
            }
        }
        
        private Community CreateSampleProperty(Guid tenantId, string name, int units, string size)
        {
            return new Community
            {
                TenantId = tenantId,
                Name = name,
                Address1 = GetSampleAddress(size),
                City = GetSampleCity(size),
                State = GetSampleState(size),
                ZipCode = GetSampleZip(size),
                PhoneNumber = "(555) 123-4567",
                Email = $"info@{name.ToLower().Replace(" ", "")}.com",
                NumberOfUnits = units,
                YearBuilt = size switch
                {
                    "Small" => 1985,
                    "Medium" => 1998,
                    "Large" => 2005,
                    _ => 2000
                },
                Description = $"A {size.ToLower()}-sized community with {units} residential units. This property includes typical common elements such as roofing, exterior painting, paving, and landscaping.",
                DateCreated = DateTime.UtcNow,
                IsDemo = true
            };
        }
        
        private ReserveStudy CreateSampleReserveStudy(Community property)
        {
            var startingBalance = property.NumberOfUnits switch
            {
                <= 50 => 125000m,
                <= 150 => 450000m,
                _ => 1200000m
            };
            
            var monthlyContribution = property.NumberOfUnits switch
            {
                <= 50 => 250m,
                <= 150 => 750m,
                _ => 2500m
            };
            
            return new ReserveStudy
            {
                CommunityId = property.Id,
                Name = $"{property.Name} - Reserve Study {DateTime.Now.Year}",
                StudyDate = DateTime.UtcNow,
                FiscalYearEnd = new DateTime(DateTime.Now.Year, 12, 31),
                CurrentReserveFunds = startingBalance,
                MonthlyReserveContribution = monthlyContribution,
                AnnualInflationRate = 3.0m,
                AnnualInterestRate = 2.0m,
                StudyType = "Full Update",
                PreparedBy = "Demo Account",
                Status = "Draft",
                Notes = "This is a sample reserve study created for demonstration purposes. All data is fictional.",
                DateCreated = DateTime.UtcNow,
                IsDemo = true
            };
        }
        
        private List<ReserveStudyCommonElement> CreateSampleElements(Guid studyId)
        {
            var elements = new List<ReserveStudyCommonElement>();
            
            // Roofing
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Asphalt Shingle Roofing",
                Description = "Replace asphalt shingle roofing on all residential buildings",
                Quantity = 15000m,
                Unit = "sq ft",
                UsefulLife = 25,
                RemainingLife = 8,
                ReplacementCost = 225000m,
                Category = "Roofing",
                Notes = "Inspection shows good condition overall with minor wear on south-facing slopes.",
                DateCreated = DateTime.UtcNow
            });
            
            // Exterior Painting
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Exterior Building Painting",
                Description = "Repaint all exterior building surfaces including trim",
                Quantity = 12000m,
                Unit = "sq ft",
                UsefulLife = 7,
                RemainingLife = 3,
                ReplacementCost = 84000m,
                Category = "Painting",
                Notes = "Current paint showing signs of fading and peeling in high-exposure areas.",
                DateCreated = DateTime.UtcNow
            });
            
            // Asphalt Paving
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Asphalt Paving Resurface",
                Description = "Mill and overlay asphalt paving in parking lots and driveways",
                Quantity = 8500m,
                Unit = "sq yd",
                UsefulLife = 15,
                RemainingLife = 6,
                ReplacementCost = 127500m,
                Category = "Paving",
                Notes = "Some cracking visible. Seal coating recommended in 2 years.",
                DateCreated = DateTime.UtcNow
            });
            
            // HVAC
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Common Area HVAC Systems",
                Description = "Replace HVAC units serving clubhouse and common areas",
                Quantity = 3m,
                Unit = "units",
                UsefulLife = 20,
                RemainingLife = 12,
                ReplacementCost = 45000m,
                Category = "Mechanical",
                Notes = "Units operating normally with regular maintenance.",
                DateCreated = DateTime.UtcNow
            });
            
            // Fencing
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Perimeter Fencing",
                Description = "Replace wood privacy fencing around property perimeter",
                Quantity = 850m,
                Unit = "linear ft",
                UsefulLife = 12,
                RemainingLife = 4,
                ReplacementCost = 25500m,
                Category = "Site Improvements",
                Notes = "Some sections showing rot at post bases.",
                DateCreated = DateTime.UtcNow
            });
            
            // Pool
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Swimming Pool Resurface",
                Description = "Resurface community swimming pool including tile and coping",
                Quantity = 1m,
                Unit = "pool",
                UsefulLife = 10,
                RemainingLife = 2,
                ReplacementCost = 35000m,
                Category = "Recreational",
                Notes = "Surface showing signs of wear. Plaster in fair condition.",
                DateCreated = DateTime.UtcNow
            });
            
            // Deck
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Pool Deck Resurfacing",
                Description = "Replace concrete pool deck with stamped concrete",
                Quantity = 2400m,
                Unit = "sq ft",
                UsefulLife = 20,
                RemainingLife = 11,
                ReplacementCost = 28800m,
                Category = "Recreational",
                Notes = "Some minor cracking present. No immediate concerns.",
                DateCreated = DateTime.UtcNow
            });
            
            // Playground
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Playground Equipment",
                Description = "Replace playground equipment including safety surfacing",
                Quantity = 1m,
                Unit = "lot",
                UsefulLife = 15,
                RemainingLife = 5,
                ReplacementCost = 42000m,
                Category = "Recreational",
                Notes = "Equipment in good condition. Safety surfacing within acceptable limits.",
                DateCreated = DateTime.UtcNow
            });
            
            // Landscaping
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Irrigation System Replacement",
                Description = "Replace underground irrigation system including controller",
                Quantity = 1m,
                Unit = "system",
                UsefulLife = 18,
                RemainingLife = 7,
                ReplacementCost = 38000m,
                Category = "Landscaping",
                Notes = "System functioning well with minor repairs needed seasonally.",
                DateCreated = DateTime.UtcNow
            });
            
            // Lighting
            elements.Add(new ReserveStudyCommonElement
            {
                ReserveStudyId = studyId,
                ElementName = "Exterior Site Lighting",
                Description = "Replace decorative site lighting fixtures throughout community",
                Quantity = 45m,
                Unit = "fixtures",
                UsefulLife = 25,
                RemainingLife = 15,
                ReplacementCost = 27000m,
                Category = "Electrical",
                Notes = "LED upgrade recommended for energy efficiency.",
                DateCreated = DateTime.UtcNow
            });
            
            return elements;
        }
        
        private string GetSampleAddress(string size) => size switch
        {
            "Small" => "1234 Sunset Boulevard",
            "Medium" => "5678 Oak Street",
            "Large" => "9012 Harbor Drive",
            _ => "123 Main Street"
        };
        
        private string GetSampleCity(string size) => size switch
        {
            "Small" => "Springfield",
            "Medium" => "Riverside",
            "Large" => "Seaside",
            _ => "Anytown"
        };
        
        private string GetSampleState(string size) => size switch
        {
            "Small" => "OH",
            "Medium" => "CA",
            "Large" => "FL",
            _ => "TX"
        };
        
        private string GetSampleZip(string size) => size switch
        {
            "Small" => "45201",
            "Medium" => "92501",
            "Large" => "33139",
            _ => "75001"
        };
    }
}
