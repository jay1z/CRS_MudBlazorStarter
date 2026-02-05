using CRS.Models;

namespace CRS.Services.Customers {
    public interface ICustomerService {
        /// <summary>
        /// Gets the count of active customers for the current tenant
        /// </summary>
        Task<int> GetActiveCustomerCountAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets all customers for the current tenant
        /// </summary>
        Task<List<CustomerAccount>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default);

        /// <summary>
        /// Gets a customer by ID
        /// </summary>
        Task<CustomerAccount?> GetByIdAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Creates a new customer
        /// </summary>
        Task<CustomerAccount> CreateAsync(CustomerAccount account, CancellationToken ct = default);

        /// <summary>
        /// Updates an existing customer
        /// </summary>
        Task<CustomerAccount?> UpdateAsync(CustomerAccount account, CancellationToken ct = default);

        /// <summary>
        /// Soft deletes a customer (sets IsActive = false)
        /// </summary>
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        /// <summary>
        /// Gets customers with their community counts
        /// </summary>
        Task<List<CustomerWithStats>> GetCustomersWithStatsAsync(CancellationToken ct = default);

        /// <summary>
        /// Searches customers by name or email
        /// </summary>
        Task<List<CustomerAccount>> SearchAsync(string query, CancellationToken ct = default);
    }

    /// <summary>
    /// Customer with aggregate statistics
    /// </summary>
    public record CustomerWithStats(
        CustomerAccount Customer,
        int CommunityCount,
        int ActiveStudyCount,
        decimal TotalRevenue
    );
}