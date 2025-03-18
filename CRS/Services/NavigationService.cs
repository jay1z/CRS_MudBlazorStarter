using System.Security.Claims;

using CRS.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

public interface INavigationService
{
    Task NavigateToHomePageByRole(ClaimsPrincipal user);
}

public class NavigationService : INavigationService
{
    private readonly NavigationManager _navigationManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public NavigationService(NavigationManager navigationManager, UserManager<ApplicationUser> userManager)
    {
        _navigationManager = navigationManager;
        _userManager = userManager;
    }

    public async Task NavigateToHomePageByRole(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
            return;

        var appUser = await _userManager.GetUserAsync(user);
        var userRoles = await _userManager.GetRolesAsync(appUser);

        // Check if user is ONLY in the User role
        if (userRoles.Count == 1 && userRoles.Contains("User"))
        {
            _navigationManager.NavigateTo("/ReserveStudies", true);
        }
        else if (userRoles.Contains("Admin"))
        {
            _navigationManager.NavigateTo("/Dashboard", true);
        }
        else if (userRoles.Contains("Specialist"))
        {
            _navigationManager.NavigateTo("/ReserveStudies", true);
        }
        else
        {
            _navigationManager.NavigateTo("/Dashboard", true);
        }
    }
}
