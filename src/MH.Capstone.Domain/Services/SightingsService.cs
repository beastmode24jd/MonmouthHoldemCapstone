using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MH.Capstone.Domain.Services
{
    public class SightingsService : ISightingsService
    {
        private readonly ILogger<SightingsService> _logger;
        private readonly IRepository<Sighting, ApplicationDbContext> _sightingsRepo;
        private readonly IRepository<ApplicationUser, ApplicationDbContext> _userRepo;
        private readonly IScoringService _scoringService;
        private readonly INotificationService _notificationService;

        public SightingsService(
            ILogger<SightingsService> logger, IScoringService scoringService,
            INotificationService notificationService,
            IRepository<Sighting, ApplicationDbContext> sightingsRepo,
            IRepository<ApplicationUser, ApplicationDbContext> userRepo)
        {
            _logger = logger;
            _sightingsRepo = sightingsRepo;
            _scoringService = scoringService;
            _userRepo = userRepo;
            _notificationService = notificationService;
        }

        public async Task<int> CreateSightingAsync(Sighting entity, string ianaTimeZoneId = "America/Los_Angeles")
        {
            if (!entity.TryValidateEntity(out var fails))
            {
                // There were one or more validation failures. Since this is a service method, we will throw an
                // exception to be handled by the caller and only care about the first failure for logging purposes.
                var firstFail = fails.First();
                throw new ArgumentException($"Sighting entity validation failed. Property {firstFail} invalid.",
                    firstFail);
            }

            try
            {
                // Calculate points based on rarity (CSP-104)
                // TODO: Replace hardcoded speciesId with actual species when Species table exists
                int globalCount = await _scoringService.GetGlobalSightingsCountAsync(1); // Placeholder species ID

                // Get the Rarity multiplier and name for the Sighting, map the metadata
                var (multiplier, rarityName) = await _scoringService.GetRarityMultiplierAndName(globalCount);
                entity.Rarity = rarityName;
                entity.RarityMultiplier = multiplier;

                // Get the initial point value of the Sighting (rarity multiplier applied to base)
                int pointsEarned = await _scoringService.CalculatePointsAsync(globalCount);

                // Get the user, to award points AND get login streak and final point calc
                var users = await _userRepo.GetAllAsync();
                var user = users.FirstOrDefault(u => u.Id == entity.UserIdentityId);

                if (user != null)
                {
                    entity.LoginStreak = user.IsStreakActive; // Save current loginStreak to Sighting

                    // Check if user has an active loginStreak.
                    // If so, apply 1.5 points multiplier to their pointsEarned.
                    if (user.IsStreakActive)
                    {
                        pointsEarned = (int)(pointsEarned * 1.5);
                    }

                    // Add and save the points to the user
                    user.Points += pointsEarned;
                    await _userRepo.AddOrUpdateAsync(user);

                    // Save the point value of the Sighting, then save in DB.
                    entity.PointValue = pointsEarned;
                    await _sightingsRepo.AddOrUpdateAsync(entity);

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
                    DateTimeOffset deviceTime = TimeZoneInfo.ConvertTime(entity.Timestamp, deviceZone);

                    // Generate the notification with the correct AM/PM and 12-hour format
                    string timeDisplay = deviceTime.ToString("MM/dd/yyyy h:mm tt");

                    await _notificationService.SendNotificationAsync(Notification.Create(user.GuidId,
                        "New Sighting Uploaded & Created!",
                        $"Congratulations! You uploaded a new sighting at {timeDisplay} and " +
                        $"earned {pointsEarned} points!"
                        ), NotificationType.NewSightingActivity);

                    _logger.LogInformation("Awarded {Points} points to user {UserId} for sighting", pointsEarned, entity.UserId);
                    
                    // AM and PM display mismatch catch logs
                    _logger.LogInformation("Raw DB Timestamp: {Raw}", entity.Timestamp);
                    _logger.LogInformation("Localized display: {Local}", timeDisplay);
                }

                // Return points to controller
                return pointsEarned;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
            {
                // This is a SQL foreign key violation, which means the UserId provided does not exist in the Users table.
                throw new ArgumentException(
                    $"Sighting entity validation failed. UserId {entity.UserId} does not exist.", nameof(entity.UserId),
                    ex);
            }
        }

        public async Task<IEnumerable<Sighting>> GetSightingsInBoundsAsync(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng)
        {
            var sevenDaysAgo = DateTimeOffset.UtcNow.AddDays(-7);

            var sightings = await _sightingsRepo.GetAllAsync(s => 
                            s.Latitude >= minLat &&
                            s.Latitude <= maxLat &&
                            s.Longitude >= minLng &&
                            s.Longitude <= maxLng &&
                            s.Timestamp >= sevenDaysAgo);
                

            return sightings.ToList();
        }

        public bool ValidateImage(IFormFile? imageBuffer)
        {
            // Null check - if null, we are not valid
            if (imageBuffer == null)
            {
                return false;
            }

            // Check file size (max 2 MB) but needs to not be 0
            if (imageBuffer.Length is > 2 * 1024 * 1024 or <= 0)
            {
                return false;
            }

            // Check file type (must be an JPG or PNG)
            string[] validImgTypes = ["jpg", "jpeg", "png"];
            if (!validImgTypes.Any(t => imageBuffer.ContentType.Contains($"image/{t}")))
            {
                return false;
            }

            // If we made it here, the image is valid
            return true;
        }

        #region CSP-96: Community Gallery

        public async Task<IEnumerable<Sighting>> GetAllSightingsAsync()
        {
            _logger.LogInformation("Fetching all sightings for community gallery");
            var sightings = await _sightingsRepo.GetAllAsync();
            var result = sightings.OrderByDescending(s => s.Timestamp).ToList();
            _logger.LogInformation("Retrieved {Count} sightings for community gallery", result.Count);
            return result;
        }

        #endregion

        #region CSP-145: Sighting Gallery Feature

        public async Task<IEnumerable<Sighting>> GetUserSightingsAsync(Guid userId)
        {
            _logger.LogInformation("Fetching sightings for user {UserId}", userId);

            // Use repository's predicate overload to filter, then order and fetch efficiently
            var queryable = await _sightingsRepo.GetAllAsync(s => s.UserIdentityId == userId.ToString());
            var sightings = queryable.OrderByDescending(s => s.Timestamp).ToList();
            _logger.LogInformation("Retrieved {Count} sightings for user {UserId}", sightings.Count, userId);

            return sightings;
        }

        #endregion
    }
}
