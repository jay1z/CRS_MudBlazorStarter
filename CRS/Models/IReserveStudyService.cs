using MudBlazor;
using static CRS.Components.Pages.Dashboard.Index;

namespace CRS.Models {
    public interface IReserveStudyService {
        Task<List<ReserveStudy>> GetAllReserveStudiesAsync();
        Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId);
        Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId);
        Task<DashboardData> GetDashboardDataAsync(Guid reserveStudyId);
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
