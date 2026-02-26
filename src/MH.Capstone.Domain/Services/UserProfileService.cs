using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    public class UserProfileService :IUserProfileService
    {
        /* Update the user bio only if it is 250 characters or below.
        Removed from the ApplicationUser data model, for clarity and ease of
        EF Migrations and DB updates. */

        public async Task UpdateUserBio(ApplicationUser user, string? newBio)
        {
            if (!string.IsNullOrEmpty(newBio) && newBio.Length < 251)
            {
                user.Bio = newBio;
            }
            // Doesn't reset the Bio string's default, if it is over 250 char.
            
            // Will need to add DB logic here later
            // await _context.SaveChangesAsync();

            await Task.CompletedTask;
        }
    }
}