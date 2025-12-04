using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using CRS.Services.Billing; // feature guard

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class ReserveStudyService : IReserveStudyService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IDispatcher _dispatcher;
        private readonly string _baseUrl;
        private readonly ITenantContext _tenantContext;
        private readonly IFeatureGuardService _featureGuard;

        public ReserveStudyService(IDbContextFactory<ApplicationDbContext> dbFactory, IMailer mailer, IDispatcher dispatcher, IConfiguration configuration, ITenantContext tenantContext, IFeatureGuardService featureGuard) {
            _dbFactory = dbFactory;
            _dispatcher = dispatcher;
            _baseUrl = configuration["Application:BaseUrl"] ?? "https://yourdomain.com";
            _tenantContext = tenantContext;
            _featureGuard = featureGuard;
        }

        public async Task<ReserveStudy> CreateReserveStudyAsync(ReserveStudy reserveStudy) {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // SECURITY: Require tenant context
            if (!_tenantContext.TenantId.HasValue) {
                throw new InvalidOperationException("Tenant context is required to create a reserve study.");
            }

            var tenantId = _tenantContext.TenantId.Value;
            
            // SECURITY: Force tenant ID assignment - never trust client input
            reserveStudy.TenantId = tenantId;

            // SECURITY: Validate existing community belongs to current tenant
            if (reserveStudy.CommunityId != Guid.Empty) {
                var existingCommunity = await context.Communities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == reserveStudy.CommunityId);

                if (existingCommunity == null) {
                    // Defensive: if a Community object is provided, create it instead of failing
                    if (reserveStudy.Community != null) {
                        // Ensure tenant and ids
                        if (reserveStudy.Community.TenantId != tenantId) {
                            reserveStudy.Community.TenantId = tenantId;
                        }
                        if (reserveStudy.Community.Id == Guid.Empty) {
                            // use provided reference if any (handle nullable Guid?)
                            reserveStudy.Community.Id = reserveStudy.CommunityId.GetValueOrDefault();
                            if (reserveStudy.Community.Id == Guid.Empty) {
                                reserveStudy.Community.Id = Guid.CreateVersion7();
                            }
                        }

                        // Ensure address ids
                        foreach (var addr in reserveStudy.Community.Addresses ?? Enumerable.Empty<Address>()) {
                            if (addr.Id == Guid.Empty) addr.Id = Guid.CreateVersion7();
                        }

                        await context.Communities.AddAsync(reserveStudy.Community);
                        await context.SaveChangesAsync();

                        // After creation, ensure CommunityId is aligned
                        reserveStudy.CommunityId = reserveStudy.Community.Id;
                    }
                    else {
                        throw new InvalidOperationException("The specified community does not exist.");
                    }
                }

                if (existingCommunity != null && existingCommunity.TenantId != tenantId) {
                    throw new UnauthorizedAccessException("You cannot create a reserve study for a community that belongs to a different organization.");
                }
            }

            // Guard: if creating a new community, enforce limits and validate tenant ID
            var creatingNewCommunity = reserveStudy.CommunityId == Guid.Empty && reserveStudy.Community != null && reserveStudy.Community.Id == Guid.Empty;
            if (creatingNewCommunity) {
                // SECURITY: Force tenant ID on new community
                if (reserveStudy.Community.TenantId != tenantId) {
                    reserveStudy.Community.TenantId = tenantId;
                }
                
                var canAdd = await _featureGuard.CanAddCommunityAsync(tenantId);
                if (!canAdd) throw new InvalidOperationException("Community limit reached or subscription inactive. Upgrade required.");
            }

            reserveStudy.IsActive = true;
            reserveStudy.IsApproved = false;
            reserveStudy.IsComplete = false;

            if (reserveStudy.Contact != null && reserveStudy.ContactId == null) {
                reserveStudy.Contact.Id = Guid.CreateVersion7();
                if (reserveStudy.Contact.TenantId == 0) reserveStudy.Contact.TenantId = tenantId;
                context.Contacts.Add(reserveStudy.Contact);
            } else if (reserveStudy.ContactId != null) {
                reserveStudy.Contact = null;
            }

            if (reserveStudy.PropertyManager != null && reserveStudy.PropertyManagerId == null) {
                reserveStudy.PropertyManager.Id = Guid.CreateVersion7();
                if (reserveStudy.PropertyManager.TenantId == 0) reserveStudy.PropertyManager.TenantId = tenantId;
                context.PropertyManagers.Add(reserveStudy.PropertyManager);
            } else if (reserveStudy.PropertyManagerId != null) {
                reserveStudy.PropertyManager = null;
            }

            context.ReserveStudies.Add(reserveStudy);
            await context.SaveChangesAsync();

            var community = await context.Communities
                .AsNoTracking()
                .Include(c => c.Addresses)
                .FirstOrDefaultAsync(c => c.Id == reserveStudy.CommunityId);
            if (community != null) reserveStudy.Community = community;

            var createdEvent = new ReserveStudyCreatedEvent(reserveStudy);
            await _dispatcher.Broadcast<ReserveStudyCreatedEvent>(createdEvent);
            return reserveStudy;
        }

        public async Task<bool> DeleteReserveStudyAsync(Guid id) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var reserveStudy = await context.ReserveStudies
                .FirstOrDefaultAsync(rs => rs.Id == id);

            if (reserveStudy == null) {
                return false;
            }

            reserveStudy.IsActive = false;
            reserveStudy.DateDeleted = DateTime.UtcNow;

            await context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Updates an existing reserve study. 
        /// HOA users can only update if the proposal has not been accepted (Status < ProposalAccepted).
        /// Staff/specialists can update at any time before completion.
        /// </summary>
        public async Task<ReserveStudy> UpdateReserveStudyAsync(ReserveStudy reserveStudy) {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // SECURITY: Require tenant context
            if (!_tenantContext.TenantId.HasValue) {
                throw new InvalidOperationException("Tenant context is required to update a reserve study.");
            }

            var tenantId = _tenantContext.TenantId.Value;

            // Fetch the existing study with tracking
            var existingStudy = await context.ReserveStudies
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .FirstOrDefaultAsync(rs => rs.Id == reserveStudy.Id && rs.IsActive);

            if (existingStudy == null) {
                throw new InvalidOperationException("Reserve study not found or is no longer active.");
            }

            // SECURITY: Verify tenant ownership
            if (existingStudy.TenantId != tenantId) {
                throw new UnauthorizedAccessException("You cannot update a reserve study that belongs to a different organization.");
            }

            // Check if the study can be updated based on its status
            if (!CanUpdateReserveStudy(existingStudy)) {
                throw new InvalidOperationException("This reserve study can no longer be modified. The proposal has already been accepted.");
            }

            // Update allowed fields
            // Contact information
            if (reserveStudy.Contact != null && existingStudy.Contact != null) {
                existingStudy.Contact.FirstName = reserveStudy.Contact.FirstName;
                existingStudy.Contact.LastName = reserveStudy.Contact.LastName;
                existingStudy.Contact.Email = reserveStudy.Contact.Email;
                existingStudy.Contact.Phone = reserveStudy.Contact.Phone;
                existingStudy.Contact.Extension = reserveStudy.Contact.Extension;
                existingStudy.Contact.CompanyName = reserveStudy.Contact.CompanyName;
            }

            // Property Manager information
            if (reserveStudy.PropertyManager != null && existingStudy.PropertyManager != null) {
                existingStudy.PropertyManager.FirstName = reserveStudy.PropertyManager.FirstName;
                existingStudy.PropertyManager.LastName = reserveStudy.PropertyManager.LastName;
                existingStudy.PropertyManager.Email = reserveStudy.PropertyManager.Email;
                existingStudy.PropertyManager.Phone = reserveStudy.PropertyManager.Phone;
                existingStudy.PropertyManager.Extension = reserveStudy.PropertyManager.Extension;
                existingStudy.PropertyManager.CompanyName = reserveStudy.PropertyManager.CompanyName;
            }

            // Update community information if allowed
            if (reserveStudy.Community != null && existingStudy.Community != null) {
                // SECURITY: Verify the community belongs to the same tenant
                if (existingStudy.Community.TenantId == tenantId) {
                    existingStudy.Community.Name = reserveStudy.Community.Name;
                    
                    // Load existing addresses for proper tracking
                    await context.Entry(existingStudy.Community).Collection(c => c.Addresses).LoadAsync();
                    
                    // Update addresses - handle additions, updates, and deletions
                    if (reserveStudy.Community.Addresses != null) {
                        // Update or add addresses
                        foreach (var updatedAddr in reserveStudy.Community.Addresses) {
                            var existingAddr = existingStudy.Community.Addresses?
                                .FirstOrDefault(a => a.Id != Guid.Empty && a.Id == updatedAddr.Id);
                            
                            if (existingAddr != null) {
                                // Update existing address
                                existingAddr.Street = updatedAddr.Street;
                                existingAddr.City = updatedAddr.City;
                                existingAddr.State = updatedAddr.State;
                                existingAddr.Zip = updatedAddr.Zip;
                                existingAddr.IsMailingAddress = updatedAddr.IsMailingAddress;
                            }
                            else {
                                // Add new address - EF Core will set CommunityId automatically via relationship
                                if (updatedAddr.Id == Guid.Empty) {
                                    updatedAddr.Id = Guid.CreateVersion7();
                                }
                                existingStudy.Community.Addresses ??= new List<Address>();
                                existingStudy.Community.Addresses.Add(updatedAddr);
                                context.Addresses.Add(updatedAddr);
                            }
                        }
                        
                        // Remove addresses that are no longer in the updated collection
                        var updatedAddressIds = reserveStudy.Community.Addresses
                            .Where(a => a.Id != Guid.Empty)
                            .Select(a => a.Id)
                            .ToHashSet();
                        
                        var addressesToRemove = existingStudy.Community.Addresses?
                            .Where(a => !updatedAddressIds.Contains(a.Id))
                            .ToList();
                        
                        if (addressesToRemove != null && addressesToRemove.Any()) {
                            foreach (var addr in addressesToRemove) {
                                existingStudy.Community.Addresses?.Remove(addr);
                                context.Remove(addr);
                            }
                        }
                    }
                }
            }

            existingStudy.LastModified = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return existingStudy;
        }

        /// <summary>
        /// Determines if a reserve study can be updated by checking its workflow status.
        /// Reserve studies can only be updated before the proposal is accepted.
        /// </summary>
        public bool CanUpdateReserveStudy(ReserveStudy reserveStudy) {
            if (reserveStudy == null) {
                return false;
            }

            // Cannot update if the study is complete or cancelled
            if (reserveStudy.IsComplete || 
                reserveStudy.Status == ReserveStudy.WorkflowStatus.RequestCompleted ||
                reserveStudy.Status == ReserveStudy.WorkflowStatus.RequestCancelled ||
                reserveStudy.Status == ReserveStudy.WorkflowStatus.RequestArchived) {
                return false;
            }

            // HOA users can only update before the proposal is accepted
            // Status values 0-5 are before ProposalAccepted (which is 6)
            return (int)reserveStudy.Status < (int)ReserveStudy.WorkflowStatus.ProposalAccepted;
        }

        /// <summary>
        /// Gets all active reserve studies
        /// </summary>
        public async Task<List<ReserveStudy>> GetAllReserveStudiesAsync() {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Where(rs => rs.IsActive)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets reserve studies assigned to a specialist
        /// </summary>
        public async Task<List<ReserveStudy>> GetAssignedReserveStudiesAsync(Guid userId) {
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Where(rs => rs.IsActive && rs.SpecialistUserId == userId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets reserve studies owned by a user
        /// </summary>
        public async Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId) {
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.User)
                .Where(rs => rs.IsActive && rs.ApplicationUserId == userId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets reserve studies for the current user based on their role and permissions
        /// </summary>
        public async Task<List<ReserveStudy>> GetUserReserveStudiesAsync(ApplicationUser currentUser, IList<string> userRoles) {
            if (currentUser == null || userRoles == null) {
                return new List<ReserveStudy>();
            }

            try {
                // Based on role, call the appropriate method
                if (userRoles.Contains("Admin")) {
                    return await GetAllReserveStudiesAsync();
                }
                else if (userRoles.Contains("Specialist")) {
                    return await GetAssignedReserveStudiesAsync(currentUser.Id);
                }
                else if (userRoles.Contains("User")) {
                    return await GetOwnedReserveStudiesAsync(currentUser.Id);
                }
                else {
                    return new List<ReserveStudy>();
                }
            }
            catch (Exception) {
                // Log exception if you have a logging mechanism
                return new List<ReserveStudy>();
            }
        }

        /// <summary>
        /// Maps reserve study data to an email model
        /// </summary>
        public ReserveStudyEmail MapToReserveStudyEmail(ReserveStudy reserveStudy, string message) {
            return new ReserveStudyEmail {
                ReserveStudy = reserveStudy,
                BaseUrl = _baseUrl,
                AdditionalMessage = message
            };
        }

        /// <summary>
        /// Filter reserve studies based on a search string
        /// </summary>
        public IEnumerable<ReserveStudy> FilterReserveStudies(IEnumerable<ReserveStudy> reserveStudies, string searchString) {
            if (string.IsNullOrWhiteSpace(searchString)) {
                return reserveStudies;
            }

            searchString = searchString.Trim().ToLower();
            return reserveStudies.Where(study =>
                study.Community?.Name?.ToLower().Contains(searchString) == true ||
                study.Contact?.FullName?.ToLower().Contains(searchString) == true ||
                study.PropertyManager?.FullName?.ToLower().Contains(searchString) == true ||
                study.Community?.Addresses?.Any(address =>
                    address.Street?.ToLower().Contains(searchString) == true ||
                    address.City?.ToLower().Contains(searchString) == true ||
                    address.State?.ToLower().Contains(searchString) == true ||
                    address.Zip?.ToLower().Contains(searchString) == true
                ) == true
            );
        }

        #region Token Access
        /// <summary>
        /// Gets a reserve study using a token for external access
        /// </summary>
        public async Task<ReserveStudy?> GetStudyByTokenAsync(string tokenStr) {
            if (!Guid.TryParse(tokenStr, out var token)) {
                return null;
            }

            // First validate the token and get the associated request ID
            var requestId = await ValidateAccessTokenAsync(token);
            if (requestId == null) {
                return null;
            }

            using var context = await _dbFactory.CreateDbContextAsync();

            // Fetch the reserve study with the request ID
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.BuildingElement)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.CommonElement)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyAdditionalElements!).ThenInclude(be => be.ServiceContact)
                .AsSplitQuery()
                .FirstOrDefaultAsync(rs => rs.Id == requestId && rs.IsActive);
        }

        /// <summary>
        /// Generates a temporary access token for a reserve study
        /// </summary>
        public async Task<Guid> GenerateAccessTokenAsync(Guid requestId) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var token = Guid.CreateVersion7();

            var accessToken = new AccessToken {
                Token = token,
                Expiration = DateTime.UtcNow.AddDays(7), // Token valid for 7 days
                RequestId = requestId
            };

            context.AccessTokens.Add(accessToken);
            await context.SaveChangesAsync();

            return token;
        }

        /// <summary>
        /// Validates an access token and returns the associated request ID if valid
        /// </summary>
        public async Task<Guid?> ValidateAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            return await context.AccessTokens
                .Where(t => t.Token == token && t.Expiration > DateTime.UtcNow)
                .Select(t => t.RequestId)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Revokes an access token
        /// </summary>
        public async Task RevokeAccessTokenAsync(Guid token) {
            using var context = await _dbFactory.CreateDbContextAsync();

            var accessToken = await context.AccessTokens.FirstOrDefaultAsync(t => t.Token == token);
            if (accessToken != null) {
                context.AccessTokens.Remove(accessToken);
                await context.SaveChangesAsync();
            }
        }
        /// <summary>
        /// Gets a reserve study by its ID
        /// </summary>
        public async Task<ReserveStudy?> GetReserveStudyByIdAsync(Guid studyId) {
            using var context = await _dbFactory.CreateDbContextAsync();

            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.BuildingElement)
                .Include(rs => rs.ReserveStudyBuildingElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.CommonElement)
                .Include(rs => rs.ReserveStudyCommonElements!).ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyAdditionalElements!).ThenInclude(be => be.ServiceContact)
                .AsSplitQuery()
                .FirstOrDefaultAsync(rs => rs.Id == studyId && rs.IsActive);
        }
        #endregion
    }
}
