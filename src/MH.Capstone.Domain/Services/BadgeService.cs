using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

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

        public async Task AddBadge(ApplicationUser user, Guid newBadgeID)
        {

            // If user already has badge (check badgeID in user's badge list),
            //      does not add nor increment point count.

            if (user.UserBadges.Any(ub => ub.BadgeId == newBadgeID))
            {
                // User already has this badge. Exit.
                return;
            }

            // Get the parameters of this new badge.
            var badgeTemplate = await GetBadgeDetails(newBadgeID);

            // If-clause catches invalid/unknown badgeID
            if (badgeTemplate != null)
            {
                /*
                Commenting out for now. Will get back to this functionality later.

                if (badgeTemplate.BadgeIcon == null)
                {
                    badgeTemplate.BadgeIcon = await File.ReadAllBytesAsync("~/MH.Capstone.WebApp/imgs/BadgeIcon1.jpg");
                }
                */

                // Adds the new badge
                var earnedBadge = new UserBadge
                {
                    User = user,
                    UserId = user.Id,
                    BadgeId = badgeTemplate.BadgeID,
                    BadgeEarned = DateTime.UtcNow
                };

                // Increment points after adding the badge to the UserBadges list.
                user.UserBadges.Add(earnedBadge);
                user.Points += badgeTemplate.PointValue;

                await _context.SaveChangesAsync();
            }

            // If the loop completes without badgeID found,
            // simply finish this task.
            await Task.CompletedTask;
            
        }

        // Helper method to retrieve badge data from LocalDB
        public async Task<Badge?> GetBadgeDetails(Guid newBadgeId)
        {
            // Looks for badge using ID from pool of badges in the DB
            return await _context.Set<Badge>().FirstOrDefaultAsync(b => b.BadgeID == newBadgeId);
        }

        // Sorts the given list of UserBadges,
        // returns new UserBadge list in descending chronologic order.
        public async Task<List<UserBadge>> SortBadgesByTime(List<UserBadge> userBadges)
        {
            // Check if list is empty.
            if (userBadges == null || !userBadges.Any())
            {
                // Default to empty list.
                return new List<UserBadge>();
            }

            // Uses LINQ to sort, retaining original badge list structure in ApplicationUser.
            var sortedList = userBadges
                    .OrderByDescending(ub => ub.BadgeEarned)
                    .ToList();

            // Return the sorted list.
            return await Task.FromResult(sortedList);
        }

    }
}