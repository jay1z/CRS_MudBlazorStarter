using CRS.Services;

using static CRS.Helpers.LinkHelper;

namespace CRS.Models {
    public class Community : BaseModel {
        public Community() {
            Addresses = new List<Address>() { new Address() };
        }
        public string? Name { get; set; }
        public List<Address>? Addresses { get; set; } = [];
        public string HashedId => new HashingService().HashId(Id, HashType.Community);
    }
}
