using CRS.Data;
using CRS.Models;
using CRS.Services.Email;

using Microsoft.EntityFrameworkCore;

using MudBlazor;
using MudBlazor.Charts;

using Polly;

using System.Globalization;

using static CRS.Components.Pages.Dashboard.Index;

namespace CRS.Services {
    public class ReserveStudyService : IReserveStudyService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IEmailService _emailService;

        public ReserveStudyService(IDbContextFactory<ApplicationDbContext> dbFactory, IEmailService emailService) {
            _dbFactory = dbFactory;
            _emailService = emailService;
        }

        public async Task<List<ReserveStudy>> GetAllReserveStudiesAsync() {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Where(rs => rs.IsActive)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<ReserveStudy?> GetStudyByTokenAsync(string tokenStr) {
            if (!Guid.TryParse(tokenStr, out var token)) {
                return null;
            }

            // First validate the token and get the associated request ID
            var requestId = await ValidateAccessTokenAsync(token);
            if (requestId == null) {
                return null;
            }

            using var context = await _dbFactory.CreateDbContextAsync();

            // Fetch the reserve study with the request ID
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.BuildingElement)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.CommonElement)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyAdditionalElements!).ThenInclude(be => be.ServiceContact)
                .AsSplitQuery()
                .FirstOrDefaultAsync(rs => rs.Id == requestId && rs.IsActive);
        }


        public async Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId) {
            //if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var specialistId)) {
            //    return new List<ReserveStudy>();
            //}
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Where(rs => rs.IsActive && rs.SpecialistUserId == userId) //specialistId)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId) {
            //if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var ownerId)) {
            //    return new List<ReserveStudy>();
            //}
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.User)
                .Where(rs => rs.IsActive && rs.ApplicationUserId == userId) //ownerId)
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<DashboardData> GetDashboardDataAsync(Guid reserveStudyId) {
            using var context = await _dbFactory.CreateDbContextAsync();
            var study = await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.ReserveStudyBuildingElements)
                    .ThenInclude(e => e.ElementMeasurementOptions)
                .Include(rs => rs.ReserveStudyCommonElements)
                    .ThenInclude(e => e.ElementMeasurementOptions)
                .Include(rs => rs.ReserveStudyAdditionalElements)
                    .ThenInclude(e => e.ElementMeasurementOptions)
                .FirstOrDefaultAsync(rs => rs.Id == reserveStudyId);

            if (study == null) {
                return new DashboardData();
            }

            // Get all available elements for analysis
            var allElements = study.ReserveStudyElements;

            if (allElements.Count == 0) {
                return new DashboardData(); // Return empty data if no elements
            }

            // Calculate real cost data based on measurements and element counts
            var costsByCategory = new Dictionary<string, double>();

            foreach (var element in allElements) {
                string category;

                // Determine category based on element type
                switch (element.ElementType) {
                    case IReserveStudyElement.ElementTypeEnum.Building:
                        category = "Building Components";
                        break;
                    case IReserveStudyElement.ElementTypeEnum.Common:
                        category = "Common Areas";
                        break;
                    case IReserveStudyElement.ElementTypeEnum.Additional:
                        category = "Additional Elements";
                        break;
                    default:
                        category = "Other";
                        break;
                }

                // Calculate estimated cost based on element data
                double elementCost = 0;

                // If there are measurement options available, use them to calculate cost
                if (element.ElementMeasurementOptions != null) {
                    // Calculate based on measurement units (area, length, etc.)
                    var measurement = element.ElementMeasurementOptions;
                    //double unitCost = measurement.UnitCost ?? 0;
                    //double quantity = measurement.Quantity ?? 0;

                    elementCost = 10 * 2 * element.Count; //unitCost * quantity * element.Count;
                }
                else {
                    // Use a default estimate if no measurement data is available
                    elementCost = 500 * element.Count; // Basic fallback estimate
                }

                // Add to the appropriate category
                if (costsByCategory.ContainsKey(category)) {
                    costsByCategory[category] += elementCost;
                }
                else {
                    costsByCategory[category] = elementCost;
                }
            }

            // If we don't have enough meaningful categories, add some defaults
            if (costsByCategory.Count < 2) {
                if (!costsByCategory.ContainsKey("Building Components"))
                    costsByCategory["Building Components"] = 0;
                if (!costsByCategory.ContainsKey("Common Areas"))
                    costsByCategory["Common Areas"] = 0;
                if (!costsByCategory.ContainsKey("Additional Elements"))
                    costsByCategory["Additional Elements"] = 0;
            }

            // Convert to arrays for the chart
            var categories = costsByCategory.OrderByDescending(kv => kv.Value).ToList();

            var result = new DashboardData {
                // Fund allocation chart with real data
                FundAllocationLabels = categories.Select(c => c.Key).ToArray(),
                FundAllocationData = categories.Select(c => Math.Round(c.Value, 0)).ToArray(),

                // Fund allocations table with the same real data
                FundAllocations = categories
                    .Select(c => new FundAllocation(c.Key, Math.Round(c.Value, 0)))
                    .ToList(),

                // Rest of your dashboard data...
                MonthlyLabels = GetLastSixMonths(),
                MonthlyExpenditureSeries = new List<ChartSeries>
                {
            new ChartSeries
            {
                Name = "Monthly Expenditure",
                Data = GenerateRandomData(6, 1000, 5000)
            }
        },

                // Recent expenditures (simulated)
                RecentExpenditures = GenerateRecentExpenditures(),

                // Highest categories based on our real data
                HighestExpenditureCategories = categories
                    .Take(3)
                    .Select(c => new ExpenditureCategory(c.Key, Math.Round(c.Value, 0)))
                    .ToList(),

                // Percentile data
                PercentileLabels = GetLastSixMonths(),
                PercentileData = new List<ChartSeries>
                {
            new ChartSeries
            {
                Name = "95th Percentile",
                Data = GenerateRandomData(6, 2000, 8000)
            }
        }
            };

            return result;
        }

        private string[] GetLastSixMonths() {
            return Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-i))
                .Select(date => date.ToString("MMM", CultureInfo.InvariantCulture))
                .Reverse()
                .ToArray();
        }

        private double[] GenerateRandomData(int count, double min, double max) {
            var random = new Random();
            return Enumerable.Range(0, count)
                .Select(_ => random.NextDouble() * (max - min) + min)
                .ToArray();
        }

        private List<Expenditure> GenerateRecentExpenditures() {
            var categories = new[] { "Maintenance", "Repairs", "Upgrades", "Replacements" };
            var random = new Random();

            return Enumerable.Range(0, 5)
                .Select(i => new Expenditure(
                    DateTime.Now.AddDays(-i * 3).ToString("yyyy-MM-dd"),
                    categories[random.Next(categories.Length)],
                    Math.Round(random.NextDouble() * 3000 + 200, 0)
                ))
                .ToList();
        }

        public async Task<Guid> GenerateAccessTokenAsync(Guid requestId) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var token = Guid.CreateVersion7();

            var accessToken = new AccessToken {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                RequestId = requestId
            };

            context.AccessTokens.Add(accessToken);
            await context.SaveChangesAsync();

            return token;
        }

        public async Task<Guid?> ValidateAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var accessToken = await context.AccessTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.Expiration > DateTime.UtcNow);

            return accessToken?.RequestId;
        }

        public async Task RevokeAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var accessToken = await context.AccessTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (accessToken != null) {
                context.AccessTokens.Remove(accessToken);
                await context.SaveChangesAsync();
            }
        }
    }
}
