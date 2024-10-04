using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OpenSourceCalendar.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public bool SubscribedToNewsletter { get; set; }
    public string VerificationCode { get; set; }

    [NotMapped] // This attribute ensures the property is not stored in the database
    public bool HasExternalLogins { get; set; }

    [NotMapped]
    public string UserRole => HasExternalLogins ? "Admin" : "User"; // Used for display and sorting
}
