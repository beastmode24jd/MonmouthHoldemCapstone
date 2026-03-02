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
        private readonly ApplicationDbContext _context;
        private readonly IRepository<Badge, ApplicationDbContext> _badgeRepo;
        private readonly IRepository<UserBadge, ApplicationDbContext> _userBadgeRepo;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;

        public BadgeService(ApplicationDbContext context,
        IRepository<Badge, ApplicationDbContext> badgeRepo,
        IRepository<UserBadge, ApplicationDbContext> userBadgeRepo,
        IRepository<ApplicationUser, ApplicationDbContext> userRepo)
        {
            // Dependency Injection of DB Context
            _context = context;

            // Switch Dependency Injection of DB context fully over to Repository structure
            _badgeRepo = badgeRepo;
            _userBadgeRepo = userBadgeRepo;
            _userRepo = userRepo;
        }

        public async Task AddBadge(ApplicationUser user, Guid newBadgeID)
        {
            // If this specific user already has this newBadgeID, exit.
            var alreadyExists = await _context.Set<UserBadge>()
                    .AnyAsync(ub => ub.UserId == user.Id && ub.BadgeId == newBadgeID);

            if (alreadyExists) { return; }

            // Get the parameters of this new badge.
            var badgeTemplate = await GetBadgeDetails(newBadgeID);

            // If-clause catches invalid/unknown badgeID
            if (badgeTemplate != null)
            {
                // Adds the new badge
                var earnedBadge = new UserBadge
                {
                    User = user,
                    UserId = user.Id,
                    BadgeId = badgeTemplate.BadgeID,
                    BadgeEarned = DateTime.UtcNow
                };

                // Increment points after adding the badge to the UserBadges list.
                user.Points += badgeTemplate.PointValue;
                await _userRepo.AddOrUpdateAsync(user);
                
                // Save the earnedBadge.
                await _userBadgeRepo.AddOrUpdateAsync(earnedBadge);

                //await _context.SaveChangesAsync();
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

        public async Task EnsureStandardBadgesCreated()
        {
            // This method gets called by Program.cs to create the default badges on runtime.

            // Check if the badges exist already
            var profileBadge = await GetBadgeDetails(Constants.BadgeId.ProfileBadgeGUID);
            var bioBadge = await GetBadgeDetails(Constants.BadgeId.CustomBioBadgeGUID);
            var firstSightingBadge = await GetBadgeDetails(Constants.BadgeId.FirstSightingBadgeGUID);

            if (profileBadge == null)
            {
                // No profileBadge was found in the local DB context.
                // So we add it
                await new Badge
                {
                    // Uses a consistent and constant ID for the badge
                    BadgeID = Constants.BadgeId.ProfileBadgeGUID,
                    Title = "Custom Profile Badge",
                    Description = "Uploaded a custom profile image.",
                    PointValue = 10
                    // Default profile image will be dealt with by frontend
                }.SaveModelAsync(_badgeRepo);
            }

            if (bioBadge == null)
            {
                await new Badge
                {
                   BadgeID = Constants.BadgeId.CustomBioBadgeGUID,
                   Title = "Custom Bio Badge",
                   Description = "Updated your profile with a custom description.",
                   PointValue = 10
                }.SaveModelAsync(_badgeRepo);
            }

            if (firstSightingBadge == null)
            {
                await new Badge
                {
                    BadgeID = Constants.BadgeId.FirstSightingBadgeGUID,
                    Title = "First Sighting Badge",
                    Description = "Uploaded your first Sighting!",
                    PointValue = 15
                }.SaveModelAsync(_badgeRepo);
            }
        }

    }
}