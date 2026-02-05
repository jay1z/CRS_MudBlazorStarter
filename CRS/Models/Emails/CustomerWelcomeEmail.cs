namespace CRS.Models.Emails {
    /// <summary>
    /// Model for customer welcome email sent after self-registration
    /// </summary>
    public class CustomerWelcomeEmail {
        public required string CustomerName { get; set; }
        public required string ContactName { get; set; }
        public required string Email { get; set; }
        public required string TenantName { get; set; }
        public required string LoginUrl { get; set; }
        public required string RequestStudyUrl { get; set; }
        public string? SupportEmail { get; set; }
        public string? SupportPhone { get; set; }
    }
}
