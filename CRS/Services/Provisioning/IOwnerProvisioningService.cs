using CRS.Data;
using CRS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Coravel.Mailer.Mail.Interfaces;
using CRS.Models.Emails; // added for OwnerWelcomeEmail
using Coravel.Mailer.Mail;
using System.Net;
using CRS.Models.Security;

namespace CRS.Services.Provisioning {
    public interface IOwnerProvisioningService {
        Task<OwnerProvisionResult> ProvisionAsync(CRS.Models.Tenant tenant, string? email, CancellationToken ct = default);
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

        public async Task<OwnerProvisionResult> ProvisionAsync(CRS.Models.Tenant tenant, string? email, CancellationToken ct = default) {
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
            var user = new ApplicationUser {
                Email = email,
                UserName = email,
                EmailConfirmed = true, // you can set false if you want separate confirmation
                TenantId = tenant.Id,
                FirstName = tenant.Name,
                LastName = "Owner"
            };
            // Create user WITHOUT an initial password so they must set one via Forgot Password
            var create = await _userManager.CreateAsync(user);
            if (!create.Succeeded) {
                _logger.LogWarning("Owner provision failed for tenant {TenantId}: {Errors}", tenant.Id, string.Join(", ", create.Errors.Select(e => e.Code)));
                return OwnerProvisionResult.Failed;
            }
            if (!await _roleManager.RoleExistsAsync("TenantOwner")) {
                await _roleManager.CreateAsync(new IdentityRole<Guid>("TenantOwner"));
            }
            await _userManager.AddToRoleAsync(user, "TenantOwner");
            _logger.LogInformation("Owner user {Email} provisioned (no password) for tenant {TenantId}", email, tenant.Id);

            // Send email with link to Forgot Password page (pre-filled email) so user sets initial password.
            try {
                var baseUrlRaw = _config["Application:BaseUrl"]?.TrimEnd('/') ?? $"https://{tenant.Subdomain}.{_config["App:RootDomain"]}";
                // Normalize base URL to scheme://host only (strip any path segments from configured value)
                string baseUrl;
                try {
                    var u = new Uri(baseUrlRaw);
                    baseUrl = $"{u.Scheme}://{u.Host}"; // ignore path/query from config
                } catch { baseUrl = baseUrlRaw; }
                var forgotLink = $"{baseUrl}/Account/ForgotPassword?email={System.Net.WebUtility.UrlEncode(email)}";
                var supportEmail = _config["Support:Email"] ?? "support@alxreservecloud.com";
                var viewModel = new OwnerWelcomeEmail {
                    TenantName = tenant.Name,
                    PasswordResetLink = forgotLink, // reuse property for button
                    SubdomainUrl = baseUrl,
                    SupportEmail = supportEmail
                };
                await _mailer.SendAsync(Coravel.Mailer.Mail.Mailable.AsInline<OwnerWelcomeEmail>()
                    .To(email)
                    .Subject($"Welcome to {tenant.Name} - Reset Password")
                    .View("~/Components/EmailTemplates/OwnerWelcome.cshtml", viewModel));
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Failed sending owner welcome (forgot password) email for tenant {TenantId}", tenant.Id);
            }

            return OwnerProvisionResult.Created;
        }
    }
}