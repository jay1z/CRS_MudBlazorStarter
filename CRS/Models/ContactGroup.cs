using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

using Microsoft.AspNetCore.Identity;

namespace CRS.Models {
    public class ContactGroup : BaseModel {
        public ApplicationUser User { get; set; }
        [ForeignKey(nameof(ApplicationUser))] public Guid ApplicationUserId { get; set; }
        public string Name { get; set; }
        [DataType(DataType.Text)] public string? Description { get; set; }
    }
}
