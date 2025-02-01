using CRS.Services;

using static CRS.Helpers.LinkHelper;

namespace CRS.Models {
    public class Community : BaseModel {
        public required string Name { get; set; }
        public required List<Address> Addresses { get; set; } = [];
        public string HashedId => new HashingService().HashId(Id, HashType.Community);
    }
}
