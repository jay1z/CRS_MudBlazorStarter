using CRS.Data;
using CRS.Models;
using CRS.Models.Emails;

namespace CRS.Services.Interfaces {
    public interface IReserveStudyService {
        Task<ReserveStudy> CreateReserveStudyAsync(ReserveStudy reserveStudy);
        Task<ReserveStudy> UpdateReserveStudyAsync(ReserveStudy reserveStudy);
        Task<bool> DeleteReserveStudyAsync(Guid id);
        Task<ReserveStudy?> GetReserveStudyByIdAsync(Guid studyId);
        Task<List<ReserveStudy>> GetAllReserveStudiesAsync();
        Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId);
        Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId);
        Task<List<ReserveStudy>> GetUserReserveStudiesAsync(ApplicationUser currentUser, IList<string> userRoles);
        IEnumerable<ReserveStudy> FilterReserveStudies(IEnumerable<ReserveStudy> reserveStudies, string searchString);
        Task<ReserveStudyEmail> MapToReserveStudyEmailAsync(ReserveStudy reserveStudy, string message);
        Task<TenantEmailInfo> GetTenantEmailInfoAsync(int tenantId);
        Task<ReserveStudy?> GetStudyByTokenAsync(string tokenStr);
        Task<Guid> GenerateAccessTokenAsync(Guid requestId);
        Task<Guid?> ValidateAccessTokenAsync(Guid token);
        Task RevokeAccessTokenAsync(Guid token);
        bool CanUpdateReserveStudy(ReserveStudy reserveStudy);
        bool CanStaffUpdateReserveStudy(ReserveStudy reserveStudy);
    }
}

