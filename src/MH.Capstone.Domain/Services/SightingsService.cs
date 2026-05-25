using MH.Capstone.Domain.Constants;
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
        private readonly IBadgeService _badgeService;
        private readonly ILiveBroadcastService _liveBroadcast;
        private readonly ILeaderboardService _leaderboardService;

        public SightingsService(
            ILogger<SightingsService> logger, IScoringService scoringService,
            INotificationService notificationService,
            IRepository<Sighting, ApplicationDbContext> sightingsRepo,
            IRepository<ApplicationUser, ApplicationDbContext> userRepo,
            IBadgeService badgeService,
            ILiveBroadcastService liveBroadcast,
            ILeaderboardService leaderboardService)
        {
            _logger = logger;
            _sightingsRepo = sightingsRepo;
            _scoringService = scoringService;
            _userRepo = userRepo;
            _notificationService = notificationService;
            _badgeService = badgeService;
            _liveBroadcast = liveBroadcast;
            _leaderboardService = leaderboardService;
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

            // CSP-177: idempotency — if the client re-submits a queued sighting that was
            // already stored (e.g. network retry), return the existing point value instead
            // of creating a duplicate.
            if (!string.IsNullOrEmpty(entity.ClientSightingId))
            {
                var existing = (await _sightingsRepo.GetAllAsync(s =>
                    s.UserIdentityId == entity.UserIdentityId &&
                    s.ClientSightingId == entity.ClientSightingId)).FirstOrDefault();
                if (existing is not null)
                    return existing.PointValue;
            }

            try
            {
                // CSP-104 / CSP-142: rarity is derived from the global count of sightings
                // sharing this species name. New uploads carry SpeciesName from the upload
                // form (manual entry or AI suggestion). Pre-CSP-142 rows are backfilled to
                // "Unknown", so they bucket together until they're re-classified.
                int globalCount = await _scoringService.GetGlobalSightingsCountAsync(entity.SpeciesName);

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

                    // Event-driven leaderboard broadcast — fires immediately after the point save
                    // so connected clients see the update in real time without polling.
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
                            "Leaderboard broadcast failed for user {UserId}; sighting submission is unaffected", user.Id);
                    }

                    // Save the point value of the Sighting, then save in DB.
                    entity.PointValue = pointsEarned;
                    await _sightingsRepo.AddOrUpdateAsync(entity);

                    // Materialize all user sightings once; derive both counts from the same list.
                    var userSightingList = (await _sightingsRepo.GetAllAsync(s => s.UserIdentityId == user.Id)).ToList();
                    int totalSightings = userSightingList.Count;
                    int uniqueAnimals = userSightingList
                        .GroupBy(s => s.SpeciesName, StringComparer.OrdinalIgnoreCase)
                        .Count();

                    await _badgeService.SyncBadgeProgressAsync(user, BadgeId.SightingNoviceBadgeGUID, totalSightings, ianaTimeZoneId);
                    await _badgeService.SyncBadgeProgressAsync(user, BadgeId.SightingStudentBadgeGUID, totalSightings, ianaTimeZoneId);
                    await _badgeService.SyncBadgeProgressAsync(user, BadgeId.AnidexBeginnerBadgeGUID, uniqueAnimals, ianaTimeZoneId);

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

        #region CSP-199: Paginated Community Gallery

        public async Task<PagedResult<Sighting>> GetSightingsPageAsync(int page, int pageSize)
        {
            // Clamp to sane lower bounds so a bad/absent query string can never produce
            // a negative Skip or a zero-size page.
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 1;

            // GetAllAsync() returns an IQueryable, so OrderBy/Skip/Take compose into the
            // SQL query — EF emits ORDER BY + OFFSET/FETCH and only this page's rows
            // (and their image bytes) leave the database. That is the actual fix for the
            // slow gallery: we stop materializing every sighting's image at once.
            var query = (await _sightingsRepo.GetAllAsync())
                .OrderByDescending(s => s.Timestamp);

            int totalCount = query.Count();

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            _logger.LogInformation(
                "Community gallery page {Page} (size {PageSize}) returned {Returned} of {Total} sightings",
                page, pageSize, items.Count, totalCount);

            return new PagedResult<Sighting>(items, totalCount, page, pageSize);
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

        #region CSP-37: Edit Sighting

        public async Task<Sighting?> UpdateSightingAsync(Guid sightingId, Guid userId, string description, string speciesName)
        {
            var sighting = await _sightingsRepo.FindByIdAsync(sightingId);
            if (sighting is null)
            {
                _logger.LogInformation("Edit requested for unknown sighting {SightingId}", sightingId);
                return null;
            }

            // Server-side ownership check — the hidden edit button is only a UX affordance.
            if (sighting.UserId != userId)
            {
                _logger.LogWarning(
                    "User {UserId} attempted to edit sighting {SightingId} owned by {OwnerId}",
                    userId, sightingId, sighting.UserId);
                throw new UnauthorizedAccessException(
                    $"User {userId} is not the owner of sighting {sightingId}.");
            }

            // Only the two user-correctable fields change. GPS, timestamp, photo, and all scoring
            // metadata (PointValue / Rarity / RarityMultiplier) are intentionally frozen — no
            // scoring service call, so a rename can never re-tier a sighting or game the leaderboard.
            sighting.Description = description;
            sighting.SpeciesName = string.IsNullOrWhiteSpace(speciesName) ? sighting.SpeciesName : speciesName.Trim();

            await _sightingsRepo.AddOrUpdateAsync(sighting);
            _logger.LogInformation("User {UserId} edited sighting {SightingId}", userId, sightingId);
            return sighting;
        }

        #endregion

        #region CSP-172: Sighting Details Page

        public async Task<Sighting?> GetSightingByIdAsync(Guid sightingId)
        {
            _logger.LogInformation("Fetching sighting {SightingId}", sightingId);
            var sighting = await _sightingsRepo.FindByIdAsync(sightingId);
            if (sighting is null)
            {
                _logger.LogInformation("Sighting {SightingId} not found.", sightingId);
            }
            return sighting;
        }

        #endregion

        #region CSP-142: Personal Anidex Collection

        public async Task<IEnumerable<AnidexEntry>> GetUserAnidexAsync(Guid userId)
        {
            _logger.LogInformation("Building Anidex for user {UserId}", userId);

            var userIdString = userId.ToString();
            var queryable = await _sightingsRepo.GetAllAsync(s => s.UserIdentityId == userIdString);
            var userSightings = queryable.ToList();

            if (userSightings.Count == 0)
            {
                _logger.LogInformation("User {UserId} has no sightings — returning empty Anidex.", userId);
                return [];
            }

            // Group by species (case-insensitive so "Coyote" and "coyote" merge into one entry).
            // Rarity is global, discovery count is per-user; the representative photo is the
            // user's most recent sighting of that species.
            var grouped = userSightings
                .GroupBy(s => s.SpeciesName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var entries = new List<AnidexEntry>(grouped.Count);
            foreach (var group in grouped)
            {
                var groupNewestFirst = group.OrderByDescending(s => s.Timestamp).ToList();
                var latest = groupNewestFirst[0];
                int globalCount = await _scoringService.GetGlobalSightingsCountAsync(latest.SpeciesName);
                var (multiplier, rarityName) = await _scoringService.GetRarityMultiplierAndName(globalCount);

                // CSP-202: preload per-sighting entries so card expansion doesn't round-trip.
                var perEntry = groupNewestFirst
                    .Select(s => new AnidexSightingEntry(s.Id, s.ImageBuffer, s.Description, s.Timestamp))
                    .ToList();

                entries.Add(new AnidexEntry(
                    SpeciesName: latest.SpeciesName,
                    DiscoveryCount: groupNewestFirst.Count,
                    RarityName: rarityName,
                    RarityMultiplier: multiplier,
                    LatestImageBuffer: latest.ImageBuffer,
                    LatestSightingTimestamp: latest.Timestamp,
                    Entries: perEntry));
            }

            // Sort: rarest first (highest multiplier), then alphabetical within tier.
            var ordered = entries
                .OrderByDescending(e => e.RarityMultiplier)
                .ThenBy(e => e.SpeciesName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            _logger.LogInformation("User {UserId} Anidex contains {Count} unique species.", userId, ordered.Count);
            return ordered;
        }

        #endregion
    }
}
