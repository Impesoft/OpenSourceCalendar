using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenSourceCalendar.Data;

public class UserService(UserManager<ApplicationUser> _userManager) : IUserService
{

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        var result = await _userManager.Users.ToListAsync();
        foreach (var user in result)
        {
            var externalLogins = await _userManager.GetLoginsAsync(user);
            user.HasExternalLogins = externalLogins.Any();  // Store whether user has external logins
        }
        return result;
    }

    public async Task DeleteUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    public UserManager<ApplicationUser> GetUserManagerAsync()
    {
        return _userManager;
    }
}
