namespace CRS.Services.Interfaces {
    public interface IDashboardService {
        Task<DashboardData> GetDashboardDataAsync(Guid reserveStudyId);
    }
}
