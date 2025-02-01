using CRS.Data;

using Microsoft.AspNetCore.Identity;

namespace CRS.Models {
    public class Settings : BaseModel {
        public required ApplicationUser User { get; set; }
        public required string ApplicationUserId { get; set; }
        public required string Class { get; set; }
        public required string Key { get; set; }
        public required string Value { get; set; }
        public required string Type { get; set; }
        public required string Context { get; set; }
    }
}
