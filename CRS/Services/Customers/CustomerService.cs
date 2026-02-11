using Coravel.Mailer.Mail.Interfaces;
using CRS.Data;
using CRS.Models;
using CRS.Models.Email;
using CRS.Models.Emails;
using CRS.Services.Tenant;
using Microsoft.EntityFrameworkCore;

namespace CRS.Services.Customers {
    public class CustomerService : ICustomerService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ITenantContext _tenant;
        private readonly ILogger<CustomerService> _logger;
        private readonly IMailer _mailer;

        public CustomerService(
            IDbContextFactory<ApplicationDbContext> dbFactory, 
            ITenantContext tenant,
            ILogger<CustomerService> logger,
            IMailer mailer) 
        { 
            _dbFactory = dbFactory; 
            _tenant = tenant;
            _logger = logger;
            _mailer = mailer;
        }

        public async Task<int> GetActiveCustomerCountAsync(CancellationToken ct = default) {
            if (!_tenant.TenantId.HasValue) return 0;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.CustomerAccounts.CountAsync(c => c.TenantId == _tenant.TenantId && c.IsActive, ct);
        }

        public async Task<List<CustomerAccount>> GetAllAsync(bool includeInactive = false, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var query = db.CustomerAccounts
                .Where(c => c.TenantId == _tenant.TenantId);

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<CustomerAccount?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return null;
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CustomerAccounts
                .Include(c => c.Communities)
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenant.TenantId, ct);
        }

        public async Task<CustomerAccount> CreateAsync(CustomerAccount account, CancellationToken ct = default) 
        {
            if (!_tenant.TenantId.HasValue) 
                throw new InvalidOperationException("Tenant context required");

            account.TenantId = _tenant.TenantId.Value;
            account.CreatedAt = DateTime.UtcNow;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            db.CustomerAccounts.Add(account);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Created customer {CustomerId} ({Name}) for tenant {TenantId}", 
                account.Id, account.Name, account.TenantId);

            return account;
        }

        public async Task<CustomerAccount?> UpdateAsync(CustomerAccount account, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return null;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var existing = await db.CustomerAccounts
                .FirstOrDefaultAsync(c => c.Id == account.Id && c.TenantId == _tenant.TenantId, ct);

            if (existing == null) return null;

            existing.Name = account.Name;
            existing.Type = account.Type;
            existing.ContactName = account.ContactName;
            existing.Email = account.Email;
            existing.Phone = account.Phone;
            existing.AddressLine1 = account.AddressLine1;
            existing.AddressLine2 = account.AddressLine2;
            existing.City = account.City;
            existing.State = account.State;
            existing.PostalCode = account.PostalCode;
            existing.Notes = account.Notes;
            existing.Website = account.Website;
            existing.TaxId = account.TaxId;
            existing.PaymentTerms = account.PaymentTerms;
            existing.IsActive = account.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Updated customer {CustomerId} ({Name})", existing.Id, existing.Name);

            return existing;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var customer = await db.CustomerAccounts
                .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == _tenant.TenantId, ct);

            if (customer == null) return false;

            // Soft delete
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Soft-deleted customer {CustomerId} ({Name})", customer.Id, customer.Name);

            return true;
        }

        public async Task<List<CustomerWithStats>> GetCustomersWithStatsAsync(CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var customers = await db.CustomerAccounts
                .Where(c => c.TenantId == _tenant.TenantId && c.IsActive)
                .Include(c => c.Communities)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            var result = new List<CustomerWithStats>();

            foreach (var customer in customers)
            {
                // Get directly linked communities (via CustomerAccountId)
                var directCommunityIds = customer.Communities
                    .Where(c => c.IsActive)
                    .Select(c => c.Id)
                    .ToList();

                // Also get communities linked via reserve studies (legacy association)
                var studyCommunityIds = new List<Guid>();
                if (customer.UserId.HasValue)
                {
                    studyCommunityIds = await db.ReserveStudies
                        .Where(rs => rs.TenantId == _tenant.TenantId &&
                                    rs.CommunityId.HasValue &&
                                    (rs.RequestedByUserId == customer.UserId || rs.ApplicationUserId == customer.UserId))
                        .Select(rs => rs.CommunityId!.Value)
                        .Distinct()
                        .ToListAsync(ct);
                }

                // Combine both sources
                var allCommunityIds = directCommunityIds
                    .Union(studyCommunityIds)
                    .Distinct()
                    .ToList();

                var activeStudies = allCommunityIds.Count > 0 
                    ? await db.ReserveStudies
                        .CountAsync(rs => rs.CommunityId.HasValue && allCommunityIds.Contains(rs.CommunityId.Value) && rs.IsActive && !rs.IsComplete, ct)
                    : 0;

                // Calculate total revenue from paid invoices
                var revenue = allCommunityIds.Count > 0
                    ? await db.Invoices
                        .Where(i => i.ReserveStudy != null && 
                                   i.ReserveStudy.CommunityId.HasValue && allCommunityIds.Contains(i.ReserveStudy.CommunityId.Value) &&
                                   i.Status == InvoiceStatus.Paid)
                        .SumAsync(i => i.TotalAmount, ct)
                    : 0m;

                result.Add(new CustomerWithStats(
                    Customer: customer,
                    CommunityCount: allCommunityIds.Count,
                    ActiveStudyCount: activeStudies,
                    TotalRevenue: revenue
                ));
            }

            return result;
        }

        public async Task<List<CustomerAccount>> SearchAsync(string query, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue || string.IsNullOrWhiteSpace(query)) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var searchTerm = query.ToLower();

            return await db.CustomerAccounts
                .Where(c => c.TenantId == _tenant.TenantId && c.IsActive)
                .Where(c => c.Name.ToLower().Contains(searchTerm) ||
                           (c.Email != null && c.Email.ToLower().Contains(searchTerm)) ||
                           (c.ContactName != null && c.ContactName.ToLower().Contains(searchTerm)))
                .OrderBy(c => c.Name)
                .Take(20)
                .ToListAsync(ct);
        }

        public async Task<CustomerAccount?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return null;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // First check new multi-user relationship
            var membershipCustomerId = await db.CustomerAccountUsers
                .Where(cau => cau.UserId == userId && cau.IsActive)
                .Select(cau => cau.CustomerAccountId)
                .FirstOrDefaultAsync(ct);

            if (membershipCustomerId != Guid.Empty)
            {
                return await db.CustomerAccounts
                    .Include(c => c.Communities)
                    .FirstOrDefaultAsync(c => c.Id == membershipCustomerId && c.TenantId == _tenant.TenantId && c.IsActive, ct);
            }

            // Fall back to legacy single-user relationship
            return await db.CustomerAccounts
                .Include(c => c.Communities)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.TenantId == _tenant.TenantId && c.IsActive, ct);
        }

        public async Task<bool> LinkCommunityAsync(Guid customerId, Guid communityId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var community = await db.Communities
                .FirstOrDefaultAsync(c => c.Id == communityId && c.TenantId == _tenant.TenantId, ct);

            if (community == null)
            {
                _logger.LogWarning("Community {CommunityId} not found for linking", communityId);
                return false;
            }

            // Check if already linked to another customer
            if (community.CustomerAccountId.HasValue && community.CustomerAccountId != customerId)
            {
                _logger.LogWarning("Community {CommunityId} already linked to customer {ExistingCustomerId}", 
                    communityId, community.CustomerAccountId);
                return false;
            }

            community.CustomerAccountId = customerId;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Linked community {CommunityId} to customer {CustomerId}", communityId, customerId);
            return true;
        }

        public async Task SendWelcomeEmailAsync(CustomerAccount customer, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(customer.Email))
            {
                _logger.LogWarning("Cannot send welcome email - customer {CustomerId} has no email", customer.Id);
                return;
            }

            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);

                // Get tenant info for branding
                var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == customer.TenantId, ct);
                var tenantName = tenant?.Name ?? "Reserve Study Services";
                var subdomain = tenant?.Subdomain ?? "";

                // Build URLs
                var baseUrl = !string.IsNullOrEmpty(subdomain) 
                    ? $"https://{subdomain}.alxreservecloud.com" 
                    : "https://alxreservecloud.com";

                var emailModel = new CustomerWelcomeEmail
                {
                    CustomerName = customer.Name,
                    ContactName = customer.ContactName ?? customer.Name,
                    Email = customer.Email,
                    TenantName = tenantName,
                    LoginUrl = $"{baseUrl}/Account/Login",
                    RequestStudyUrl = $"{baseUrl}/ReserveStudies/Request",
                    SupportEmail = tenant?.DefaultNotificationEmail,
                    SupportPhone = null // Tenant doesn't have a phone field
                };

                var fromEmail = tenant?.DefaultNotificationEmail ?? "no-reply@alxreservecloud.com";
                var mailable = new CustomerWelcomeMailable(emailModel, customer.Email, fromEmail);

                await _mailer.SendAsync(mailable);

                _logger.LogInformation("Sent welcome email to customer {CustomerId} ({Email})", 
                    customer.Id, customer.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send welcome email to customer {CustomerId}", customer.Id);
                // Don't throw - welcome email failure shouldn't block registration
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // MULTI-USER / TEAM MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        public async Task<List<CustomerAccountMembership>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Check new multi-user relationship
            var memberships = await db.CustomerAccountUsers
                .Include(cau => cau.CustomerAccount)
                .ThenInclude(ca => ca.Communities)
                .Where(cau => cau.UserId == userId && cau.IsActive && 
                             cau.CustomerAccount.TenantId == _tenant.TenantId && cau.CustomerAccount.IsActive)
                .Select(cau => new CustomerAccountMembership(cau.CustomerAccount, cau.Role, cau.JoinedAt))
                .ToListAsync(ct);

            // Also check legacy single-user relationship for backwards compatibility
            var legacyCustomer = await db.CustomerAccounts
                .Include(c => c.Communities)
                .Where(c => c.UserId == userId && c.TenantId == _tenant.TenantId && c.IsActive)
                .FirstOrDefaultAsync(ct);

            if (legacyCustomer != null && !memberships.Any(m => m.Customer.Id == legacyCustomer.Id))
            {
                memberships.Add(new CustomerAccountMembership(legacyCustomer, CustomerAccountRole.Owner, legacyCustomer.CreatedAt));
            }

            return memberships;
        }

        public async Task<List<CustomerAccountUser>> GetTeamMembersAsync(Guid customerId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CustomerAccountUsers
                .Include(cau => cau.User)
                .Where(cau => cau.CustomerAccountId == customerId && cau.IsActive)
                .OrderBy(cau => cau.Role)
                .ThenBy(cau => cau.JoinedAt)
                .ToListAsync(ct);
        }

        public async Task<CustomerAccountUser> AddTeamMemberAsync(Guid customerId, Guid userId, CustomerAccountRole role, Guid? invitedByUserId = null, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context required");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Verify customer exists and belongs to tenant
            var customer = await db.CustomerAccounts
                .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == _tenant.TenantId, ct);

            if (customer == null)
                throw new InvalidOperationException("Customer account not found");

            // Check if user is already a member
            var existing = await db.CustomerAccountUsers
                .FirstOrDefaultAsync(cau => cau.CustomerAccountId == customerId && cau.UserId == userId, ct);

            if (existing != null)
            {
                if (existing.IsActive)
                    throw new InvalidOperationException("User is already a team member");

                // Reactivate existing membership
                existing.IsActive = true;
                existing.Role = role;
                existing.JoinedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Reactivated team member {UserId} on customer {CustomerId} with role {Role}",
                    userId, customerId, role);

                return existing;
            }

            var teamMember = new CustomerAccountUser
            {
                CustomerAccountId = customerId,
                UserId = userId,
                Role = role,
                InvitedByUserId = invitedByUserId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.CustomerAccountUsers.Add(teamMember);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Added team member {UserId} to customer {CustomerId} with role {Role}",
                userId, customerId, role);

            return teamMember;
        }

        public async Task<bool> RemoveTeamMemberAsync(Guid customerId, Guid userId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var member = await db.CustomerAccountUsers
                .FirstOrDefaultAsync(cau => cau.CustomerAccountId == customerId && cau.UserId == userId && cau.IsActive, ct);

            if (member == null) return false;

            // Prevent removing the last owner
            if (member.Role == CustomerAccountRole.Owner)
            {
                var ownerCount = await db.CustomerAccountUsers
                    .CountAsync(cau => cau.CustomerAccountId == customerId && 
                                      cau.Role == CustomerAccountRole.Owner && cau.IsActive, ct);

                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot remove the last owner from the account");
            }

            member.IsActive = false;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Removed team member {UserId} from customer {CustomerId}", userId, customerId);

            return true;
        }

        public async Task<bool> UpdateTeamMemberRoleAsync(Guid customerId, Guid userId, CustomerAccountRole newRole, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var member = await db.CustomerAccountUsers
                .FirstOrDefaultAsync(cau => cau.CustomerAccountId == customerId && cau.UserId == userId && cau.IsActive, ct);

            if (member == null) return false;

            // Prevent demoting the last owner
            if (member.Role == CustomerAccountRole.Owner && newRole != CustomerAccountRole.Owner)
            {
                var ownerCount = await db.CustomerAccountUsers
                    .CountAsync(cau => cau.CustomerAccountId == customerId && 
                                      cau.Role == CustomerAccountRole.Owner && cau.IsActive, ct);

                if (ownerCount <= 1)
                    throw new InvalidOperationException("Cannot demote the last owner");
            }

            member.Role = newRole;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Updated team member {UserId} role to {Role} on customer {CustomerId}",
                userId, newRole, customerId);

            return true;
        }

        public async Task<bool> HasRoleAsync(Guid customerId, Guid userId, CustomerAccountRole minimumRole, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Check new multi-user relationship
            var membership = await db.CustomerAccountUsers
                .FirstOrDefaultAsync(cau => cau.CustomerAccountId == customerId && 
                                           cau.UserId == userId && cau.IsActive, ct);

            if (membership != null)
            {
                return membership.Role <= minimumRole; // Lower enum value = higher permission
            }

            // Check legacy relationship (treat as Owner)
            var isLegacyOwner = await db.CustomerAccounts
                .AnyAsync(c => c.Id == customerId && c.UserId == userId && c.IsActive, ct);

            return isLegacyOwner && minimumRole >= CustomerAccountRole.Owner;
        }

        // ═══════════════════════════════════════════════════════════════
        // INVITATION MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        public async Task<CustomerAccountInvitation> CreateInvitationAsync(Guid customerId, string email, CustomerAccountRole role, Guid invitedByUserId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue)
                throw new InvalidOperationException("Tenant context required");

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Verify customer exists
            var customer = await db.CustomerAccounts
                .FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == _tenant.TenantId, ct);

            if (customer == null)
                throw new InvalidOperationException("Customer account not found");

            // Check for existing pending invitation
            var existingInvite = await db.CustomerAccountInvitations
                .FirstOrDefaultAsync(i => i.CustomerAccountId == customerId && 
                                         i.Email.ToLower() == email.ToLower() && 
                                         i.Status == InvitationStatus.Pending, ct);

            if (existingInvite != null)
            {
                // Update existing invitation
                existingInvite.Role = role;
                existingInvite.CreatedAt = DateTime.UtcNow;
                existingInvite.ExpiresAt = DateTime.UtcNow.AddDays(7);
                existingInvite.Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                existingInvite.InvitedByUserId = invitedByUserId;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation("Updated existing invitation for {Email} to customer {CustomerId}", 
                    email, customerId);

                return existingInvite;
            }

            var invitation = new CustomerAccountInvitation
            {
                CustomerAccountId = customerId,
                Email = email,
                Role = role,
                InvitedByUserId = invitedByUserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Status = InvitationStatus.Pending
            };

            db.CustomerAccountInvitations.Add(invitation);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Created invitation for {Email} to customer {CustomerId} with role {Role}",
                email, customerId, role);

            return invitation;
        }

        public async Task<List<CustomerAccountInvitation>> GetPendingInvitationsAsync(Guid customerId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return [];

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            return await db.CustomerAccountInvitations
                .Where(i => i.CustomerAccountId == customerId && 
                           i.Status == InvitationStatus.Pending &&
                           i.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<CustomerAccountInvitation?> GetInvitationByTokenAsync(string token, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Use IgnoreQueryFilters to bypass tenant filter on CustomerAccount
            // Invitations are looked up by token across tenant boundaries
            return await db.CustomerAccountInvitations
                .Include(i => i.CustomerAccount)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Token == token, ct);
        }

        public async Task<CustomerAccountUser?> AcceptInvitationAsync(string token, Guid userId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            // Use IgnoreQueryFilters to bypass tenant filter - invitations work across tenant boundaries
            var invitation = await db.CustomerAccountInvitations
                .Include(i => i.CustomerAccount)
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.Token == token, ct);

            if (invitation == null)
            {
                _logger.LogWarning("Invitation not found for token");
                return null;
            }

            if (invitation.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning("Invitation {InvitationId} is not pending (status: {Status})", 
                    invitation.Id, invitation.Status);
                return null;
            }

            if (invitation.ExpiresAt < DateTime.UtcNow)
            {
                invitation.Status = InvitationStatus.Expired;
                await db.SaveChangesAsync(ct);
                _logger.LogWarning("Invitation {InvitationId} has expired", invitation.Id);
                return null;
            }

            // Add user to the team
            var teamMember = new CustomerAccountUser
            {
                CustomerAccountId = invitation.CustomerAccountId,
                UserId = userId,
                Role = invitation.Role,
                InvitedByUserId = invitation.InvitedByUserId,
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };

            db.CustomerAccountUsers.Add(teamMember);

            // Update invitation status
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.AcceptedByUserId = userId;

            await db.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} accepted invitation {InvitationId} to customer {CustomerId}",
                userId, invitation.Id, invitation.CustomerAccountId);

            return teamMember;
        }

        public async Task<bool> RevokeInvitationAsync(Guid invitationId, CancellationToken ct = default)
        {
            if (!_tenant.TenantId.HasValue) return false;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var invitation = await db.CustomerAccountInvitations
                .Include(i => i.CustomerAccount)
                .FirstOrDefaultAsync(i => i.Id == invitationId && 
                                         i.CustomerAccount.TenantId == _tenant.TenantId, ct);

            if (invitation == null || invitation.Status != InvitationStatus.Pending)
                return false;

            invitation.Status = InvitationStatus.Revoked;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Revoked invitation {InvitationId}", invitationId);

            return true;
        }

        public async Task SendInvitationEmailAsync(CustomerAccountInvitation invitation, CancellationToken ct = default)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);

                // Get customer and tenant info - use IgnoreQueryFilters to ensure we can
                // find the customer regardless of the current tenant context, since invitations
                // may be sent/resent in various contexts
                var customer = await db.CustomerAccounts
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == invitation.CustomerAccountId, ct);

                if (customer == null) return;

                var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == customer.TenantId, ct);
                var tenantName = tenant?.Name ?? "Reserve Study Services";
                var subdomain = tenant?.Subdomain ?? "";

                // Get inviter name
                var inviter = await db.Users.FirstOrDefaultAsync(u => u.Id == invitation.InvitedByUserId, ct);
                var inviterName = inviter != null ? $"{inviter.FirstName} {inviter.LastName}" : customer.Name;

                // Build accept URL
                var baseUrl = !string.IsNullOrEmpty(subdomain)
                    ? $"https://{subdomain}.alxreservecloud.com"
                    : "https://alxreservecloud.com";
                var acceptUrl = $"{baseUrl}/Account/AcceptInvitation?token={Uri.EscapeDataString(invitation.Token)}";

                var emailModel = new TeamInvitationEmail
                {
                    CustomerName = customer.Name,
                    InviterName = inviterName,
                    Email = invitation.Email,
                    Role = GetRoleName(invitation.Role),
                    TenantName = tenantName,
                    AcceptUrl = acceptUrl,
                    ExpiresAt = invitation.ExpiresAt
                };

                var fromEmail = tenant?.DefaultNotificationEmail ?? "DoNotReply@4b9bbf9f-0f50-4984-9cf1-a70b8e8b1f32.azurecomm.net";
                var mailable = new TeamInvitationMailable(emailModel, invitation.Email, fromEmail);

                await _mailer.SendAsync(mailable);

                _logger.LogInformation("Sent invitation email to {Email} for customer {CustomerId}",
                    invitation.Email, invitation.CustomerAccountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {Email}", invitation.Email);
                // Don't throw - email failure shouldn't block invitation creation
            }
        }

        private static string GetRoleName(CustomerAccountRole role) => role switch
        {
            CustomerAccountRole.Owner => "Owner",
            CustomerAccountRole.Admin => "Admin",
            CustomerAccountRole.Member => "Member",
            CustomerAccountRole.Viewer => "Viewer",
            _ => role.ToString()
        };
    }
}