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

        private readonly ApplicationDbContext _context;

        public UserProfileService(ApplicationDbContext context)
        {
            // Dependency Injection of DB Context
            _context = context;
        }

        public async Task UpdateUserBio(ApplicationUser user, string? newBio)
        {
            if (!string.IsNullOrWhiteSpace(newBio) && newBio.Length < 251)
            {
                user.Bio = newBio;

                // Mark as changed, save to LocalDB.
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

            }
            
            // Doesn't reset the Bio string's default, if it is over 250 char.
            await Task.CompletedTask;
        }
    }
}