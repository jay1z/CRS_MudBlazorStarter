namespace CRS.Services.Interfaces {
    public interface ICommunityService {
        Task<bool> DeleteCommunityAsync(Guid id);
    }
}
