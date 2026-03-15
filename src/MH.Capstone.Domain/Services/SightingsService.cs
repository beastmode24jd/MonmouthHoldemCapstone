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
                // Step 1: Save the sighting to database
                await _sightingsRepo.AddOrUpdateAsync(entity);

                // Step 2: Calculate points based on rarity (CSP-104)
                // TODO: Replace hardcoded speciesId with actual species when Species table exists
                int globalCount = await _scoringService.GetGlobalSightingsCountAsync(1); // Placeholder species ID
                int pointsEarned = await _scoringService.CalculatePointsAsync(globalCount);

                // Step 3: Award points to the user
                var users = await _userRepo.GetAllAsync();
                var user = users.FirstOrDefault(u => u.Id == entity.UserIdentityId);
                
                if (user != null)
                {
                    user.Points += pointsEarned;
                    await _userRepo.AddOrUpdateAsync(user);

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
                        ));
                    _logger.LogInformation("Awarded {Points} points to user {UserId} for sighting", pointsEarned, entity.UserId);
                    // AM and PM display mismatch?
                    _logger.LogInformation("Raw DB Timestamp: {Raw}", entity.Timestamp);
                    _logger.LogInformation("Localized display: {Local}", timeDisplay);
                }

                // Step 4: Return points to controller
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
