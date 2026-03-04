using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using MH.Capstone.Domain.Migrations;
using System.Reflection.Metadata;

namespace MH.Capstone.Domain.Services
{
    public class BadgeService : IBadgeService
    {
        private readonly IRepository<Badge, ApplicationDbContext> _badgeRepo;
        private readonly IRepository<UserBadge, ApplicationDbContext> _userBadgeRepo;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;

        public BadgeService(IRepository<Badge, ApplicationDbContext> badgeRepo,
        IRepository<UserBadge, ApplicationDbContext> userBadgeRepo,
        IRepository<ApplicationUser, ApplicationDbContext> userRepo)
        {
            // Switch Dependency Injection of DB context fully over to Repository structure
            _badgeRepo = badgeRepo;
            _userBadgeRepo = userBadgeRepo;
            _userRepo = userRepo;
        }

        public async Task AddBadge(ApplicationUser user, Guid newBadgeID)
        {
            // Get the list of user badges.
            var existingBadges = await _userBadgeRepo.GetAllAsync();

            // If this specific user already has this newBadgeID, exit.
            var alreadyExists = existingBadges.Any(ub => ub.UserId == user.Id && ub.BadgeId == newBadgeID);

            if (alreadyExists) { return; }

            // Get the parameters of this new badge.
            var badgeTemplate = await _badgeRepo.FindByIdAsync(newBadgeID);

            // If-clause catches invalid/unknown badgeID
            if (badgeTemplate != null)
            {
                // Adds the new join-table badge
                var earnedBadge = new UserBadge
                {
                    User = user,
                    UserId = user.Id,
                    BadgeId = badgeTemplate.BadgeID,
                    BadgeEarned = DateTime.UtcNow
                };

                // Save it
                await _userBadgeRepo.AddOrUpdateAsync(earnedBadge);

                // Increment points after adding the badge to the UserBadges list.
                user.Points += badgeTemplate.PointValue;
                await _userRepo.AddOrUpdateAsync(user);
                
            }

            // If the loop completes without badgeID found,
            // simply finish this task.
            await Task.CompletedTask;
            
        }

        // Helper method to retrieve badge data from LocalDB
        public async Task<Badge?> GetBadgeDetails(Guid newBadgeId)
        {
            // Looks for badge using ID from pool of badges in the DB
            // return await _context.Set<Badge>().FirstOrDefaultAsync(b => b.BadgeID == id);
            return await _badgeRepo.FindByIdAsync(newBadgeId);
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