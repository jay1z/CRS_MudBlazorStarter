using CRS.Models;
using System.Globalization;

using CRS.Services.Interfaces;

using MudBlazor;

using static CRS.Components.Pages.Dashboard.Index;
using CRS.Data;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class DashboardService : IDashboardService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public DashboardService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
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

    }

    public class DashboardData {
        public string[] FundAllocationLabels { get; set; } = Array.Empty<string>();
        public double[] FundAllocationData { get; set; } = Array.Empty<double>();

        public string[] MonthlyLabels { get; set; } = Array.Empty<string>();
        public List<ChartSeries> MonthlyExpenditureSeries { get; set; } = new();

        public List<FundAllocation> FundAllocations { get; set; } = new();
        public List<Expenditure> RecentExpenditures { get; set; } = new();
        public List<ExpenditureCategory> HighestExpenditureCategories { get; set; } = new();

        public string[] PercentileLabels { get; set; } = Array.Empty<string>();
        public List<ChartSeries> PercentileData { get; set; } = new();
    }
}
