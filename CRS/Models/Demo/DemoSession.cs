using System.ComponentModel.DataAnnotations;
using CRS.Models;

namespace CRS.Models.Demo
{
    /// <summary>
    /// Represents a demo session for potential customers to try the system
    /// </summary>
    public class DemoSession : BaseModel
    {
        /// <summary>
        /// Unique session identifier shown to user
        /// </summary>
        [Required]
        [StringLength(50)]
        public string SessionId { get; set; } = string.Empty;
        
        /// <summary>
        /// IP address of the demo user (for rate limiting)
        /// </summary>
        [StringLength(50)]
        public string IpAddress { get; set; } = string.Empty;
        
        /// <summary>
        /// When the demo session expires
        /// </summary>
        [Required]
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Last activity timestamp for inactivity tracking
        /// </summary>
        [Required]
        public DateTime LastActivityAt { get; set; }
        
        /// <summary>
        /// Whether the session is still active
        /// </summary>
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// ID of the demo tenant created for this session
        /// </summary>
        public Guid? DemoTenantId { get; set; }
        
        /// <summary>
        /// ID of the demo user created for this session
        /// </summary>
        [StringLength(450)]
        public string? DemoUserId { get; set; }
        
        /// <summary>
        /// Optional email if user provided it
        /// </summary>
        [StringLength(255)]
        public string? Email { get; set; }
        
        /// <summary>
        /// User agent string for analytics
        /// </summary>
        [StringLength(500)]
        public string? UserAgent { get; set; }
        
        /// <summary>
        /// Referrer URL
        /// </summary>
        [StringLength(500)]
        public string? Referrer { get; set; }
        
        /// <summary>
        /// Whether user converted to real account
        /// </summary>
        public bool ConvertedToRealAccount { get; set; } = false;
        
        /// <summary>
        /// When user converted (if they did)
        /// </summary>
        public DateTime? ConvertedAt { get; set; }
        
        /// <summary>
        /// Navigation to demo tenant
        /// </summary>
        public virtual Tenant? DemoTenant { get; set; }
    }
}
