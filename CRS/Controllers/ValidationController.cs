using CRS.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CRS.Controllers {
    /// <summary>
    /// Public API for validating signup form fields (subdomain, email) availability
    /// Used by signup page for real-time validation before Stripe checkout
    /// </summary>
    [ApiController]
    [Route("api/validation")]
    public class ValidationController : ControllerBase {
        private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ValidationController> _logger;

        public ValidationController(
            IDbContextFactory<ApplicationDbContext> dbFactory,
            UserManager<ApplicationUser> userManager,
            ILogger<ValidationController> logger) {
            _dbFactory = dbFactory;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Check if a subdomain is available for registration
        /// </summary>
        /// <param name="subdomain">The subdomain to check (e.g., "acme")</param>
        /// <returns>JSON indicating if subdomain is available and any validation errors</returns>
        [HttpGet("subdomain/{subdomain}")]
        public async Task<IActionResult> CheckSubdomainAvailability(string subdomain, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(subdomain)) {
                return Ok(new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Subdomain is required"
                });
            }

            var normalized = subdomain.Trim().ToLowerInvariant();

            // Validate format
            if (!Regex.IsMatch(normalized, "^[a-z0-9-]{3,63}$")) {
                return Ok(new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Subdomain must be 3-63 characters: lowercase letters, numbers, and hyphens only"
                });
            }

            // Check for reserved subdomains
            var reserved = new[] { "www", "api", "app", "admin", "support", "help", "mail", "ftp", 
                                   "localhost", "staging", "dev", "test", "demo", "platform", "dashboard" };
            if (reserved.Contains(normalized)) {
                return Ok(new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = true,
                    Message = $"The subdomain '{normalized}' is reserved and cannot be used"
                });
            }

            // Check database
            try {
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var exists = await db.Tenants
                    .AsNoTracking()
                    .AnyAsync(t => t.Subdomain == normalized, ct);

                if (exists) {
                    _logger.LogInformation("Subdomain check: '{Subdomain}' is already taken", normalized);
                    return Ok(new SubdomainValidationResult {
                        IsAvailable = false,
                        IsValid = true,
                        Message = $"The subdomain '{normalized}' is already taken. Please choose another."
                    });
                }

                _logger.LogInformation("Subdomain check: '{Subdomain}' is available", normalized);
                return Ok(new SubdomainValidationResult {
                    IsAvailable = true,
                    IsValid = true,
                    Message = $"'{normalized}' is available!"
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking subdomain availability for '{Subdomain}'", normalized);
                return StatusCode(500, new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Unable to check subdomain availability. Please try again."
                });
            }
        }

        /// <summary>
        /// Check if an email address is available for registration
        /// </summary>
        /// <param name="email">The email address to check</param>
        /// <returns>JSON indicating if email is available and any validation errors</returns>
        [HttpGet("email")]
        public async Task<IActionResult> CheckEmailAvailability([FromQuery] string email, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(email)) {
                return Ok(new EmailValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Email address is required"
                });
            }

            var normalized = email.Trim().ToLowerInvariant();

            // Basic email format validation
            if (!Regex.IsMatch(normalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
                return Ok(new EmailValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Please enter a valid email address"
                });
            }

            // Check if email is already registered
            try {
                var existingUser = await _userManager.FindByEmailAsync(normalized);
                
                if (existingUser != null) {
                    _logger.LogInformation("Email check: '{Email}' is already registered", normalized);
                    return Ok(new EmailValidationResult {
                        IsAvailable = false,
                        IsValid = true,
                        Message = "This email address is already registered. Please use a different email or sign in."
                    });
                }

                // Also check pending tenant owner emails (in case tenant was created but user account pending)
                await using var db = await _dbFactory.CreateDbContextAsync(ct);
                var pendingTenant = await db.Tenants
                    .AsNoTracking()
                    .AnyAsync(t => t.PendingOwnerEmail != null && t.PendingOwnerEmail.ToLower() == normalized, ct);

                if (pendingTenant) {
                    _logger.LogInformation("Email check: '{Email}' is pending tenant owner registration", normalized);
                    return Ok(new EmailValidationResult {
                        IsAvailable = false,
                        IsValid = true,
                        Message = "This email address has a pending registration. Please check your email or contact support."
                    });
                }

                _logger.LogInformation("Email check: '{Email}' is available", normalized);
                return Ok(new EmailValidationResult {
                    IsAvailable = true,
                    IsValid = true,
                    Message = "Email is available"
                });
            } catch (Exception ex) {
                _logger.LogError(ex, "Error checking email availability for '{Email}'", normalized);
                return StatusCode(500, new EmailValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Unable to check email availability. Please try again."
                });
            }
        }

        /// <summary>
        /// Check both subdomain and email availability in a single request
        /// </summary>
        [HttpPost("check-availability")]
        public async Task<IActionResult> CheckAvailability([FromBody] AvailabilityCheckRequest request, CancellationToken ct) {
            var subdomainCheck = await CheckSubdomainAvailabilityInternal(request.Subdomain, ct);
            var emailCheck = await CheckEmailAvailabilityInternal(request.Email, ct);

            return Ok(new {
                subdomain = subdomainCheck,
                email = emailCheck,
                canProceed = subdomainCheck.IsAvailable && emailCheck.IsAvailable
            });
        }

        // Internal methods that don't return IActionResult (for combined check)
        private async Task<SubdomainValidationResult> CheckSubdomainAvailabilityInternal(string subdomain, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(subdomain)) {
                return new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Subdomain is required"
                };
            }

            var normalized = subdomain.Trim().ToLowerInvariant();

            if (!Regex.IsMatch(normalized, "^[a-z0-9-]{3,63}$")) {
                return new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Subdomain must be 3-63 characters: lowercase letters, numbers, and hyphens only"
                };
            }

            var reserved = new[] { "www", "api", "app", "admin", "support", "help", "mail", "ftp",
                                   "localhost", "staging", "dev", "test", "demo", "platform", "dashboard" };
            if (reserved.Contains(normalized)) {
                return new SubdomainValidationResult {
                    IsAvailable = false,
                    IsValid = true,
                    Message = $"The subdomain '{normalized}' is reserved"
                };
            }

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var exists = await db.Tenants.AsNoTracking().AnyAsync(t => t.Subdomain == normalized, ct);

            return new SubdomainValidationResult {
                IsAvailable = !exists,
                IsValid = true,
                Message = exists ? $"'{normalized}' is already taken" : $"'{normalized}' is available"
            };
        }

        private async Task<EmailValidationResult> CheckEmailAvailabilityInternal(string email, CancellationToken ct) {
            if (string.IsNullOrWhiteSpace(email)) {
                return new EmailValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Email is required"
                };
            }

            var normalized = email.Trim().ToLowerInvariant();

            if (!Regex.IsMatch(normalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) {
                return new EmailValidationResult {
                    IsAvailable = false,
                    IsValid = false,
                    Message = "Invalid email format"
                };
            }

            var existingUser = await _userManager.FindByEmailAsync(normalized);
            if (existingUser != null) {
                return new EmailValidationResult {
                    IsAvailable = false,
                    IsValid = true,
                    Message = "Email already registered"
                };
            }

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var pendingTenant = await db.Tenants.AsNoTracking()
                .AnyAsync(t => t.PendingOwnerEmail != null && t.PendingOwnerEmail.ToLower() == normalized, ct);

            return new EmailValidationResult {
                IsAvailable = !pendingTenant,
                IsValid = true,
                Message = pendingTenant ? "Email has pending registration" : "Email is available"
            };
        }
    }

    // DTOs
    public class SubdomainValidationResult {
        public bool IsAvailable { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class EmailValidationResult {
        public bool IsAvailable { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AvailabilityCheckRequest {
        public string Subdomain { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}
