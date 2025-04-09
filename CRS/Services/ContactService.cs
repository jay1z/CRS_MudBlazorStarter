using CRS.Data;
using CRS.Models;

using Microsoft.EntityFrameworkCore;

namespace CRS.Services {
    public class ContactService : IContactService {
        private readonly ApplicationDbContext _context;

        public ContactService(ApplicationDbContext context) {
            _context = context;
        }

        #region Contact Methods
        public async Task<List<Contact>> GetUserContactsAsync(string userId) {
            return await _context.Contacts.Where(c => !c.DateDeleted.HasValue).ToListAsync();
        }

        public async Task<List<ServiceContact>> GetServiceContactsAsync() {
            return await _context.ServiceContacts.Where(c => !c.DateDeleted.HasValue).ToListAsync();
        }

        public async Task<Contact> GetContactByIdAsync(Guid contactId) {
            return await _context.Contacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue) ?? throw new KeyNotFoundException($"Contact with ID {contactId} not found.");
        }

        public async Task<ServiceContact> GetServiceContactByIdAsync(Guid contactId) {
            return await _context.ServiceContacts.FirstOrDefaultAsync(c => c.Id == contactId && !c.DateDeleted.HasValue) ?? throw new KeyNotFoundException($"Service Contact with ID {contactId} not found.");
        }

        public async Task<Contact> CreateContactAsync(Contact contact) {
            contact.DateCreated = DateTime.UtcNow;
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<ServiceContact> CreateServiceContactAsync(ServiceContact contact) {
            contact.DateCreated = DateTime.UtcNow;
            _context.ServiceContacts.Add(contact);
            await _context.SaveChangesAsync();
            return contact;
        }

        public async Task<Contact> UpdateContactAsync(Contact contact) {
            var existingContact = await GetContactByIdAsync(contact.Id);

            // Update properties based on actual Contact model
            existingContact.FirstName = contact.FirstName;
            existingContact.LastName = contact.LastName;
            existingContact.CompanyName = contact.CompanyName;
            existingContact.Email = contact.Email;
            existingContact.Phone = contact.Phone;
            existingContact.Extension = contact.Extension;
            existingContact.DateModified = DateTime.UtcNow;

            _context.Contacts.Update(existingContact);
            await _context.SaveChangesAsync();
            return existingContact;
        }

        public async Task<ServiceContact> UpdateServiceContactAsync(ServiceContact contact) {
            var existingContact = await GetServiceContactByIdAsync(contact.Id);

            // Update properties based on actual ServiceContact model
            existingContact.FirstName = contact.FirstName;
            existingContact.LastName = contact.LastName;
            existingContact.CompanyName = contact.CompanyName;
            existingContact.Email = contact.Email;
            existingContact.Phone = contact.Phone;
            existingContact.Extension = contact.Extension;
            existingContact.DateModified = DateTime.UtcNow;

            _context.ServiceContacts.Update(existingContact);
            await _context.SaveChangesAsync();
            return existingContact;
        }

        public async Task DeleteContactAsync(Guid contactId) {
            var contact = await GetContactByIdAsync(contactId);
            // Soft delete
            contact.DateDeleted = DateTime.UtcNow;
            _context.Contacts.Update(contact);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteServiceContactAsync(Guid contactId) {
            var contact = await GetServiceContactByIdAsync(contactId);
            // Soft delete
            contact.DateDeleted = DateTime.UtcNow;
            _context.ServiceContacts.Update(contact);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region Contact Group Methods
        public async Task<List<ContactGroup>> GetContactGroupsAsync(string userId) {
            if (Guid.TryParse(userId, out Guid userGuid)) {
                return await _context.ContactGroups
                    .Where(g => g.ApplicationUserId == userGuid && !g.DateDeleted.HasValue)
                    .OrderBy(g => g.Name)
                    .ToListAsync();
            }

            return new List<ContactGroup>();
        }

        public async Task<ContactGroup> GetContactGroupByIdAsync(Guid groupId) {
            return await _context.ContactGroups.FirstOrDefaultAsync(g => g.Id == groupId && !g.DateDeleted.HasValue) ?? throw new KeyNotFoundException($"Contact group with ID {groupId} not found.");
        }

        public async Task<List<Contact>> GetContactsByGroupIdAsync(Guid groupId) {
            return await _context.ContactXContactGroups
                .Where(x => x.ContactGroup.Id == groupId && !x.DateDeleted.HasValue)
                .Select(x => x.Contact)
                .Where(c => !c.DateDeleted.HasValue)
                .ToListAsync();
        }

        public async Task<ContactGroup> CreateContactGroupAsync(ContactGroup group) {
            // Set creation date
            group.DateCreated = DateTime.UtcNow;

            // Make sure ApplicationUserId is set correctly
            //if (group.ApplicationUserId == Guid.Empty && !string.IsNullOrEmpty(group.UserId)) {
            //    if (Guid.TryParse(group.UserId, out Guid userGuid)) {
                    //group.ApplicationUserId = userGuid;
            //    }
            //}

            _context.ContactGroups.Add(group);
            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<ContactGroup> UpdateContactGroupAsync(ContactGroup group) {
            var existingGroup = await GetContactGroupByIdAsync(group.Id);

            existingGroup.Name = group.Name;
            existingGroup.Description = group.Description;
            existingGroup.DateModified = DateTime.UtcNow;

            _context.ContactGroups.Update(existingGroup);
            await _context.SaveChangesAsync();
            return existingGroup;
        }

        public async Task DeleteContactGroupAsync(Guid groupId) {
            var group = await GetContactGroupByIdAsync(groupId);

            // Soft delete the group
            group.DateDeleted = DateTime.UtcNow;
            _context.ContactGroups.Update(group);

            // Also soft delete all relationships with this group
            var relationships = await _context.ContactXContactGroups
                .Where(x => x.ContactGroup.Id == groupId && !x.DateDeleted.HasValue)
                .ToListAsync();

            foreach (var rel in relationships) {
                rel.DateDeleted = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task AddContactToGroupAsync(Guid contactId, Guid groupId) {
            // Check if contact and group exist
            var contact = await GetContactByIdAsync(contactId);
            var group = await GetContactGroupByIdAsync(groupId);

            // Check if relationship already exists
            var existingRelationship = await _context.ContactXContactGroups
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

            _context.ContactXContactGroups.Add(relationship);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveContactFromGroupAsync(Guid contactId, Guid groupId) {
            // Find the relationship
            var relationship = await _context.ContactXContactGroups
                .FirstOrDefaultAsync(x => x.Contact.Id == contactId &&
                                        x.ContactGroup.Id == groupId &&
                                        !x.DateDeleted.HasValue);

            if (relationship == null) {
                // Relationship doesn't exist
                return;
            }

            // Soft delete the relationship
            relationship.DateDeleted = DateTime.UtcNow;
            _context.ContactXContactGroups.Update(relationship);
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}