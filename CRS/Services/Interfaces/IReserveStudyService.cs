using CRS.Models;
using CRS.Models.Emails;

namespace CRS.Services.Interfaces {
    public interface IReserveStudyService {
        Task<ReserveStudy> CreateReserveStudyAsync(ReserveStudy reserveStudy);
        Task<List<ReserveStudy>> GetAllReserveStudiesAsync();
        Task<bool> DeleteReserveStudyAsync(Guid id);

        ReserveStudyEmail MapToReserveStudyEmail(ReserveStudy reserveStudy, string message);

        Task<ReserveStudy?> GetStudyByTokenAsync(string tokenStr);
        Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId);
        Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId);
    }
}
