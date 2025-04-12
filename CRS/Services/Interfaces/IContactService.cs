using CRS.Models;

namespace CRS.Services.Interfaces {
    public interface IContactService {
        // Contact Methods
        Task<List<Contact>> GetUserContactsAsync(string userId);
        Task<List<ServiceContact>> GetServiceContactsAsync();
        Task<Contact> GetContactByIdAsync(Guid contactId);
        Task<ServiceContact> GetServiceContactByIdAsync(Guid contactId);
        Task<Contact> CreateContactAsync(Contact contact);
        Task<ServiceContact> CreateServiceContactAsync(ServiceContact contact);
        Task<Contact> UpdateContactAsync(Contact contact);
        Task<ServiceContact> UpdateServiceContactAsync(ServiceContact contact);
        Task DeleteContactAsync(Guid contactId);
        Task DeleteServiceContactAsync(Guid contactId);

        // Contact Group Methods
        Task<List<ContactGroup>> GetContactGroupsAsync(string userId);
        Task<ContactGroup> GetContactGroupByIdAsync(Guid groupId);
        Task<List<Contact>> GetContactsByGroupIdAsync(Guid groupId);
        Task<ContactGroup> CreateContactGroupAsync(ContactGroup group);
        Task<ContactGroup> UpdateContactGroupAsync(ContactGroup group);
        Task DeleteContactGroupAsync(Guid groupId);
        Task AddContactToGroupAsync(Guid contactId, Guid groupId);
        Task RemoveContactFromGroupAsync(Guid contactId, Guid groupId);
    }
}
