using Microsoft.AspNetCore.Identity;

namespace MH.Capstone.Domain.DataModels
{
    // Represents a user in the application. Extends IdentityUser to include Identity features
    // like password hashing, email confirmation, two-factor auth, etc.
    public class ApplicationUser : IdentityUser
    {
        // Identity already provides: Id, Email, UserName, PasswordHash, etc.
        // Add custom properties if needed:

        // Example: public string? FirstName { get; set; }
        // Example: public string? LastName { get; set; }

        // Sets profile icon to default in wwwroot folder if not custom
        public byte[]? ProfileImage { get; set; }
    }
}