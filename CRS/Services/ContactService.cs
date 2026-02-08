using CRS.Data;
using CRS.Models;
using CRS.Services.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class ContactService : IContactService {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

        public ContactService(IDbContextFactory<ApplicationDbContext> dbFactory) {
            _dbFactory = dbFactory;
        }

        #region Contact Methods
        public async Task<List<Contact>> GetUserContactsAsync(string userId) {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Load all non-deleted contacts
            var allContacts = await context.Contacts
                .Where(c => !c.DateDeleted.HasValue)
                // Filter out empty contacts (created but never populated)
                .Where(c => !string.IsNullOrWhiteSpace(c.FirstName) || 
                           !string.IsNullOrWhiteSpace(c.LastName) || 
                           !string.IsNullOrWhiteSpace(c.Email))
                .ToListAsync();

            // Deduplicate by name/email/phone in memory
            return allContacts
                .GroupBy(c => new { 
                    FirstName = c.FirstName?.ToLowerInvariant()?.Trim(),
                    LastName = c.LastName?.ToLowerInvariant()?.Trim(),
                    Email = c.Email?.ToLowerInvariant()?.Trim()
                })
                .Select(g => g.First())
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToList();
        }

        public async Task<List<ServiceContact>> GetServiceContactsAsync() {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Load all non-empty service contacts
            var allContacts = await context.ServiceContacts
                .Where(c => !c.DateDeleted.HasValue)
                // Filter out empty service contacts (created by element initialization but never populated)
                .Where(c => !string.IsNullOrWhiteSpace(c.FirstName) || 
                           !string.IsNullOrWhiteSpace(c.LastName) || 
                           !string.IsNullOrWhiteSpace(c.CompanyName) ||
                           !string.IsNullOrWhiteSpace(c.Email))
                .ToListAsync();

            // Deduplicate by name/email/company/phone in memory
            return allContacts
                .GroupBy(c => new { 
                    FirstName = c.FirstName?.ToLowerInvariant()?.Trim(),
                    LastName = c.LastName?.ToLowerInvariant()?.Trim(),
                    Email = c.Email?.ToLowerInvariant()?.Trim(), 
                    CompanyName = c.CompanyName?.ToLowerInvariant()?.Trim(),
                    Phone = c.Phone?.Trim()
                })
                .Select(g => g.First())
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .ToList();
        }

        /// <summary>
        /// Gets all service contact IDs that match the given service contact's identifying info.
        /// Used for finding all elements associated with a "grouped" service contact.
        /// </summary>
        public async Task<List<Guid>> GetMatchingServiceContactIdsAsync(Guid serviceContactId) {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var contact = await context.ServiceContacts
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == serviceContactId);

            if (contact == null) return [serviceContactId];

            return await context.ServiceContacts
                .Where(c => !c.DateDeleted.HasValue)
                .Where(c => 
                    (c.FirstName ?? "").ToLower().Trim() == (contact.FirstName ?? "").ToLower().Trim() &&
                    (c.LastName ?? "").ToLower().Trim() == (contact.LastName ?? "").ToLower().Trim() &&
                    (c.Email ?? "").ToLower().Trim() == (contact.Email ?? "").ToLower().Trim() &&
                    (c.CompanyName ?? "").ToLower().Trim() == (contact.CompanyName ?? "").ToLower().Trim() &&
                    (c.Phone ?? "").Trim() == (contact.Phone ?? "").Trim())
                .Select(c => c.Id)
                .ToListAsync();
        }

        public async Task<Contact> GetContactByIdAsync(Guid contactId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue) ?? throw new KeyNotFoundException($"Contact with ID {contactId} not found.");
        }

        public async Task<ServiceContact> GetServiceContactByIdAsync(Guid contactId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ServiceContacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue) ?? throw new KeyNotFoundException($"Service Contact with ID {contactId} not found.");
        }

        public async Task<Contact> CreateContactAsync(Contact contact) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            contact.DateCreated = DateTime.UtcNow;
            context.Contacts.Add(contact);
            await context.SaveChangesAsync();
            return contact;
        }

        public async Task<ServiceContact> CreateServiceContactAsync(ServiceContact contact) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            contact.DateCreated = DateTime.UtcNow;
            context.ServiceContacts.Add(contact);
            await context.SaveChangesAsync();
            return contact;
        }

        public async Task<Contact> UpdateContactAsync(Contact contact) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var existingContact = await context.Contacts.FirstOrDefaultAsync(c => c.Id == contact.Id && !c.DateDeleted.HasValue) 
                ?? throw new KeyNotFoundException($"Contact with ID {contact.Id} not found.");

            // Update properties based on actual Contact model
            existingContact.FirstName = contact.FirstName;
            existingContact.LastName = contact.LastName;
            existingContact.CompanyName = contact.CompanyName;
            existingContact.Email = contact.Email;
            existingContact.Phone = contact.Phone;
            existingContact.Extension = contact.Extension;
            existingContact.DateModified = DateTime.UtcNow;

            context.Contacts.Update(existingContact);
            await context.SaveChangesAsync();
            return existingContact;
        }

        public async Task<ServiceContact> UpdateServiceContactAsync(ServiceContact contact) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var existingContact = await context.ServiceContacts.FirstOrDefaultAsync(c => c.Id == contact.Id && !c.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Service Contact with ID {contact.Id} not found.");

            // Update properties based on actual ServiceContact model
            existingContact.FirstName = contact.FirstName;
            existingContact.LastName = contact.LastName;
            existingContact.CompanyName = contact.CompanyName;
            existingContact.Email = contact.Email;
            existingContact.Phone = contact.Phone;
            existingContact.Extension = contact.Extension;
            existingContact.DateModified = DateTime.UtcNow;

            context.ServiceContacts.Update(existingContact);
            await context.SaveChangesAsync();
            return existingContact;
        }

        public async Task DeleteContactAsync(Guid contactId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var contact = await context.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Contact with ID {contactId} not found.");
            // Soft delete
            contact.DateDeleted = DateTime.UtcNow;
            context.Contacts.Update(contact);
            await context.SaveChangesAsync();
        }

        public async Task DeleteServiceContactAsync(Guid contactId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var contact = await context.ServiceContacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Service Contact with ID {contactId} not found.");
            // Soft delete
            contact.DateDeleted = DateTime.UtcNow;
            context.ServiceContacts.Update(contact);
            await context.SaveChangesAsync();
        }
        #endregion

        #region Contact Group Methods
        public async Task<List<ContactGroup>> GetContactGroupsAsync(string userId) {
            if (Guid.TryParse(userId, out Guid userGuid)) {
                await using var context = await _dbFactory.CreateDbContextAsync();
                return await context.ContactGroups
                    .Where(g => g.ApplicationUserId == userGuid && !g.DateDeleted.HasValue)
                    .OrderBy(g => g.Name)
                    .ToListAsync();
            }

            return new List<ContactGroup>();
        }

        public async Task<ContactGroup> GetContactGroupByIdAsync(Guid groupId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ContactGroups.FirstOrDefaultAsync(g => g.Id == groupId && !g.DateDeleted.HasValue) 
                ?? throw new KeyNotFoundException($"Contact group with ID {groupId} not found.");
        }

        public async Task<List<Contact>> GetContactsByGroupIdAsync(Guid groupId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            return await context.ContactXContactGroups
                .Where(x => x.ContactGroup.Id == groupId && !x.DateDeleted.HasValue)
                .Select(x => x.Contact)
                .Where(c => !c.DateDeleted.HasValue)
                .ToListAsync();
        }

        public async Task<ContactGroup> CreateContactGroupAsync(ContactGroup group) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            group.DateCreated = DateTime.UtcNow;
            context.ContactGroups.Add(group);
            await context.SaveChangesAsync();
            return group;
        }

        public async Task<ContactGroup> UpdateContactGroupAsync(ContactGroup group) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var existingGroup = await context.ContactGroups.FirstOrDefaultAsync(g => g.Id == group.Id && !g.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Contact group with ID {group.Id} not found.");

            existingGroup.Name = group.Name;
            existingGroup.Description = group.Description;
            existingGroup.DateModified = DateTime.UtcNow;

            context.ContactGroups.Update(existingGroup);
            await context.SaveChangesAsync();
            return existingGroup;
        }

        public async Task DeleteContactGroupAsync(Guid groupId) {
            await using var context = await _dbFactory.CreateDbContextAsync();
            var group = await context.ContactGroups.FirstOrDefaultAsync(g => g.Id == groupId && !g.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Contact group with ID {groupId} not found.");

            // Soft delete the group
            group.DateDeleted = DateTime.UtcNow;
            context.ContactGroups.Update(group);

            // Also soft delete all relationships with this group
            var relationships = await context.ContactXContactGroups
                .Where(x => x.ContactGroup.Id == groupId && !x.DateDeleted.HasValue)
                .ToListAsync();

            foreach (var rel in relationships) {
                rel.DateDeleted = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }

        public async Task AddContactToGroupAsync(Guid contactId, Guid groupId) {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Check if contact and group exist
            var contact = await context.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Contact with ID {contactId} not found.");
            var group = await context.ContactGroups.FirstOrDefaultAsync(g => g.Id == groupId && !g.DateDeleted.HasValue)
                ?? throw new KeyNotFoundException($"Contact group with ID {groupId} not found.");

            // Check if relationship already exists
            var existingRelationship = await context.ContactXContactGroups
                .FirstOrDefaultAsync(x => x.Contact.Id == contactId &&
                                        x.ContactGroup.Id == groupId &&
                                        !x.DateDeleted.HasValue);

            if (existingRelationship != null) {
                // Relationship already exists, no need to add again
                return;
            }

            // Create a new relationship
            var relationship = new ContactXContactGroup {
                Contact = contact,
                ContactGroup = group,
                DateCreated = DateTime.UtcNow
            };

            context.ContactXContactGroups.Add(relationship);
            await context.SaveChangesAsync();
        }

        public async Task RemoveContactFromGroupAsync(Guid contactId, Guid groupId) {
            await using var context = await _dbFactory.CreateDbContextAsync();

            // Find the relationship
            var relationship = await context.ContactXContactGroups
                .FirstOrDefaultAsync(x => x.Contact.Id == contactId &&
                                        x.ContactGroup.Id == groupId &&
                                        !x.DateDeleted.HasValue);

            if (relationship == null) {
                // Relationship doesn't exist
                return;
            }

            // Soft delete the relationship
            relationship.DateDeleted = DateTime.UtcNow;
            context.ContactXContactGroups.Update(relationship);
            await context.SaveChangesAsync();
        }
        #endregion
    }
}