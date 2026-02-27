using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Horizon.Services.Tenant;

namespace Horizon.Models {
    public class Message : ITenantScoped {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }

        [Required]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public DateTime DateSent { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Tenant scope
        public int TenantId { get; set; }

        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}