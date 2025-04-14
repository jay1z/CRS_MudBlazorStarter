using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using CRS.Data;

namespace CRS.Services {
    public interface IUserStateService {
        ApplicationUser? CurrentUser { get; }
        ClaimsPrincipal? ClaimsPrincipal { get; }
        IList<string>? UserRoles { get; }
        bool IsAuthenticated { get; }
        bool IsInRole(string role);
        Task InitializeAsync();
        event Action? OnChange;
        Task RefreshStateAsync();
        Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName);
    }

    public class UserStateService : IUserStateService {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserStateService> _logger;
        private ApplicationUser? _currentUser;
        private ClaimsPrincipal? _claimsPrincipal;
        private IList<string>? _userRoles;
        private bool _isInitialized = false;

        public UserStateService(
            AuthenticationStateProvider authenticationStateProvider,
            UserManager<ApplicationUser> userManager,
            ILogger<UserStateService> logger) {
            _authenticationStateProvider = authenticationStateProvider;
            _userManager = userManager;
            _logger = logger;

            // Subscribe to authentication state changes
            _authenticationStateProvider.AuthenticationStateChanged += async _ => await RefreshStateAsync();
        }

        public ApplicationUser? CurrentUser => _currentUser;
        public ClaimsPrincipal? ClaimsPrincipal => _claimsPrincipal;
        public IList<string>? UserRoles => _userRoles;
        public bool IsAuthenticated => _claimsPrincipal?.Identity?.IsAuthenticated == true;

        public bool IsInRole(string role) =>
            _userRoles?.Contains(role, StringComparer.OrdinalIgnoreCase) == true;

        public async Task InitializeAsync() {
            if (_isInitialized) return;

            _logger.LogInformation("Initializing user state service");
            await UpdateStateFromAuthenticationAsync();
            _isInitialized = true;
        }

        public async Task RefreshStateAsync() {
            _logger.LogInformation("Refreshing user state");
            await UpdateStateFromAuthenticationAsync();
            NotifyStateChanged();
        }

        private async Task UpdateStateFromAuthenticationAsync() {
            try {
                var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated == true) {
                    _claimsPrincipal = authState.User;
                    _currentUser = await _userManager.GetUserAsync(authState.User);
                    if (_currentUser != null) {
                        _userRoles = await _userManager.GetRolesAsync(_currentUser);
                        _logger.LogInformation($"User {_currentUser.Id} authenticated with {_userRoles.Count} roles",
                            _currentUser.Id, _userRoles.Count);
                    }
                }
                else {
                    _claimsPrincipal = null;
                    _currentUser = null;
                    _userRoles = null;
                    _logger.LogInformation("No authenticated user");
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Error updating authentication state");
            }
        }

        public async Task<List<ApplicationUser>> GetUsersByRoleAsync(string roleName) {
            try {
                if (string.IsNullOrEmpty(roleName)) {
                    _logger.LogWarning("GetUsersByRoleAsync called with null or empty role name");
                    return new List<ApplicationUser>();
                }

                var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);
                _logger.LogInformation($"Found {usersInRole.Count} users in role {roleName}");
                return usersInRole.ToList();
            }
            catch (Exception ex) {
                _logger.LogError(ex, $"Error retrieving users in role {roleName}");
                return new List<ApplicationUser>();
            }
        }

        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
