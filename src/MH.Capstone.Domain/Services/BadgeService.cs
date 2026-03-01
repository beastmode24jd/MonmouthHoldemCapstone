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

        public BadgeService(ApplicationDbContext context)
        {
            // Dependency Injection of DB Context
            _context = context;
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
                _context.Set<UserBadge>().Add(earnedBadge);

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

        public async Task EnsureStandardBadgesCreated()
        {
            // This method gets called by Program.cs to create the default badges on runtime.

            // Check if the badges exist already
            var profileBadge = await _context.Set<Badge>().FindAsync(Constants.BadgeId.ProfileBadgeGUID);
            var bioBadge = await _context.Set<Badge>().FindAsync(Constants.BadgeId.CustomBioBadgeGUID);
            var firstSightingBadge = await _context.Set<Badge>().FindAsync(Constants.BadgeId.FirstSightingBadgeGUID);

            if (profileBadge == null)
            {
                // No profileBadge was found in the local DB context.
                // So we add it
                _context.Set<Badge>().Add(new Badge
                {
                    // Uses a consistent and constant ID for the badge
                    BadgeID = Constants.BadgeId.ProfileBadgeGUID,
                    Title = "Custom Profile Badge",
                    Description = "Uploaded a custom profile image.",
                    PointValue = 10
                    // Default profile image will be dealt with by frontend
                });

                await _context.SaveChangesAsync();
            }

            if (bioBadge == null)
            {
                _context.Set<Badge>().Add(new Badge
                {
                   BadgeID = Constants.BadgeId.CustomBioBadgeGUID,
                   Title = "Custom Bio Badge",
                   Description = "Updated your profile with a custom description.",
                   PointValue = 10
                });

                await _context.SaveChangesAsync();
            }

            if (firstSightingBadge == null)
            {
                _context.Set<Badge>().Add(new Badge
                {
                    BadgeID = Constants.BadgeId.FirstSightingBadgeGUID,
                    Title = "First Sighting Badge",
                    Description = "Uploaded your first Sighting!",
                    PointValue = 15
                });

                await _context.SaveChangesAsync();
            }
        }

    }
}