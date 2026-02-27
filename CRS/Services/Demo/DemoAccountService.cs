using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Horizon.Data;
using Horizon.Models;
using Horizon.Models.Demo;
using Horizon.Models.Security;
using TenantModel = Horizon.Models.Tenant;

namespace Horizon.Services.Demo
{
    public interface IDemoAccountService
    {
        Task<DemoSession> CreateDemoSessionAsync(string ipAddress, string? userAgent, string? referrer);
        Task<DemoSession?> GetDemoSessionAsync(string sessionId);
        Task<bool> IsDemoModeAsync(string? userId);
        Task UpdateLastActivityAsync(string sessionId);
        Task<bool> ConvertToRealAccountAsync(string sessionId, string email, string password);
        Task<int> CleanupExpiredSessionsAsync();
        Task<bool> IsRateLimitedAsync(string ipAddress);
    }
    
    public class DemoAccountService : IDemoAccountService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DemoAccountService> _logger;
        private readonly IDemoDataSeedService _seedService;
        
        // Configuration
        private const int SessionExpirationHours = 24;
        private const int InactivityTimeoutHours = 2;
        private const int MaxDemoSessionsPerIp = 3;
        private const int RateLimitWindowHours = 24;
        
        public DemoAccountService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<DemoAccountService> logger,
            IDemoDataSeedService seedService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _seedService = seedService;
        }
        
        public async Task<DemoSession> CreateDemoSessionAsync(string ipAddress, string? userAgent, string? referrer)
        {
            try
            {
                // Check rate limit
                if (await IsRateLimitedAsync(ipAddress))
                {
                    _logger.LogWarning("Demo session creation rate limited for IP: {IpAddress}", ipAddress);
                    throw new InvalidOperationException("Too many demo sessions from this IP address. Please try again later.");
                }
                
                // Generate unique session ID
                var sessionId = GenerateSessionId();
                
                // Create demo user
                var demoUser = new ApplicationUser
                {
                    UserName = $"demo_{sessionId}",
                    Email = $"demo_{sessionId}@alxreservecloud.com",
                    EmailConfirmed = true,
                    IsDemo = true
                };
                
                var result = await _userManager.CreateAsync(demoUser, GenerateSecurePassword());
                
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create demo user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    throw new InvalidOperationException("Failed to create demo account.");
                }
                
                // Create demo tenant
                var demoTenant = new TenantModel
                {
                    Name = $"Demo Account - {sessionId}",
                    OwnerId = demoUser.Id.ToString(),
                    IsDemo = true,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Tenants.Add(demoTenant);
                await _context.SaveChangesAsync();
                
                // Assign user to tenant
                demoUser.TenantId = demoTenant.Id;
                await _userManager.UpdateAsync(demoUser);
                
                // Create demo session record
                var session = new DemoSession
                {
                    SessionId = sessionId,
                    IpAddress = ipAddress,
                    ExpiresAt = DateTime.UtcNow.AddHours(SessionExpirationHours),
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true,
                    DemoTenantId = demoTenant.Id,
                    DemoUserId = demoUser.Id,
                    UserAgent = userAgent,
                    Referrer = referrer
                };
                
                _context.DemoSessions.Add(session);
                await _context.SaveChangesAsync();
                
                // Seed demo data
                await _seedService.SeedDemoDataAsync(demoTenant.Id);
                
                _logger.LogInformation("Demo session created: {SessionId} for IP: {IpAddress}", sessionId, ipAddress);
                
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating demo session");
                throw;
            }
        }
        
        public async Task<DemoSession?> GetDemoSessionAsync(string sessionId)
        {
            return await _context.DemoSessions
                .Include(s => s.DemoTenant)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
        }
        
        public async Task<bool> IsDemoModeAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;
                
            var user = await _userManager.FindByIdAsync(userId);
            return user?.IsDemo ?? false;
        }
        
        public async Task UpdateLastActivityAsync(string sessionId)
        {
            var session = await _context.DemoSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
                
            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        
        public async Task<bool> ConvertToRealAccountAsync(string sessionId, string email, string password)
        {
            try
            {
                var session = await _context.DemoSessions
                    .Include(s => s.DemoTenant)
                    .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
                    
                if (session == null || session.DemoUserId == null)
                    return false;
                
                var demoUser = await _userManager.FindByIdAsync(session.DemoUserId.Value.ToString());
                if (demoUser == null)
                    return false;
                
                // Update user to real account
                demoUser.UserName = email;
                demoUser.Email = email;
                demoUser.IsDemo = false;
                demoUser.EmailConfirmed = false; // Require email confirmation
                
                var updateResult = await _userManager.UpdateAsync(demoUser);
                if (!updateResult.Succeeded)
                    return false;
                
                // Update password
                var token = await _userManager.GeneratePasswordResetTokenAsync(demoUser);
                var passwordResult = await _userManager.ResetPasswordAsync(demoUser, token, password);
                if (!passwordResult.Succeeded)
                    return false;
                
                // Update tenant
                if (session.DemoTenant != null)
                {
                    session.DemoTenant.IsDemo = false;
                    session.DemoTenant.Name = email; // User can change this later
                }
                
                // Update session
                session.ConvertedToRealAccount = true;
                session.ConvertedAt = DateTime.UtcNow;
                session.Email = email;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Demo session converted to real account: {SessionId} -> {Email}", sessionId, email);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting demo session to real account");
                return false;
            }
        }
        
        public async Task<int> CleanupExpiredSessionsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;
                var inactivityThreshold = now.AddHours(-InactivityTimeoutHours);
                
                // Find expired sessions (either past expiration time or inactive)
                var expiredSessions = await _context.DemoSessions
                    .Where(s => s.IsActive && 
                           (s.ExpiresAt < now || s.LastActivityAt < inactivityThreshold))
                    .Include(s => s.DemoTenant)
                    .ToListAsync();
                
                var count = 0;
                
                foreach (var session in expiredSessions)
                {
                    // Mark session as inactive
                    session.IsActive = false;
                    session.DateDeleted = DateTime.UtcNow;
                    
                    // Delete demo user
                    if (session.DemoUserId != null)
                    {
                        var user = await _userManager.FindByIdAsync(session.DemoUserId.Value.ToString());
                        if (user != null && user.IsDemo)
                        {
                            await _userManager.DeleteAsync(user);
                        }
                    }
                    
                    // Mark demo tenant as deleted (soft delete)
                    if (session.DemoTenant != null && session.DemoTenant.IsDemo)
                    {
                        session.DemoTenant.DateDeleted = DateTime.UtcNow;
                    }
                    
                    count++;
                }
                
                if (count > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Cleaned up {Count} expired demo sessions", count);
                }
                
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired demo sessions");
                return 0;
            }
        }
        
        public async Task<bool> IsRateLimitedAsync(string ipAddress)
        {
            var windowStart = DateTime.UtcNow.AddHours(-RateLimitWindowHours);
            
            var recentSessionCount = await _context.DemoSessions
                .Where(s => s.IpAddress == ipAddress && s.DateCreated >= windowStart)
                .CountAsync();
            
            return recentSessionCount >= MaxDemoSessionsPerIp;
        }
        
        private static string GenerateSessionId()
        {
            // Generate a secure random session ID (8 characters, URL-safe)
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[8];
            random.GetBytes(bytes);
            
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
        
        private static string GenerateSecurePassword()
        {
            // Generate a secure random password (16 characters)
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = RandomNumberGenerator.Create();
            var bytes = new byte[16];
            random.GetBytes(bytes);
            
            return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
        }
    }
}
