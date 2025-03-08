using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using CRS.Data;

using Microsoft.AspNetCore.Identity;

namespace CRS.Models {
    public class Settings : BaseModel {
        [ForeignKey(nameof(ApplicationUser))] public  Guid ApplicationUserId { get; set; }
        public  ApplicationUser User { get; set; }
        public  string Class { get; set; }
        public  string Key { get; set; }
        public  string Value { get; set; }
        public  string Type { get; set; }
        public  string Context { get; set; }
    }
}
