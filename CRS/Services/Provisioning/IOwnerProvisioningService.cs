using Horizon.Data;
using Horizon.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Coravel.Mailer.Mail.Interfaces;
using Horizon.Models.Emails; // added for OwnerWelcomeEmail
using Coravel.Mailer.Mail;
using System.Net;
using Horizon.Models.Security;

namespace Horizon.Services.Provisioning {
    public interface IOwnerProvisioningService {
        Task<OwnerProvisionResult> ProvisionAsync(Horizon.Models.Tenant tenant, string? email, CancellationToken ct = default);
    }

    public enum OwnerProvisionResult { SkippedNoEmail, AlreadyExists, Created, Failed, RoleAdded }

    public class OwnerProvisioningService : IOwnerProvisioningService {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly ILogger<OwnerProvisioningService> _logger;
        private readonly ApplicationDbContext _db;
        private readonly IMailer _mailer;
        private readonly IConfiguration _config;

        public OwnerProvisioningService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, ILogger<OwnerProvisioningService> logger, ApplicationDbContext db, IMailer mailer, IConfiguration config) {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _db = db;
            _mailer = mailer;
            _config = config;
        }

        public async Task<OwnerProvisionResult> ProvisionAsync(Horizon.Models.Tenant tenant, string? email, CancellationToken ct = default) {
            if (string.IsNullOrWhiteSpace(email)) return OwnerProvisionResult.SkippedNoEmail;
            email = email.Trim();
            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null) {
                // Check if user already has TenantOwner role for this tenant
                var hasRole = await _db.UserRoleAssignments.AnyAsync(a => a.UserId == existing.Id && a.Role.Name == "TenantOwner" && a.TenantId == tenant.Id, ct);
                if (hasRole) return OwnerProvisionResult.AlreadyExists;
                // Add TenantOwner role for this tenant
                var role = await _db.Roles2.FirstOrDefaultAsync(r => r.Name == "TenantOwner", ct);
                if (role == null) {
                    role = new Role { Name = "TenantOwner", Scope = RoleScope.Tenant };
                    _db.Roles2.Add(role);
                    await _db.SaveChangesAsync(ct);
                }
                var assignment = new UserRoleAssignment { UserId = existing.Id, RoleId = role.Id, TenantId = tenant.Id };
                _db.UserRoleAssignments.Add(assignment);
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Added TenantOwner role for existing user {Email} to tenant {TenantId}", email, tenant.Id);
                return OwnerProvisionResult.RoleAdded;
            }
            // Generate a secure temporary password
            // For testing in Development, use a fixed password; otherwise generate a secure random one
            var isDevelopment = _config.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development";
            var tempPassword = isDevelopment ? "Letmeinnow1_" : GenerateSecureTemporaryPassword();
            
            var user = new ApplicationUser {
                Email = email,
                UserName = email,
                EmailConfirmed = true, // you can set false if you want separate confirmation
                TenantId = tenant.Id,
                FirstName = tenant.Name,
                LastName = "Owner"
            };
            
            // Create user WITH a temporary password
            var create = await _userManager.CreateAsync(user, tempPassword);
            if (!create.Succeeded) {
                _logger.LogWarning("Owner provision failed for tenant {TenantId}: {Errors}", tenant.Id, string.Join(", ", create.Errors.Select(e => e.Code)));
                return OwnerProvisionResult.Failed;
            }
            if (!await _roleManager.RoleExistsAsync("TenantOwner")) {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("TenantOwner"));
            }
            await _userManager.AddToRoleAsync(user, "TenantOwner");
            _logger.LogInformation("Owner user {Email} provisioned with temporary password for tenant {TenantId}", email, tenant.Id);

            // Send email with temporary password and link to change it
            try {
                var baseUrlRaw = _config["Application:BaseUrl"]?.TrimEnd('/') ?? $"https://{tenant.Subdomain}.{_config["App:RootDomain"]}";
                // Normalize base URL to scheme://host only (strip any path segments from configured value)
                string baseUrl;
                try {
                    var u = new Uri(baseUrlRaw);
                    baseUrl = $"{u.Scheme}://{u.Host}"; // ignore path/query from config
                } catch { baseUrl = baseUrlRaw; }
                var loginLink = $"{baseUrl}/Account/Login";
                var supportEmail = _config["Support:Email"] ?? "support@alxreservecloud.com";
                var viewModel = new OwnerWelcomeEmail {
                    TenantName = tenant.Name,
                    PasswordResetLink = loginLink, // Link to login page
                    TemporaryPassword = tempPassword, // Add temporary password
                    SubdomainUrl = baseUrl,
                    SupportEmail = supportEmail
                };
                await _mailer.SendAsync(Coravel.Mailer.Mail.Mailable.AsInline<OwnerWelcomeEmail>()
                    .To(email)
                    .Subject($"Welcome to {tenant.Name} - Your Account Details")
                    .View("~/Components/EmailTemplates/OwnerWelcome.cshtml", viewModel));
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Failed sending owner welcome email for tenant {TenantId}", tenant.Id);
            }

            return OwnerProvisionResult.Created;
        }
        
        /// <summary>
        /// Generates a secure temporary password that meets Identity password requirements
        /// </summary>
        private static string GenerateSecureTemporaryPassword()
        {
            // Generate a password that meets these requirements:
            // - At least 12 characters
            // - Contains digits, uppercase, lowercase, and non-alphanumeric
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            
            var random = new Random();
            var password = new char[16]; // 16 character password
            
            // Ensure at least one of each required character type
            password[0] = lowercase[random.Next(lowercase.Length)];
            password[1] = uppercase[random.Next(uppercase.Length)];
            password[2] = digits[random.Next(digits.Length)];
            password[3] = special[random.Next(special.Length)];
            
            // Fill the rest with random characters from all sets
            var allChars = lowercase + uppercase + digits + special;
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = allChars[random.Next(allChars.Length)];
            }
            
            // Shuffle the password to avoid predictable patterns
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }
            
            return new string(password);
        }
    }
}