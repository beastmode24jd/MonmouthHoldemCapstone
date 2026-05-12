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
        private readonly ILogger<BadgeService> _logger;
        private readonly ILiveBroadcastService _liveBroadcast;
        private readonly ILeaderboardService _leaderboardService;

        public BadgeService(IRepository<Badge, ApplicationDbContext> badgeRepo,
        IRepository<UserBadge, ApplicationDbContext> userBadgeRepo,
        IRepository<ApplicationUser, ApplicationDbContext> userRepo,
        INotificationService notificationService,
        ILogger<BadgeService> logger,
        ILiveBroadcastService liveBroadcast,
        ILeaderboardService leaderboardService)
        {
            // Switch Dependency Injection of DB context fully over to Repository structure
            _badgeRepo = badgeRepo;
            _userBadgeRepo = userBadgeRepo;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _logger = logger;
            _liveBroadcast = liveBroadcast;
            _leaderboardService = leaderboardService;
        }

        public async Task AddBadge(ApplicationUser user, Guid newBadgeID, string ianaTimeZoneId = "America/Los_Angeles")
        {
            // Get the parameters of this new badge.
            var badgeTemplate = await _badgeRepo.FindByIdAsync(newBadgeID);

            // If-clause catches invalid/unknown badgeID
            if (badgeTemplate == null) { return; }

            // Check if user already earned this Badge
            var userBadge = await _userBadgeRepo.FindAsync(ub => 
                ub.UserId == user.Id && ub.BadgeId == newBadgeID);

            // If they already have the badge (timestamp is set), do nothing
            if (userBadge?.BadgeEarned != null) 
            { 
                return; 
            }
            
            // Clause for single-step badges (ex. First Sighting), no previous entry
            if (userBadge == null)
            {
                userBadge = new UserBadge
                {
                    User = user,
                    UserId = user.Id,
                    BadgeId = badgeTemplate.BadgeID,
                };
            }
            
            // Finalize the badge
            userBadge.BadgeEarned = DateTimeOffset.Now;
            userBadge.BadgeProgress = badgeTemplate.BadgeSteps; // Ensure progress is maxed out

            // Save it
            await _userBadgeRepo.AddOrUpdateAsync(userBadge);

            // Increment user's points *************

            // Check if the user has a valid Login Streak, and apply the 1.5 points
            // multiplier if they do.
            var badgePointTotal = user.IsStreakActive ? (int)(badgeTemplate.PointValue * 1.5) : badgeTemplate.PointValue;

            user.Points += badgePointTotal;
            await _userRepo.AddOrUpdateAsync(user);

            // Event-driven leaderboard broadcast — fires immediately after the point save.
            try
            {
                int rank = await _leaderboardService.GetUserRankAsync(user.Id);
                await _liveBroadcast.BroadcastLeaderboardUpdateAsync(new LeaderboardEntryUpdate
                {
                    UserId = user.Id,
                    DisplayName = user.DisplayName,
                    Points = user.Points,
                    Rank = rank
                });
            }
            catch (Exception broadcastEx)
            {
                _logger.LogWarning(broadcastEx,
                    "Leaderboard broadcast failed for user {UserId}; badge award is unaffected", user.Id);
            }

            // Send the notification for the Badge. *************
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
            DateTimeOffset deviceTime = TimeZoneInfo.ConvertTime((DateTimeOffset)userBadge.BadgeEarned, deviceZone);

            // Generate the notification with the correct AM/PM and 12-hour format
            string timeDisplay = deviceTime.ToString("MM/dd/yyyy h:mm tt");

            await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId,
                $"Earned the {badgeTemplate.Title} Badge",
                $"Congratulations! You earned {badgeTemplate.Title} at {timeDisplay} and " +
                $"won {badgePointTotal} points!"
                ), NotificationType.BadgeAwarded);
        }

        public async Task UpdateBadge(ApplicationUser user, Guid badgeID, string ianaTimeZoneId = "America/Los_Angeles")
        {
            // Arguments will be used if user completes the Badge after running this method

            // Check if BadgeID is in valid Badge list ********************
            var badgeToUpdate = await _badgeRepo.FindByIdAsync(badgeID);

            // Reject invalid/unknown badgeIDs
            if (badgeToUpdate == null) { return; }

            // Exit if user already earned the Badge ******************
            var userBadges = await _userBadgeRepo.GetAllAsync();
            var userBadge = userBadges.FirstOrDefault(ub => ub.UserId == user.Id && ub.BadgeId == badgeID);

            // If badge is already earned (DateTimeOffset for BadgeEarned is set), exit
            if (userBadge != null && userBadge.BadgeEarned != null) return;

            // Update the BadgeProgress in the UserBadge **************

            // Create new UserBadge if it doesn't exist yet for multi-step progress
            if (userBadge == null)
            {
                userBadge = new UserBadge
                {
                    UserId = user.Id,
                    BadgeId = badgeID,
                    BadgeProgress = 0
                };
            }

            // Add a step to BadgeProgress
            userBadge.BadgeProgress++;

            // Recheck if BadgeProgress field is equal to BadgeSteps field *********

            if (userBadge.BadgeProgress >= badgeToUpdate.BadgeSteps)
            {
                // Badge completed, award it
                await AddBadge(user, badgeID, ianaTimeZoneId);
            }
            else
            {
                // Badge incomplete
                // Save current progress score
                await _userBadgeRepo.AddOrUpdateAsync(userBadge);
            }
        }

        public async Task SyncBadgeProgressAsync(ApplicationUser user, Guid badgeId, int actualCount, string ianaTimeZoneId = "America/Los_Angeles")
        {
            // Need to update older accounts with:
            //      - Anidex Beginner (5 unique animal entries saved)
            //      - Sighting Novice (5 Sightings)
            //      - Sighting Student (25 Sightings)

            var badgeTemplate = await _badgeRepo.FindByIdAsync(badgeId);
            if (badgeTemplate == null) return;

            // Fetch the user's current progress record
            var userBadges = await _userBadgeRepo.GetAllAsync();
            var userBadge = userBadges.FirstOrDefault(ub => ub.UserId == user.Id && ub.BadgeId == badgeId);

            // If already earned, exit
            if (userBadge?.BadgeEarned != null) return;

            // Case 1: Badge requirement hit
            if (actualCount >= badgeTemplate.BadgeSteps)
            {
                // AddBadge handles timestamp, progress, points, and notifications
                await AddBadge(user, badgeId, ianaTimeZoneId);
            }
            // Case 2: Badge progress made, not earned though
            else if (actualCount > (userBadge?.BadgeProgress ?? 0))
            {
                if (userBadge == null)
                {
                    userBadge = new UserBadge
                    {
                        UserId = user.Id,
                        BadgeId = badgeId,
                        BadgeProgress = actualCount
                    };
                }
                else
                {
                    userBadge.BadgeProgress = actualCount;
                }

                await _userBadgeRepo.AddOrUpdateAsync(userBadge);
            }
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