using Horizon.Models;

namespace Horizon.Services.Customers {
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

        /// <summary>
        /// Gets a customer account by the associated user ID (checks both legacy UserId and CustomerAccountUsers)
        /// </summary>
        Task<CustomerAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Gets all customer accounts a user belongs to
        /// </summary>
        Task<List<CustomerAccountMembership>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Links a community to a customer account
        /// </summary>
        Task<bool> LinkCommunityAsync(Guid customerId, Guid communityId, CancellationToken ct = default);

        /// <summary>
        /// Sends a welcome email to a newly registered customer
        /// </summary>
        Task SendWelcomeEmailAsync(CustomerAccount customer, CancellationToken ct = default);

        // ═══════════════════════════════════════════════════════════════
        // TEAM MEMBER MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Gets all team members for a customer account
        /// </summary>
        Task<List<CustomerAccountUser>> GetTeamMembersAsync(Guid customerId, CancellationToken ct = default);

        /// <summary>
        /// Adds a user to a customer account team
        /// </summary>
        Task<CustomerAccountUser> AddTeamMemberAsync(Guid customerId, Guid userId, CustomerAccountRole role, Guid? invitedByUserId = null, CancellationToken ct = default);

        /// <summary>
        /// Removes a user from a customer account team
        /// </summary>
        Task<bool> RemoveTeamMemberAsync(Guid customerId, Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Updates a team member's role
        /// </summary>
        Task<bool> UpdateTeamMemberRoleAsync(Guid customerId, Guid userId, CustomerAccountRole newRole, CancellationToken ct = default);

        /// <summary>
        /// Checks if a user has a specific role or higher on a customer account
        /// </summary>
        Task<bool> HasRoleAsync(Guid customerId, Guid userId, CustomerAccountRole minimumRole, CancellationToken ct = default);

        // ═══════════════════════════════════════════════════════════════
        // INVITATION MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// Creates an invitation for a user to join a customer account
        /// </summary>
        Task<CustomerAccountInvitation> CreateInvitationAsync(Guid customerId, string email, CustomerAccountRole role, Guid invitedByUserId, CancellationToken ct = default);

        /// <summary>
        /// Gets pending invitations for a customer account
        /// </summary>
        Task<List<CustomerAccountInvitation>> GetPendingInvitationsAsync(Guid customerId, CancellationToken ct = default);

        /// <summary>
        /// Accepts an invitation using the token
        /// </summary>
        Task<CustomerAccountUser?> AcceptInvitationAsync(string token, Guid userId, CancellationToken ct = default);

        /// <summary>
        /// Revokes a pending invitation
        /// </summary>
        Task<bool> RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default);

        /// <summary>
        /// Gets an invitation by token
        /// </summary>
        Task<CustomerAccountInvitation?> GetInvitationByTokenAsync(string token, CancellationToken ct = default);

        /// <summary>
        /// Sends an invitation email
        /// </summary>
        Task SendInvitationEmailAsync(CustomerAccountInvitation invitation, CancellationToken ct = default);
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

    /// <summary>
    /// Represents a user's membership in a customer account
    /// </summary>
    public record CustomerAccountMembership(
        CustomerAccount Customer,
        CustomerAccountRole Role,
        DateTime JoinedAt
    );
}