using CRS.Services;

using static CRS.Services.HashingService;

namespace CRS.Models {
    public class Community : BaseModel {
        public string? Name { get; set; }
        public List<Address>? Addresses { get; set; } = [];
    }
}
