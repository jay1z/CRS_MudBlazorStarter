using Coravel.Events.Interfaces;
using Coravel.Mailer.Mail.Interfaces;

using CRS.Data;
using CRS.EventsAndListeners;
using CRS.Models;
using CRS.Models.Emails;
using CRS.Models.Workflow;
using CRS.Services.Interfaces;
using CRS.Services.Tenant;
using CRS.Services.Billing; // feature guard

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CRS.Services {
    public class ReserveStudyService : IReserveStudyService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly IDispatcher _dispatcher;
        private readonly IConfiguration _configuration;
        private readonly ITenantContext _tenantContext;
        private readonly IFeatureGuardService _featureGuard;
        private readonly ILogger<ReserveStudyService> _logger;

        public ReserveStudyService(IDbContextFactory<ApplicationDbContext> dbFactory, IMailer mailer, IDispatcher dispatcher, IConfiguration configuration, ITenantContext tenantContext, IFeatureGuardService featureGuard, ILogger<ReserveStudyService> logger) {
            _dbFactory = dbFactory;
            _dispatcher = dispatcher;
            _configuration = configuration;
            _tenantContext = tenantContext;
            _featureGuard = featureGuard;
            _logger = logger;
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

            _logger.LogInformation("CreateReserveStudyAsync: CommunityId={CommunityId}, Community is {CommunityState}", 
                reserveStudy.CommunityId, 
                reserveStudy.Community == null ? "null" : $"not null (Id={reserveStudy.Community.Id})");

            // SECURITY: Validate existing community belongs to current tenant
            if (reserveStudy.CommunityId != Guid.Empty) {
                var existingCommunity = await context.Communities
                    .IgnoreQueryFilters() // Bypass tenant filter since we validate tenant below
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == reserveStudy.CommunityId);

                _logger.LogInformation("CreateReserveStudyAsync: Looking for community {CommunityId}, found={Found}", 
                    reserveStudy.CommunityId, existingCommunity != null);

                if (existingCommunity == null) {
                    // Community not found - this shouldn't happen if it was just saved
                    // Check if CommunityId is a valid GUID that was set but community wasn't actually saved
                    _logger.LogWarning("CreateReserveStudyAsync: Community {CommunityId} not found in database. Community object is {State}", 
                        reserveStudy.CommunityId, 
                        reserveStudy.Community == null ? "null" : "available");
                    
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

                        // Ensure physical address has an ID and set the FK
                        if (reserveStudy.Community.PhysicalAddress != null) {
                            if (reserveStudy.Community.PhysicalAddress.Id == Guid.Empty) {
                                reserveStudy.Community.PhysicalAddress.Id = Guid.CreateVersion7();
                            }
                            reserveStudy.Community.PhysicalAddressId = reserveStudy.Community.PhysicalAddress.Id;
                        }

                        // Ensure mailing address has an ID if provided
                        if (reserveStudy.Community.MailingAddress != null) {
                            if (reserveStudy.Community.MailingAddress.Id == Guid.Empty) {
                                reserveStudy.Community.MailingAddress.Id = Guid.CreateVersion7();
                            }
                            reserveStudy.Community.MailingAddressId = reserveStudy.Community.MailingAddress.Id;
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

            // Ensure ReserveStudy has an ID before processing elements
            if (reserveStudy.Id == Guid.Empty) {
                reserveStudy.Id = Guid.CreateVersion7();
            }

            // Log incoming element counts for debugging
            _logger.LogInformation("CreateReserveStudyAsync: Study ID: {StudyId}", reserveStudy.Id);
            _logger.LogInformation("CreateReserveStudyAsync: BuildingElements count: {Count}", 
                reserveStudy.ReserveStudyBuildingElements?.Count ?? 0);
            _logger.LogInformation("CreateReserveStudyAsync: CommonElements count: {Count}", 
                reserveStudy.ReserveStudyCommonElements?.Count ?? 0);
            _logger.LogInformation("CreateReserveStudyAsync: AdditionalElements count: {Count}", 
                reserveStudy.ReserveStudyAdditionalElements?.Count ?? 0);

            // Process Building Elements
            if (reserveStudy.ReserveStudyBuildingElements != null && reserveStudy.ReserveStudyBuildingElements.Any()) {
                foreach (var element in reserveStudy.ReserveStudyBuildingElements) {
                    if (element.Id == Guid.Empty) {
                        element.Id = Guid.CreateVersion7();
                    }
                    element.ReserveStudyId = reserveStudy.Id;
                    // Don't track the BuildingElement navigation property to avoid duplicate tracking
                    element.BuildingElement = null;
                    _logger.LogInformation("CreateReserveStudyAsync: Processing BuildingElement {ElementId} -> {BuildingElementId}", 
                        element.Id, element.BuildingElementId);
                }
            }

            // Process Common Elements
            if (reserveStudy.ReserveStudyCommonElements != null && reserveStudy.ReserveStudyCommonElements.Any()) {
                foreach (var element in reserveStudy.ReserveStudyCommonElements) {
                    if (element.Id == Guid.Empty) {
                        element.Id = Guid.CreateVersion7();
                    }
                    element.ReserveStudyId = reserveStudy.Id;
                    // Don't track the CommonElement navigation property to avoid duplicate tracking
                    element.CommonElement = null;
                    _logger.LogInformation("CreateReserveStudyAsync: Processing CommonElement {ElementId} -> {CommonElementId}", 
                        element.Id, element.CommonElementId);
                }
            }

            // Process Additional Elements
            if (reserveStudy.ReserveStudyAdditionalElements != null && reserveStudy.ReserveStudyAdditionalElements.Any()) {
                foreach (var element in reserveStudy.ReserveStudyAdditionalElements) {
                    if (element.Id == Guid.Empty) {
                        element.Id = Guid.CreateVersion7();
                    }
                    element.ReserveStudyId = reserveStudy.Id;
                }
            }

            context.ReserveStudies.Add(reserveStudy);
            await context.SaveChangesAsync();

            var community = await context.Communities
            .AsNoTracking()
            .Include(c => c.PhysicalAddress)
            .Include(c => c.MailingAddress)
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

            // Log incoming data for debugging
            _logger.LogInformation("UpdateReserveStudyAsync: Starting update for study {StudyId}", reserveStudy.Id);
            _logger.LogInformation("UpdateReserveStudyAsync: Incoming BuildingElements count: {Count}", 
                reserveStudy.ReserveStudyBuildingElements?.Count ?? 0);
            _logger.LogInformation("UpdateReserveStudyAsync: Incoming CommonElements count: {Count}", 
                reserveStudy.ReserveStudyCommonElements?.Count ?? 0);
            _logger.LogInformation("UpdateReserveStudyAsync: Incoming AdditionalElements count: {Count}", 
                reserveStudy.ReserveStudyAdditionalElements?.Count ?? 0);
            _logger.LogInformation("UpdateReserveStudyAsync: Incoming MailingAddress: {HasAddress}", 
                reserveStudy.Community?.MailingAddress != null ? $"Yes - {reserveStudy.Community.MailingAddress.Street}" : "No");

            // Fetch the existing study with tracking
            var existingStudy = await context.ReserveStudies
                .Include(rs => rs.Community)
                    .ThenInclude(c => c.PhysicalAddress)
                .Include(rs => rs.Community)
                    .ThenInclude(c => c.MailingAddress)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.ReserveStudyBuildingElements)
                    .ThenInclude(be => be.ServiceContact)
                .Include(rs => rs.ReserveStudyCommonElements)
                    .ThenInclude(ce => ce.ServiceContact)
                .Include(rs => rs.ReserveStudyAdditionalElements)
                    .ThenInclude(ae => ae.ServiceContact)
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

            // Property Manager information - handle add, update, and remove scenarios
            if (reserveStudy.PropertyManager != null) {
                if (existingStudy.PropertyManager != null) {
                    // Update existing property manager
                    existingStudy.PropertyManager.FirstName = reserveStudy.PropertyManager.FirstName;
                    existingStudy.PropertyManager.LastName = reserveStudy.PropertyManager.LastName;
                    existingStudy.PropertyManager.Email = reserveStudy.PropertyManager.Email;
                    existingStudy.PropertyManager.Phone = reserveStudy.PropertyManager.Phone;
                    existingStudy.PropertyManager.Extension = reserveStudy.PropertyManager.Extension;
                    existingStudy.PropertyManager.CompanyName = reserveStudy.PropertyManager.CompanyName;
                } else {
                    // Add new property manager
                    var newPropertyManager = new PropertyManager {
                        Id = Guid.CreateVersion7(),
                        FirstName = reserveStudy.PropertyManager.FirstName,
                        LastName = reserveStudy.PropertyManager.LastName,
                        Email = reserveStudy.PropertyManager.Email,
                        Phone = reserveStudy.PropertyManager.Phone,
                        Extension = reserveStudy.PropertyManager.Extension,
                        CompanyName = reserveStudy.PropertyManager.CompanyName,
                        TenantId = tenantId
                    };
                    context.PropertyManagers.Add(newPropertyManager);
                    existingStudy.PropertyManagerId = newPropertyManager.Id;
                    existingStudy.PropertyManager = newPropertyManager;
                }
            } else if (existingStudy.PropertyManager != null) {
                // Property manager was removed - clear the reference
                existingStudy.PropertyManagerId = null;
                existingStudy.PropertyManager = null;
            }

            // Update Point of Contact Type
            existingStudy.PointOfContactType = reserveStudy.PointOfContactType;

            // Update community information if allowed
            if (reserveStudy.Community != null && existingStudy.Community != null) {
                // SECURITY: Verify the community belongs to the same tenant
                if (existingStudy.Community.TenantId == tenantId) {
                    existingStudy.Community.Name = reserveStudy.Community.Name;
                    existingStudy.Community.AnnualMeetingDate = reserveStudy.Community.AnnualMeetingDate;
                    
                    // Update physical address
                    if (reserveStudy.Community.PhysicalAddress != null) {
                        if (existingStudy.Community.PhysicalAddress != null) {
                            existingStudy.Community.PhysicalAddress.Street = reserveStudy.Community.PhysicalAddress.Street;
                            existingStudy.Community.PhysicalAddress.City = reserveStudy.Community.PhysicalAddress.City;
                            existingStudy.Community.PhysicalAddress.State = reserveStudy.Community.PhysicalAddress.State;
                            existingStudy.Community.PhysicalAddress.Zip = reserveStudy.Community.PhysicalAddress.Zip;
                        } else {
                            // Create new physical address
                            var newPhysicalAddr = new Address {
                                Id = Guid.CreateVersion7(),
                                Street = reserveStudy.Community.PhysicalAddress.Street,
                                City = reserveStudy.Community.PhysicalAddress.City,
                                State = reserveStudy.Community.PhysicalAddress.State,
                                Zip = reserveStudy.Community.PhysicalAddress.Zip
                            };
                            context.Addresses.Add(newPhysicalAddr);
                            existingStudy.Community.PhysicalAddressId = newPhysicalAddr.Id;
                            existingStudy.Community.PhysicalAddress = newPhysicalAddr;
                        }
                    }

                    // Update mailing address
                    if (reserveStudy.Community.MailingAddress != null) {
                        if (existingStudy.Community.MailingAddress != null) {
                            existingStudy.Community.MailingAddress.Street = reserveStudy.Community.MailingAddress.Street;
                            existingStudy.Community.MailingAddress.City = reserveStudy.Community.MailingAddress.City;
                            existingStudy.Community.MailingAddress.State = reserveStudy.Community.MailingAddress.State;
                            existingStudy.Community.MailingAddress.Zip = reserveStudy.Community.MailingAddress.Zip;
                        } else {
                            // Create new mailing address
                            var newMailingAddr = new Address {
                                Id = Guid.CreateVersion7(),
                                Street = reserveStudy.Community.MailingAddress.Street,
                                City = reserveStudy.Community.MailingAddress.City,
                                State = reserveStudy.Community.MailingAddress.State,
                                Zip = reserveStudy.Community.MailingAddress.Zip
                            };
                            context.Addresses.Add(newMailingAddr);
                            existingStudy.Community.MailingAddressId = newMailingAddr.Id;
                            existingStudy.Community.MailingAddress = newMailingAddr;
                        }
                    } else if (existingStudy.Community.MailingAddressId != null) {
                        // Mailing address was removed - set to null (use physical as mailing)
                        existingStudy.Community.MailingAddressId = null;
                        existingStudy.Community.MailingAddress = null;
                    }
                }
            }

            // Update Building Elements - handle additions, removals, and service contacts
            if (reserveStudy.ReserveStudyBuildingElements != null) {
                var existingBuildingElementIds = existingStudy.ReserveStudyBuildingElements?
                    .Select(e => e.BuildingElementId).ToHashSet() ?? new HashSet<Guid>();
                var updatedBuildingElementIds = reserveStudy.ReserveStudyBuildingElements
                    .Select(e => e.BuildingElementId).ToHashSet();

                // Add new building elements or update existing ones
                foreach (var element in reserveStudy.ReserveStudyBuildingElements) {
                    if (!existingBuildingElementIds.Contains(element.BuildingElementId)) {
                        // New element
                        if (element.Id == Guid.Empty) {
                            element.Id = Guid.CreateVersion7();
                        }
                        element.ReserveStudyId = existingStudy.Id;
                        element.BuildingElement = null; // Prevent navigation property tracking issues
                        existingStudy.ReserveStudyBuildingElements ??= new List<ReserveStudyBuildingElement>();
                        existingStudy.ReserveStudyBuildingElements.Add(element);
                        context.Add(element);
                    } else {
                        // Update existing element's properties
                        var existingElement = existingStudy.ReserveStudyBuildingElements?
                            .FirstOrDefault(e => e.BuildingElementId == element.BuildingElementId);
                        if (existingElement != null) {
                            // Update element details (staff mode data entry)
                            existingElement.Count = element.Count;
                            existingElement.LastServiced = element.LastServiced;
                            existingElement.UsefulLifeOptionId = element.UsefulLifeOptionId;
                            existingElement.RemainingLifeOptionId = element.RemainingLifeOptionId;
                            existingElement.MeasurementOptionId = element.MeasurementOptionId;
                            
                            // Update service contact if provided
                            if (element.ServiceContact != null) {
                                UpdateServiceContact(existingElement, element.ServiceContact);
                            }
                        }
                    }
                }

                // Remove building elements that are no longer selected
                var buildingElementsToRemove = existingStudy.ReserveStudyBuildingElements?
                    .Where(e => !updatedBuildingElementIds.Contains(e.BuildingElementId))
                    .ToList();
                if (buildingElementsToRemove != null && buildingElementsToRemove.Any()) {
                    foreach (var element in buildingElementsToRemove) {
                        existingStudy.ReserveStudyBuildingElements?.Remove(element);
                        context.Remove(element);
                    }
                }
            }

            // Update Common Elements - handle additions, removals, and service contacts
            if (reserveStudy.ReserveStudyCommonElements != null) {
                var existingCommonElementIds = existingStudy.ReserveStudyCommonElements?
                    .Select(e => e.CommonElementId).ToHashSet() ?? new HashSet<Guid>();
                var updatedCommonElementIds = reserveStudy.ReserveStudyCommonElements
                    .Select(e => e.CommonElementId).ToHashSet();

                // Add new common elements or update existing ones
                foreach (var element in reserveStudy.ReserveStudyCommonElements) {
                    if (!existingCommonElementIds.Contains(element.CommonElementId)) {
                        // New element
                        if (element.Id == Guid.Empty) {
                            element.Id = Guid.CreateVersion7();
                        }
                        element.ReserveStudyId = existingStudy.Id;
                        element.CommonElement = null; // Prevent navigation property tracking issues
                        existingStudy.ReserveStudyCommonElements ??= new List<ReserveStudyCommonElement>();
                        existingStudy.ReserveStudyCommonElements.Add(element);
                        context.Add(element);
                    } else {
                        // Update existing element's properties
                        var existingElement = existingStudy.ReserveStudyCommonElements?
                            .FirstOrDefault(e => e.CommonElementId == element.CommonElementId);
                        if (existingElement != null) {
                            // Update element details (staff mode data entry)
                            existingElement.Count = element.Count;
                            existingElement.LastServiced = element.LastServiced;
                            existingElement.UsefulLifeOptionId = element.UsefulLifeOptionId;
                            existingElement.RemainingLifeOptionId = element.RemainingLifeOptionId;
                            existingElement.MeasurementOptionId = element.MeasurementOptionId;
                            
                            // Update service contact if provided
                            if (element.ServiceContact != null) {
                                UpdateServiceContact(existingElement, element.ServiceContact);
                            }
                        }
                    }
                }

                // Remove common elements that are no longer selected
                var commonElementsToRemove = existingStudy.ReserveStudyCommonElements?
                    .Where(e => !updatedCommonElementIds.Contains(e.CommonElementId))
                    .ToList();
                if (commonElementsToRemove != null && commonElementsToRemove.Any()) {
                    foreach (var element in commonElementsToRemove) {
                        existingStudy.ReserveStudyCommonElements?.Remove(element);
                        context.Remove(element);
                    }
                }
            }

            // Update Additional Elements - handle additions, updates, and removals
            if (reserveStudy.ReserveStudyAdditionalElements != null) {
                _logger.LogInformation("UpdateReserveStudyAsync: Processing {Count} incoming additional elements", 
                    reserveStudy.ReserveStudyAdditionalElements.Count);
                
                // Get IDs of elements that already exist in the database
                var existingAdditionalElementIds = existingStudy.ReserveStudyAdditionalElements?
                    .Select(e => e.Id).ToHashSet() ?? new HashSet<Guid>();
                    
                _logger.LogInformation("UpdateReserveStudyAsync: Existing additional element IDs in DB: {Count}", 
                    existingAdditionalElementIds.Count);

                // Get IDs of incoming elements that exist in the database (for removal check)
                var incomingElementIds = reserveStudy.ReserveStudyAdditionalElements
                    .Select(e => e.Id).ToHashSet();

                // Add or update additional elements
                foreach (var element in reserveStudy.ReserveStudyAdditionalElements) {
                    // Check if this element exists in the database
                    bool existsInDb = existingAdditionalElementIds.Contains(element.Id);
                    
                    _logger.LogInformation("UpdateReserveStudyAsync: Processing additional element Id={Id}, Name={Name}, ExistsInDb={Exists}", 
                        element.Id, element.Name, existsInDb);
                    
                    if (!existsInDb) {
                        // New element - only add if it has a name
                        if (!string.IsNullOrWhiteSpace(element.Name)) {
                            element.ReserveStudyId = existingStudy.Id;
                            existingStudy.ReserveStudyAdditionalElements ??= new List<ReserveStudyAdditionalElement>();
                            existingStudy.ReserveStudyAdditionalElements.Add(element);
                            context.Add(element);
                            _logger.LogInformation("UpdateReserveStudyAsync: Added new additional element {Name} with Id {Id}", element.Name, element.Id);
                        }
                    } else {
                        // Update existing element
                        var existingElement = existingStudy.ReserveStudyAdditionalElements?
                            .FirstOrDefault(e => e.Id == element.Id);
                        if (existingElement != null) {
                            existingElement.Name = element.Name;
                            existingElement.NeedsService = element.NeedsService;
                            // Update element details (staff mode data entry)
                            existingElement.Count = element.Count;
                            existingElement.LastServiced = element.LastServiced;
                            existingElement.UsefulLifeOptionId = element.UsefulLifeOptionId;
                            existingElement.RemainingLifeOptionId = element.RemainingLifeOptionId;
                            existingElement.MeasurementOptionId = element.MeasurementOptionId;
                            // Update service contact
                            if (element.ServiceContact != null) {
                                UpdateServiceContact(existingElement, element.ServiceContact);
                            }
                            _logger.LogInformation("UpdateReserveStudyAsync: Updated additional element {Name}", element.Name);
                        }
                    }
                }

                // Remove additional elements that are no longer in the incoming collection
                var additionalElementsToRemove = existingStudy.ReserveStudyAdditionalElements?
                    .Where(e => !incomingElementIds.Contains(e.Id))
                    .ToList();
                    
                _logger.LogInformation("UpdateReserveStudyAsync: Elements to remove: {Count}", 
                    additionalElementsToRemove?.Count ?? 0);
                    
                if (additionalElementsToRemove != null && additionalElementsToRemove.Any()) {
                    foreach (var element in additionalElementsToRemove) {
                        _logger.LogInformation("UpdateReserveStudyAsync: Removing additional element {Name}", element.Name);
                        existingStudy.ReserveStudyAdditionalElements?.Remove(element);
                        context.Remove(element);
                    }
                }
            }

            existingStudy.DateModified = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return existingStudy;
        }

        /// <summary>
        /// Updates the service contact on a reserve study element.
        /// </summary>
        private void UpdateServiceContact(IReserveStudyElement existingElement, ServiceContact incomingContact) {
            if (existingElement.ServiceContact == null) {
                existingElement.ServiceContact = new ServiceContact();
            }
            
            existingElement.ServiceContact.CompanyName = incomingContact.CompanyName;
            existingElement.ServiceContact.FirstName = incomingContact.FirstName;
            existingElement.ServiceContact.LastName = incomingContact.LastName;
            existingElement.ServiceContact.Email = incomingContact.Email;
            existingElement.ServiceContact.Phone = incomingContact.Phone;
            existingElement.ServiceContact.Extension = incomingContact.Extension;
        }

        /// <summary>
        /// Determines if a reserve study can be updated by the HOA user.
        /// Reserve studies can only be updated before the proposal is accepted.
        /// </summary>
        public bool CanUpdateReserveStudy(ReserveStudy reserveStudy) {
            if (reserveStudy == null) {
                return false;
            }

            // Cannot update if the study is complete or cancelled
            if (reserveStudy.IsComplete || 
                reserveStudy.CurrentStatus == StudyStatus.RequestCompleted ||
                reserveStudy.CurrentStatus == StudyStatus.RequestCancelled ||
                reserveStudy.CurrentStatus == StudyStatus.RequestArchived) {
                return false;
            }

            // HOA users can only update before the proposal is accepted
            return reserveStudy.CurrentStatus < StudyStatus.ProposalAccepted;
        }

        /// <summary>
        /// Determines if a reserve study can be updated by staff (TenantOwner, TenantSpecialist, Admin).
        /// Staff can update at any time before the study is completed.
        /// </summary>
        public bool CanStaffUpdateReserveStudy(ReserveStudy reserveStudy) {
            if (reserveStudy == null) {
                return false;
            }

            // Cannot update if the study is complete or cancelled
            if (reserveStudy.IsComplete || 
                reserveStudy.CurrentStatus == StudyStatus.RequestCompleted ||
                reserveStudy.CurrentStatus == StudyStatus.RequestCancelled ||
                reserveStudy.CurrentStatus == StudyStatus.RequestArchived) {
                return false;
            }

            // Staff can update at any point before completion
            return true;
        }

        /// <summary>
        /// Gets all active reserve studies
        /// </summary>
        public async Task<List<ReserveStudy>> GetAllReserveStudiesAsync() {
            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                    .ThenInclude(c => c.PhysicalAddress)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.User)
                .Include(rs => rs.StudyRequest)
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
                    .ThenInclude(c => c.PhysicalAddress)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.Specialist)
                .Include(rs => rs.StudyRequest)
                .Where(rs => rs.IsActive && rs.SpecialistUserId == userId)
                .OrderBy(rs => rs.Community.Name)
                .AsSplitQuery()
                .ToListAsync();
        }

        /// <summary>
        /// Gets reserve studies owned by a user (as creator OR as the requested customer)
        /// </summary>
        public async Task<List<ReserveStudy>> GetOwnedReserveStudiesAsync(Guid userId) {
            if (userId == Guid.Empty) {
                return new List<ReserveStudy>();
            }

            using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ReserveStudies
                .AsNoTracking()
                .Include(rs => rs.Community)
                    .ThenInclude(c => c.PhysicalAddress)
                .Include(rs => rs.Contact)
                .Include(rs => rs.PropertyManager)
                .Include(rs => rs.User)
                .Include(rs => rs.RequestedByUser)
                .Include(rs => rs.StudyRequest)
                .Where(rs => rs.IsActive && (rs.ApplicationUserId == userId || rs.RequestedByUserId == userId))
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
        /// Maps reserve study data to an email model with tenant-specific base URL and branding
        /// </summary>
        public async Task<ReserveStudyEmail> MapToReserveStudyEmailAsync(ReserveStudy reserveStudy, string message) {
            // Build tenant-specific base URL
            var baseUrl = await BuildTenantBaseUrlAsync(reserveStudy.TenantId);
            
            // Get tenant email info for branding
            var tenantInfo = await GetTenantEmailInfoAsync(reserveStudy.TenantId);

            return new ReserveStudyEmail {
                ReserveStudy = reserveStudy,
                BaseUrl = baseUrl,
                AdditionalMessage = message,
                TenantInfo = tenantInfo
            };
        }

        /// <summary>
        /// Gets tenant-specific email branding and contact information
        /// </summary>
        public async Task<TenantEmailInfo> GetTenantEmailInfoAsync(int tenantId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenant = await context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null) {
                _logger.LogWarning("Could not find tenant {TenantId} for email info", tenantId);
                return TenantEmailInfo.CreateDefault();
            }

            var tenantInfo = new TenantEmailInfo {
                CompanyName = tenant.Name,
                Subdomain = tenant.Subdomain
            };

            // Parse branding JSON to extract company contact info
            if (!string.IsNullOrWhiteSpace(tenant.BrandingJson)) {
                try {
                    var payload = System.Text.Json.JsonSerializer.Deserialize<ThemeService.BrandingPayload>(tenant.BrandingJson);
                    if (payload != null) {
                        tenantInfo.FromEmail = payload.CompanyEmail;
                        tenantInfo.FromName = tenant.Name;
                        tenantInfo.Phone = payload.CompanyPhone;
                        tenantInfo.Website = payload.CompanyWebsite;
                        tenantInfo.Address = payload.CompanyAddress;
                        tenantInfo.Tagline = payload.CompanyTagline;
                        tenantInfo.LogoUrl = payload.CompanyLogoUrl;
                        tenantInfo.PrimaryColor = payload.Primary ?? "#667eea";
                        tenantInfo.SecondaryColor = payload.Secondary ?? "#764ba2";
                    }
                } catch (Exception ex) {
                    _logger.LogWarning(ex, "Failed to parse branding JSON for tenant {TenantId}", tenantId);
                }
            }

            // Build logo URL if not explicitly set
            if (string.IsNullOrWhiteSpace(tenantInfo.LogoUrl)) {
                var rootDomain = _configuration["App:RootDomain"]?.Trim().Trim('.');
                if (!string.IsNullOrWhiteSpace(rootDomain) && !string.IsNullOrWhiteSpace(tenant.Subdomain)) {
                    // Try to use tenant logo from storage
                    tenantInfo.LogoUrl = $"https://{tenant.Subdomain}.{rootDomain}/api/logos/{tenantId}";
                }
            }

            return tenantInfo;
        }

        /// <summary>
        /// Builds the base URL for a tenant using their subdomain
        /// </summary>
        private async Task<string> BuildTenantBaseUrlAsync(int tenantId) {
            var rootDomain = _configuration["App:RootDomain"]?.Trim().Trim('.');

            if (string.IsNullOrWhiteSpace(rootDomain)) {
                // Fallback to configured base URL if root domain is not set
                return _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://localhost";
            }

            await using var context = await _dbFactory.CreateDbContextAsync();
            var tenant = await context.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Subdomain)) {
                _logger.LogWarning("Could not find tenant {TenantId} or subdomain is empty for email URL", tenantId);
                return _configuration["Application:BaseUrl"]?.TrimEnd('/') ?? "https://localhost";
            }

            return $"https://{tenant.Subdomain}.{rootDomain}";
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
                study.Community?.PhysicalAddress?.Street?.ToLower().Contains(searchString) == true ||
                study.Community?.PhysicalAddress?.City?.ToLower().Contains(searchString) == true ||
                study.Community?.PhysicalAddress?.State?.ToLower().Contains(searchString) == true ||
                study.Community?.PhysicalAddress?.Zip?.ToLower().Contains(searchString) == true
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
                .Include(rs => rs.FinancialInfo)
                .Include(rs => rs.CurrentProposal)
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

