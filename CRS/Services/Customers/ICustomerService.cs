using CRS.Models;

namespace CRS.Services.Customers {
    public interface ICustomerService {
        Task<int> GetActiveCustomerCountAsync(CancellationToken ct = default);
        Task<CustomerAccount> CreateCustomerAsync(CustomerAccount account, CancellationToken ct = default);
    }
}