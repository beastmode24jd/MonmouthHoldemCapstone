using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.Services
{
    public class BadgeService : IBadgeService
    {
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

            if (user.Badges.Any(b => b.BadgeID == newBadgeID))
            {
                // User already has this badge. Exit.
                return;
            }

            // Get the parameters of this new badge.
            var badgeTemplate = await GetBadgeDetails(newBadgeID);

            if (badgeTemplate != null)
            {
                // Catches invalid/unknown badgeID

                badgeTemplate.BadgeEarned = DateTime.UtcNow;
                user.Badges.Add(badgeTemplate);
                user.Points += badgeTemplate.PointValue;
            }

            // If the loop completes without badgeID found,
            // it will simply finish this task.
            await Task.CompletedTask;
            
        }

        // Helper method to retrieve badge data from LocalDB
        public async Task<Badge?> GetBadgeDetails(int newBadgeId)
        {
            // Looks for badge using ID from pool of badges in the DB
            return await _context.Set<Badge>().FirstOrDefaultAsync(b => b.BadgeID == newBadgeId);
        }

    }
}