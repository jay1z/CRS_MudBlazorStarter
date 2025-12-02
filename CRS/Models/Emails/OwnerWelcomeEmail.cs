namespace CRS.Models.Emails {
    public class OwnerWelcomeEmail {
        public required string TenantName { get; set; }
        public required string PasswordResetLink { get; set; }
        public string? TemporaryPassword { get; set; }
        public string? SubdomainUrl { get; set; }
        public string? SupportEmail { get; set; }
    }
}