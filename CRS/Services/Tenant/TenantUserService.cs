using CRS.Data;
using CRS.Models.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;

namespace CRS.Services.Tenant {
    public class TenantUserService : ITenantUserService {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly ILogger<TenantUserService> _logger;
        private readonly IEmailSender<ApplicationUser> _emailSender;
        private readonly IConfiguration _config;
        private const string DefaultTempPassword = "Letmeinnow1_";

        public TenantUserService(UserManager<ApplicationUser> userManager,
                                 RoleManager<IdentityRole<Guid>> roleManager,
                                 IDbContextFactory<ApplicationDbContext> dbFactory,
                                 ILogger<TenantUserService> logger,
                                 IEmailSender<ApplicationUser> emailSender,
                                 IConfiguration config) {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbFactory = dbFactory;
            _logger = logger;
            _emailSender = emailSender;
            _config = config;
        }

        public async Task<ApplicationUser?> CreateTenantUserAsync(string email, string password, string firstName, string lastName, int tenantId, string roleName) {
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email required", nameof(email));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("password required", nameof(password));
            if (tenantId <= 0) throw new ArgumentException("tenantId required", nameof(tenantId));

            var existing = await _userManager.FindByEmailAsync(email);
            if (existing != null) {
                _logger.LogWarning("CreateTenantUserAsync: user {Email} already exists", email);
                return null;
            }

            // For testing: force a consistent temporary password to avoid manual resets
            password = DefaultTempPassword;

            var user = new ApplicationUser {
                UserName = email,
                Email = email,
                EmailConfirmed = false,
                FirstName = firstName,
                LastName = lastName,
                TenantId = tenantId
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded) {
                _logger.LogError("Failed creating tenant user {Email}: {Errors}", email, string.Join(",", result.Errors.Select(e => e.Description)));
                return null;
            }

            // Ensure Identity role exists and add to Identity roles (for legacy checks)
            if (!await _roleManager.RoleExistsAsync(roleName)) {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
            }
            var addRole = await _userManager.AddToRoleAsync(user, roleName);
            if (!addRole.Succeeded) {
                _logger.LogWarning("CreateTenantUserAsync: failed to add identity role {Role} to {Email}: {Errors}", roleName, email, string.Join(',', addRole.Errors.Select(e => e.Code)));
            }

            // Create custom role assignment
            try {
                await using var db = await _dbFactory.CreateDbContextAsync();
                var customRole = await db.Roles2.FirstOrDefaultAsync(r => r.Name == roleName);
                if (customRole == null) {
                    customRole = new Role { Name = roleName, Scope = RoleScope.Tenant };
                    db.Roles2.Add(customRole);
                    await db.SaveChangesAsync();
                }

                db.Users.Attach(new ApplicationUser { Id = user.Id, UserName = user.UserName, Email = user.Email, TenantId = user.TenantId });
                db.UserRoleAssignments.Add(new UserRoleAssignment { UserId = user.Id, RoleId = customRole.Id, TenantId = tenantId });
                await db.SaveChangesAsync();
            } catch (Exception ex) {
                _logger.LogError(ex, "Error creating custom role assignment for {Email}", email);
            }

            // Send invitation (email confirmation + password reset link)
            try {
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                // Use Base64Url encoding to match ConfirmEmail and ResetPassword pages
                var encodedConfirmation = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(confirmationToken));
                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var encodedReset = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(resetToken));

                // Build base URL using tenant subdomain
                await using var dbLookup = await _dbFactory.CreateDbContextAsync();
                var tenant = await dbLookup.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId);
                var rootDomain = _config["App:RootDomain"]?.Trim('.');
                var baseUrl = (!string.IsNullOrWhiteSpace(rootDomain) && tenant != null)
                    ? $"https://{tenant.Subdomain}.{rootDomain}" : _config["Application:BaseUrl"] ?? "https://localhost";

                var confirmLink = $"{baseUrl}/Account/ConfirmEmail?userId={user.Id}&code={encodedConfirmation}";
                var resetLink = $"{baseUrl}/Account/ResetPassword?userId={user.Id}&code={encodedReset}";
                await _emailSender.SendConfirmationLinkAsync(user, email, confirmLink);
                await _emailSender.SendPasswordResetLinkAsync(user, email, resetLink);
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Failed sending invite emails to {Email}", email);
            }

            return user;
        }

        public async Task<IdentityResult> UpdateUserAsync(ApplicationUser user) {
            if (user == null) throw new ArgumentNullException(nameof(user));
            
            try {
                return await _userManager.UpdateAsync(user);
            } catch (Exception ex) {
                _logger.LogError(ex, "Error updating user {UserId}", user.Id);
                throw;
            }
        }

        // Random password generator retained for future use
        private static string GenerateRandomPassword() {
            const string lowers = "abcdefghijkmnopqrstuvwxyz";
            const string uppers = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string digits = "23456789";
            const string specials = "!@$%^*?_";
            var rnd = System.Random.Shared;
            char Pick(string s) => s[rnd.Next(s.Length)];
            var chars = new List<char> { Pick(lowers), Pick(uppers), Pick(digits), Pick(specials) };
            string all = lowers + uppers + digits + specials;
            while (chars.Count < 12) chars.Add(Pick(all));
            return new string(chars.OrderBy(_ => rnd.Next()).ToArray());
        }
    }
}
