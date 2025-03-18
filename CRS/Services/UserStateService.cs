using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using CRS.Data;

namespace CRS.Services {
    public class UserStateService {
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly UserManager<ApplicationUser> _userManager;
        private ApplicationUser? _currentUser;
        private ClaimsPrincipal? _claimsPrincipal;
        private IList<string>? _userRoles;
        private bool _isInitialized = false;

        public UserStateService(
            AuthenticationStateProvider authenticationStateProvider,
            UserManager<ApplicationUser> userManager) {
            _authenticationStateProvider = authenticationStateProvider;
            _userManager = userManager;
        }

        public ApplicationUser? CurrentUser {
            get => _currentUser;
            set {
                _currentUser = value;
                NotifyStateChanged();
            }
        }

        public ClaimsPrincipal? ClaimsPrincipal {
            get => _claimsPrincipal;
            set {
                _claimsPrincipal = value;
                NotifyStateChanged();
            }
        }

        public IList<string>? UserRoles {
            get => _userRoles;
            set {
                _userRoles = value;
                NotifyStateChanged();
            }
        }

        public async Task InitializeAsync() {
            if (_isInitialized) return;

            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = factory.CreateLogger("UserStateService");
            logger.LogInformation("Hello World! Logging is {Description}.", "fun");

            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true) {
                ClaimsPrincipal = authState.User;
                CurrentUser = await _userManager.GetUserAsync(authState.User);
                if (CurrentUser != null) {
                    UserRoles = await _userManager.GetRolesAsync(CurrentUser);
                }
            }
            _isInitialized = true;
        }

        public event Action? OnChange;
        private void NotifyStateChanged() => OnChange?.Invoke();
    }
}
