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
        private readonly INotificationService _notificationService;

        public BadgeService(IRepository<Badge, ApplicationDbContext> badgeRepo,
        IRepository<UserBadge, ApplicationDbContext> userBadgeRepo,
        IRepository<ApplicationUser, ApplicationDbContext> userRepo,
        INotificationService notificationService)
        {
            // Switch Dependency Injection of DB context fully over to Repository structure
            _badgeRepo = badgeRepo;
            _userBadgeRepo = userBadgeRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
        }

        public async Task AddBadge(ApplicationUser user, Guid newBadgeID, string ianaTimeZoneId = "America/Los_Angeles")
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
                    BadgeEarned = DateTimeOffset.Now
                };

                // Save it
                await _userBadgeRepo.AddOrUpdateAsync(earnedBadge);

                // Increment points after adding the badge to the UserBadges list.

                // Check if the user has a valid Login Streak, and apply the 1.5 points
                // multipler if they do.
                var badgePointTotal = 0;
                if (user.IsStreakActive)
                {
                    badgePointTotal = (int)(badgeTemplate.PointValue * 1.5);
                    user.Points += badgePointTotal;
                }
                else
                {
                    // IsStreakActive is a bool value. This catches all other cases.
                    badgePointTotal = badgeTemplate.PointValue;
                    user.Points += badgePointTotal;
                }

                await _userRepo.AddOrUpdateAsync(user);

                // Then send the notification for the awarded Badge. *************
                // Convert timezone IANA ID to a TimeZoneInfo object
                TimeZoneInfo deviceZone;

                try
                {
                    // Converts the IANA ID successfully
                    deviceZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZoneId);
                }
                catch
                {
                    // Fallback to Windows-style Pacific ID if IANA fails on Windows Server
                    deviceZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
                }

                // Convert the timestamp to the device's actual zone
                DateTimeOffset deviceTime = TimeZoneInfo.ConvertTime((DateTimeOffset)earnedBadge.BadgeEarned, deviceZone);

                // Generate the notification with the correct AM/PM and 12-hour format
                string timeDisplay = deviceTime.ToString("MM/dd/yyyy h:mm tt");

                await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId,
                    $"Earned the {earnedBadge.Badge.Title}",
                    $"Congratulations! You earned the {earnedBadge.Badge.Title} at {timeDisplay} and " +
                    $"won {badgePointTotal} points!"
                    ));
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