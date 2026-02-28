using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    public class BadgeService : IBadgeService
    {
        /* Update the user bio only if it is 250 characters or below.
        Removed from the ApplicationUser data model, for clarity and ease of
        EF Migrations and DB updates. */

        private readonly ApplicationDbContext _context;

        public BadgeService(ApplicationDbContext context)
        {
            // Dependency Injection of DB Context
            _context = context;
        }

        public async Task AddBadge(ApplicationUser user, int newBadgeID)
        {

            // If user already has badge (check badgeID in user's badge list),
            //      does not add nor increment point count.

            foreach (badge in user.BadgeList)
            {
                if (badge.ID != newBadgeID)
                {
                    // Badge does not exist in user's current list. Add.
                    badge newBadge = await GetBadgeDetails(newBadgeID);
                    user.BadgeList.Add(newBadge);

                    // Increment the user's point count by how much the badge is worth.
                    user.Points += newBadge.PointValue;

                    // Save user changes to context.
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    
                }
            }

            // If the loop completes without badgeID being a new badge for the user,
            // it will simply finish this task.
            await Task.CompletedTask;
            
        }

        // Helper method to retrieve badge data from LocalDB?
        public async Task GetBadgeDetails(int BadgeID)
        {
            
        }

    }
}