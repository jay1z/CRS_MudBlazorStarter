using System.ComponentModel.DataAnnotations;

using CRS.Data;

using Microsoft.AspNetCore.Identity;

namespace CRS.Models {
    public class ContactGroup : BaseModel {
        public required ApplicationUser User { get; set; }
        public required string ApplicationUserId { get; set; }
        public required string Name { get; set; }
        [DataType(DataType.Text)] public string? Description { get; set; }
    }
}
