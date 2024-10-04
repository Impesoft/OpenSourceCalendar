using Microsoft.AspNetCore.Identity;
using OpenSourceCalendar.Data;

public interface IUserService
{
    Task DeleteUserAsync(string userId);
    Task<List<ApplicationUser>> GetAllUsersAsync();
    UserManager<ApplicationUser> GetUserManagerAsync();
}