using CRS.Data;
using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface ICommunityService {
        Task<bool> DeleteCommunityAsync(Guid id);
        Task<List<Community>> GetAllCommunitiesAsync();
        Task<List<Community>> GetAssignedCommunitiesAsync(Guid userId);
        Task<List<Community>> GetOwnedCommunitiesAsync(Guid userId);
        Task<List<Community>> GetUserCommunitiesAsync(ApplicationUser currentUser, IList<string> userRoles);
        IEnumerable<Community> FilterCommunities(IEnumerable<Community> communities, string searchString);
    }
}
