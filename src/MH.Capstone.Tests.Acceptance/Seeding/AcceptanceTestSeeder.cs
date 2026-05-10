using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BadgeConstants = MH.Capstone.Domain.Constants.BadgeId;

namespace MH.Capstone.Tests.Acceptance.Seeding;

/// <summary>
/// Seeds the acceptance test database (<c>WAID_AcceptanceDb</c>) with a known,
/// stable set of fixtures.
///
/// <para>
/// <see cref="SeedAsync"/> first wipes <em>every row</em> from all application
/// tables using SQL DELETE statements (the database and its schema are never
/// touched), then inserts the complete fixture set.  It is safe to call multiple
/// times — each call leaves the database in exactly the same state.
/// </para>
///
/// <para>
/// All primary keys are fixed GUIDs so that foreign-key references remain
/// consistent across re-seeds and across test runs.
/// </para>
///
/// <para>
/// Personas are defined in <c>docs/acceptance-testing.md</c>.  Only the accounts
/// listed there are seeded here:
/// <list type="bullet">
///   <item><description>Alex (<c>alex@test.com</c>) — standard User</description></item>
///   <item><description>Patricia (<c>patricia@test.com</c>) — Admin + User</description></item>
///   <item><description>Lily (<c>lily@test.com</c>) — second standard User</description></item>
///   <item><description>James — unauthenticated visitor; <em>no database account</em></description></item>
/// </list>
/// </para>
/// </summary>
[ExcludeFromCodeCoverage]
internal static class AcceptanceTestSeeder
{
    // =========================================================================
    // Stable, well-known GUIDs
    // Exposed as internal so step definitions can reference specific records.
    // =========================================================================

    // -- Users ----------------------------------------------------------------
    /// <summary>Alex — standard User.  Primary persona for user-facing scenarios.</summary>
    internal static readonly Guid AlexUserId     = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    /// <summary>Patricia — Admin + User.  Used for admin-only and elevated-permission scenarios.</summary>
    internal static readonly Guid PatriciaUserId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    /// <summary>Lily — second standard User.  Used for multi-user interaction scenarios.</summary>
    internal static readonly Guid LilyUserId     = new("cccccccc-cccc-cccc-cccc-cccccccccccc");

    /// <summary>Owen — standard User with DisplayName == "UNSET". Used for forced display-name *completion* scenario (CSP-168).</summary>
    internal static readonly Guid OwenUserId     = new("dddddddd-dddd-dddd-dddd-dddddddddddd");

    /// <summary>Faye — standard User with DisplayName == "UNSET". Used for the forced display-name *redirect* check scenario (CSP-168).
    /// Kept separate from Owen so the two scenarios do not share mutable state.</summary>
    internal static readonly Guid FayeUserId     = new("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    // James has no database account — he represents an unauthenticated visitor.

    // -- Roles (stored as strings to match ASP.NET Identity) ------------------
    private const string UserRoleId  = "11111111-1111-1111-1111-111111111111";
    private const string AdminRoleId = "22222222-2222-2222-2222-222222222222";

    // -- Shared test credential (satisfies Identity password policy) ----------
    //    min 8 chars · uppercase · lowercase · digit · non-alphanumeric
    internal const string TestPassword = "Capstone26!";

    // =========================================================================
    // Public entry point
    // =========================================================================

    /// <summary>
    /// Wipes all application table rows and re-inserts the complete acceptance
    /// test fixture set.
    /// </summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken token = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearAllAsync(db, token);
        await SeedRolesAsync(db, token);
        await SeedBadgesAsync(db, token);
        await SeedUsersAsync(db, token);
        await SeedUserRolesAsync(db, token);
        await SeedSightingsAsync(db, token);
        await SeedUserBadgesAsync(db, token);
        await SeedNotificationsAsync(db, token);
        await SeedReportsAsync(db, token);
    }

    // =========================================================================
    // Wipe — DELETE in FK-safe order.  No DROP TABLE, no database deletion.
    // =========================================================================

    private static async Task ClearAllAsync(ApplicationDbContext db, CancellationToken token)
    {
        // 1. Leaf tables — nothing else has an FK pointing into these.
        await db.EmailQueue.ExecuteDeleteAsync(token);
        await db.Reports.ExecuteDeleteAsync(token);
        await db.Notifications.ExecuteDeleteAsync(token);
        await db.UserBadges.ExecuteDeleteAsync(token);

        // CSP-187: moderation log -> Comment -> Sighting; clear logs first, then comments,
        // then sightings (existing line). Follow + block tables are independent leaves.
        await db.CommentModerationLogs.ExecuteDeleteAsync(token);
        await db.Comments.ExecuteDeleteAsync(token);
        await db.UserFollows.ExecuteDeleteAsync(token);
        await db.UserBlocks.ExecuteDeleteAsync(token);

        await db.Sightings.ExecuteDeleteAsync(token);

        // 2. Club tables — Messages and ClubMemberships reference both Club and User;
        //    Clubs references User via OwnerId. Clear all three before touching Users.
        await db.Messages.ExecuteDeleteAsync(token);
        await db.ClubMemberships.ExecuteDeleteAsync(token);
        await db.Clubs.ExecuteDeleteAsync(token);

        // 3. Notification preferences — FK → User.
        await db.UserNotificationPreferences.ExecuteDeleteAsync(token);

        // 4. ASP.NET Identity junction tables (FK → AspNetUsers and/or AspNetRoles).
        await db.UserTokens.ExecuteDeleteAsync(token);
        await db.UserLogins.ExecuteDeleteAsync(token);
        await db.UserClaims.ExecuteDeleteAsync(token);
        await db.UserRoles.ExecuteDeleteAsync(token);

        // 5. Root identity tables — cleared last because the junction tables above
        //    held FKs into them.
        await db.Users.ExecuteDeleteAsync(token);
        await db.Badges.ExecuteDeleteAsync(token);
        await db.RoleClaims.ExecuteDeleteAsync(token);
        await db.Roles.ExecuteDeleteAsync(token);
    }

    // =========================================================================
    // Seed — Roles
    // =========================================================================

    private static async Task SeedRolesAsync(ApplicationDbContext db, CancellationToken token)
    {
        db.Roles.AddRange(
            new IdentityRole { Id = UserRoleId,  Name = "User",  NormalizedName = "USER"  },
            new IdentityRole { Id = AdminRoleId, Name = "Admin", NormalizedName = "ADMIN" }
        );
        await db.SaveChangesAsync(token);
    }

    // =========================================================================
    // Seed — Badges
    // Mirrors the three badges normally seeded by ApplicationDbContextSeeding,
    // using the same well-known GUIDs from BadgeId constants.
    // =========================================================================

    private static async Task SeedBadgesAsync(ApplicationDbContext db, CancellationToken token)
    {
        db.Badges.AddRange(
            new Badge
            {
                BadgeID     = BadgeConstants.ProfileBadgeGUID,
                Title       = "Custom Profile Badge",
                Description = "Uploaded a custom profile image.",
                PointValue  = 10,
            },
            new Badge
            {
                BadgeID     = BadgeConstants.CustomBioBadgeGUID,
                Title       = "Custom Bio Badge",
                Description = "Updated your profile with a custom description.",
                PointValue  = 10,
            },
            new Badge
            {
                BadgeID     = BadgeConstants.FirstSightingBadgeGUID,
                Title       = "First Sighting Badge",
                Description = "Uploaded your first Sighting!",
                PointValue  = 25,
            }
        );
        await db.SaveChangesAsync(token);
    }

    // =========================================================================
    // Seed — Users
    //
    // Personas (defined in docs/acceptance-testing.md)
    // -------------------------------------------------
    // Alex      Standard user.  Primary persona for all user-facing scenarios.
    //           Has sightings, one badge, and a mix of read/unread notifications.
    //
    // Patricia  Admin + User.  Used for admin-only pages and report moderation.
    //           No sightings — keeps her state minimal and predictable for admin
    //           scenarios that should not be affected by sighting data.
    //           Holds both roles so her Admin access and normal User access can
    //           each be verified.
    //
    // Lily      Second standard user.  Distinct point total from Alex so that
    //           leaderboard ordering between users can be asserted.  Has sightings
    //           across different Oregon locations from Alex, and all three badges,
    //           enabling "viewing another user's content" scenarios.
    //
    // James     Unauthenticated visitor — no database account is seeded.
    //           Represented in step definitions by logging out before the scenario.
    // =========================================================================

    private static async Task SeedUsersAsync(ApplicationDbContext db, CancellationToken token)
    {
        var hasher = new PasswordHasher<ApplicationUser>();

        db.Users.AddRange(
            // Alex — leaderboard rank #2, active login streak
            MakeUser(AlexUserId, "alex@test.com", hasher,
                displayName: "Alex",
                points: 75,
                bio: "Wildlife enthusiast from Monmouth, OR.",
                loginStreak: 5,
                lastLoginDaysAgo: 1),

            // Patricia — admin account, minimal data for clean admin scenarios
            MakeUser(PatriciaUserId, "patricia@test.com", hasher,
                displayName: "Patricia",
                points: 0,
                bio: "System administrator.",
                loginStreak: 0,
                lastLoginDaysAgo: 1),

            // Lily — leaderboard rank #1, all three badges, longer history
            MakeUser(LilyUserId, "lily@test.com", hasher,
                displayName: "Lily",
                points: 200,
                bio: "Passionate nature photographer and conservationist.",
                loginStreak: 10,
                lastLoginDaysAgo: 1),

            // Owen — DisplayName == "UNSET"; used for the *completion* scenario (sets name during test)
            MakeUser(OwenUserId, "owen@test.com", hasher,
                displayName: "UNSET",
                points: 0,
                bio: null,
                loginStreak: 0,
                lastLoginDaysAgo: null),

            // Faye — DisplayName == "UNSET"; used only for the *redirect check* scenario (never sets name)
            MakeUser(FayeUserId, "faye@test.com", hasher,
                displayName: "UNSET",
                points: 0,
                bio: null,
                loginStreak: 0,
                lastLoginDaysAgo: null)
        );

        await db.SaveChangesAsync(token);
    }

    private static ApplicationUser MakeUser(
        Guid id,
        string email,
        PasswordHasher<ApplicationUser> hasher,
        string displayName,
        int points,
        string? bio,
        int loginStreak,
        int? lastLoginDaysAgo)
    {
        var normalized = email.ToUpperInvariant();
        var user = new ApplicationUser
        {
            Id                 = id.ToString(),
            UserName           = email,
            NormalizedUserName = normalized,
            Email              = email,
            NormalizedEmail    = normalized,
            EmailConfirmed     = true,
            SecurityStamp      = Guid.NewGuid().ToString("D"),
            ConcurrencyStamp   = Guid.NewGuid().ToString("D"),
            DisplayName        = displayName,
            Points             = points,
            Bio                = bio,
            LoginStreak        = loginStreak,
            LastLogin          = lastLoginDaysAgo.HasValue
                                     ? DateTimeOffset.UtcNow.AddDays(-lastLoginDaysAgo.Value)
                                     : null,
            IsDeactivated      = false,
        };
        user.PasswordHash = hasher.HashPassword(user, TestPassword);
        return user;
    }

    // =========================================================================
    // Seed — User ↔ Role assignments
    // Patricia holds both Admin and User roles per the persona definition.
    // =========================================================================

    private static async Task SeedUserRolesAsync(ApplicationDbContext db, CancellationToken token)
    {
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = AlexUserId.ToString(),     RoleId = UserRoleId  },
            new IdentityUserRole<string> { UserId = PatriciaUserId.ToString(), RoleId = AdminRoleId },
            new IdentityUserRole<string> { UserId = PatriciaUserId.ToString(), RoleId = UserRoleId  },
            new IdentityUserRole<string> { UserId = LilyUserId.ToString(),     RoleId = UserRoleId  },
            new IdentityUserRole<string> { UserId = OwenUserId.ToString(),     RoleId = UserRoleId  },
            new IdentityUserRole<string> { UserId = FayeUserId.ToString(),     RoleId = UserRoleId  }
        );
        await db.SaveChangesAsync(token);
    }

    // =========================================================================
    // Seed — Sightings
    //
    // Geographic coverage
    // -------------------
    // Alex (3):  Willamette Valley / WOU campus area — all within a tight
    //            Oregon bounding box; good for map viewport tests.
    //            One entry has a null description to exercise optional-field
    //            rendering paths in the gallery and detail views.
    //
    // Lily (5):  Spread across Oregon (Crater Lake, Portland, Eugene, Silver
    //            Falls, Lincoln City coast) plus one outside Oregon (Los Angeles)
    //            so map-bounds filtering tests can verify out-of-range sightings
    //            are excluded.
    //
    // Patricia:  No sightings — admin scenarios should not be affected by
    //            sighting data.
    // =========================================================================

    private static async Task SeedSightingsAsync(ApplicationDbContext db, CancellationToken token)
    {
        var img = new byte[] { 0x01 }; // 1-byte placeholder satisfies the [Length(1, 2MB)] constraint

        db.Sightings.AddRange(

            // ── Alex ─────────────────────────────────────────────────────────

            new Sighting(
                id:          new Guid("a1000000-0000-0000-0000-000000000001"),
                userId:      AlexUserId,
                latitude:    44.847600m,
                longitude:  -123.234300m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-10),
                description: "Great blue heron standing motionless at the WOU campus pond.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Great Blue Heron" },

            new Sighting(
                id:          new Guid("a1000000-0000-0000-0000-000000000002"),
                userId:      AlexUserId,
                latitude:    44.943000m,
                longitude:  -123.035100m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-25),
                description: "Bald eagle circling above the Willamette River near Salem.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Bald Eagle" },

            // null description — exercises optional-field rendering.
            // CSP-142: same species as the first sighting so Alex's Anidex
            // shows a discovery count of 2 for "Great Blue Heron".
            new Sighting(
                id:          new Guid("a1000000-0000-0000-0000-000000000003"),
                userId:      AlexUserId,
                latitude:    44.849000m,
                longitude:  -123.229000m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-45),
                description: null,
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Great Blue Heron" },

            // CSP-172: SpeciesName is intentionally a string the Animals API will
            // never resolve, so the details page must render its fun-fact fallback.
            new Sighting(
                id:          new Guid("a1000000-0000-0000-0000-000000000004"),
                userId:      AlexUserId,
                latitude:    44.851000m,
                longitude:  -123.231000m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-3),
                description: "An unrecognized animal photographed near the WOU campus.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.0) { SpeciesName = "Mystery Critter Z" },

            // ── Lily ──────────────────────────────────────────────────────────

            // Crater Lake — mythic-tier candidate (very few global sightings expected)
            new Sighting(
                id:          new Guid("a2000000-0000-0000-0000-000000000001"),
                userId:      LilyUserId,
                latitude:    42.944600m,
                longitude:  -122.109000m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-5),
                description: "Wolverine spotted at the Crater Lake rim — extremely rare in Oregon.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Wolverine" },

            // Portland — within typical Oregon map bounds
            new Sighting(
                id:          new Guid("a2000000-0000-0000-0000-000000000002"),
                userId:      LilyUserId,
                latitude:    45.523100m,
                longitude:  -122.676200m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-20),
                description: "Peregrine falcon nesting atop a downtown Portland high-rise.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Peregrine Falcon" },

            // Eugene
            new Sighting(
                id:          new Guid("a2000000-0000-0000-0000-000000000003"),
                userId:      LilyUserId,
                latitude:    44.052100m,
                longitude:  -123.086800m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-40),
                description: "River otter family playing along the Willamette near Eugene.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "River Otter" },

            // Silver Falls
            new Sighting(
                id:          new Guid("a2000000-0000-0000-0000-000000000004"),
                userId:      LilyUserId,
                latitude:    44.877000m,
                longitude:  -122.654000m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-60),
                description: "Roosevelt elk herd of ~30 animals crossing near Silver Falls State Park.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Roosevelt Elk" },

            // Los Angeles — deliberately outside any Oregon bounding box so
            // map-bounds filtering tests can verify that out-of-range sightings
            // are excluded from results.
            new Sighting(
                id:          new Guid("a2000000-0000-0000-0000-000000000005"),
                userId:      LilyUserId,
                latitude:    34.052200m,
                longitude:  -118.243700m,
                timestamp:   DateTimeOffset.UtcNow.AddDays(-15),
                description: "Coyote spotted at dusk in Griffith Park, Los Angeles.",
                imageBuffer: img,
                pointValue: 10,
                loginStreak: true,
                rarity: "Common",
                rarityMultiplier: 1.7) { SpeciesName = "Coyote" }
        );

        await db.SaveChangesAsync(token);
    }

    // =========================================================================
    // Seed — User Badges (PersonalBadges join table)
    //
    // Alex:  FirstSighting badge — awarded when he posted his first sighting.
    // Lily:  All three badges — exercises full badge display on the dashboard
    //        and supports "viewing another user's content" comparisons.
    // Patricia: No badges — keeps admin state minimal.
    // =========================================================================

    private static async Task SeedUserBadgesAsync(ApplicationDbContext db, CancellationToken token)
    {
        db.UserBadges.AddRange(

            // Alex
            new UserBadge
            {
                UserBadgeId = new Guid("b1000000-0000-0000-0000-000000000001"),
                UserId      = AlexUserId.ToString(),
                BadgeId     = BadgeConstants.FirstSightingBadgeGUID,
                BadgeEarned = DateTimeOffset.UtcNow.AddDays(-45),
            },

            // Lily
            new UserBadge
            {
                UserBadgeId = new Guid("b2000000-0000-0000-0000-000000000001"),
                UserId      = LilyUserId.ToString(),
                BadgeId     = BadgeConstants.ProfileBadgeGUID,
                BadgeEarned = DateTimeOffset.UtcNow.AddDays(-120),
            },
            new UserBadge
            {
                UserBadgeId = new Guid("b2000000-0000-0000-0000-000000000002"),
                UserId      = LilyUserId.ToString(),
                BadgeId     = BadgeConstants.CustomBioBadgeGUID,
                BadgeEarned = DateTimeOffset.UtcNow.AddDays(-115),
            },
            new UserBadge
            {
                UserBadgeId = new Guid("b2000000-0000-0000-0000-000000000003"),
                UserId      = LilyUserId.ToString(),
                BadgeId     = BadgeConstants.FirstSightingBadgeGUID,
                BadgeEarned = DateTimeOffset.UtcNow.AddDays(-90),
            }
        );

        await db.SaveChangesAsync(token);
    }

    // =========================================================================
    // Seed — Notifications
    //
    // Coverage
    // --------
    // Alex:     2 unread + 2 read — exercises unread badge count and
    //           read/unread rendering distinctions.
    //
    // Patricia: 1 read welcome — keeps admin state minimal; verifies notifications
    //           work for admin-role users.
    //
    // Lily:     3 read + 1 postdated (SentAt in the future → IsPostdated = true)
    //           — tests future-delivery rendering and the unread count for a user
    //           with richer notification history.
    // =========================================================================

    private static async Task SeedNotificationsAsync(ApplicationDbContext db, CancellationToken token)
    {
        var now = DateTimeOffset.UtcNow;

        db.Notifications.AddRange(

            // ── Alex ─────────────────────────────────────────────────────────

            new Notification(
                id:          new Guid("c1000000-0000-0000-0000-000000000001"),
                recipientId: AlexUserId,
                title:       "Welcome to Wildlife AID!",
                message:     "Start exploring wildlife in your area and earn points for every sighting.",
                sentAt:      now.AddDays(-45))
            { IsRead = false },

            new Notification(
                id:          new Guid("c1000000-0000-0000-0000-000000000002"),
                recipientId: AlexUserId,
                title:       "Badge Earned!",
                message:     "Congratulations! You earned the First Sighting Badge (+25 points).",
                sentAt:      now.AddDays(-45))
            { IsRead = false },

            new Notification(
                id:          new Guid("c1000000-0000-0000-0000-000000000003"),
                recipientId: AlexUserId,
                title:       "Points Awarded",
                message:     "You earned 25 points for your first wildlife sighting. Keep it up!",
                sentAt:      now.AddDays(-45))
            { IsRead = true },

            new Notification(
                id:          new Guid("c1000000-0000-0000-0000-000000000004"),
                recipientId: AlexUserId,
                title:       "Leaderboard Update",
                message:     "You climbed to rank #2 on the leaderboard. Keep sighting to reach #1!",
                sentAt:      now.AddDays(-20))
            { IsRead = true },

            // ── Patricia ─────────────────────────────────────────────────────

            new Notification(
                id:          new Guid("c2000000-0000-0000-0000-000000000001"),
                recipientId: PatriciaUserId,
                title:       "Welcome to Wildlife AID!",
                message:     "Your administrator account is ready. You can manage users and reports from the admin panel.",
                sentAt:      now.AddDays(-30))
            { IsRead = true },

            // ── Lily ──────────────────────────────────────────────────────────

            new Notification(
                id:          new Guid("c3000000-0000-0000-0000-000000000001"),
                recipientId: LilyUserId,
                title:       "Badge Earned!",
                message:     "Congratulations! You earned the Custom Profile Badge (+10 points).",
                sentAt:      now.AddDays(-120))
            { IsRead = true },

            new Notification(
                id:          new Guid("c3000000-0000-0000-0000-000000000002"),
                recipientId: LilyUserId,
                title:       "Badge Earned!",
                message:     "Congratulations! You earned the First Sighting Badge (+25 points).",
                sentAt:      now.AddDays(-90))
            { IsRead = true },

            new Notification(
                id:          new Guid("c3000000-0000-0000-0000-000000000003"),
                recipientId: LilyUserId,
                title:       "Leaderboard Update",
                message:     "You are ranked #1 on the leaderboard. Great work!",
                sentAt:      now.AddDays(-10))
            { IsRead = true },

            // Postdated: SentAt is 7 days in the future → IsPostdated == true
            new Notification(
                id:          new Guid("c3000000-0000-0000-0000-000000000004"),
                recipientId: LilyUserId,
                title:       "Monthly Summary",
                message:     "Your monthly wildlife sighting summary will be ready to view soon.",
                sentAt:      now.AddDays(7))
            { IsRead = false }
        );

        await db.SaveChangesAsync(token);
    }

    // =========================================================================
    // Seed — Reports
    //
    // Coverage
    // --------
    // Alex:  1 open/unresolved report — appears in Patricia's admin report queue.
    //
    // Lily:  1 resolved + 1 open on different URLs.
    //        The resolved report demonstrates that the filtered unique index
    //        (IsResolved = 0) allows Lily to re-report the same URL once resolved.
    //        The second open report tests multi-report state in the admin queue.
    //
    // Patricia: No reports filed — keeps admin state clean for moderation scenarios.
    // =========================================================================

    private static async Task SeedReportsAsync(ApplicationDbContext db, CancellationToken token)
    {
        db.Reports.AddRange(

            // Alex — one open report
            new Report
            {
                Id              = new Guid("d1000000-0000-0000-0000-000000000001"),
                ReportingUserId = AlexUserId,
                ReportedPageUrl = "https://localhost:5001/Sighting/Details/00000000-0000-0000-0000-999999999999",
                Reason          = "Inappropriate content",
                Description     = "This sighting appears to contain an inappropriate image.",
                SubmittedAt     = DateTime.UtcNow.AddDays(-3),
                IsResolved      = false,
            },

            // Lily — resolved report (demonstrates re-reporting is allowed after resolution)
            new Report
            {
                Id              = new Guid("d2000000-0000-0000-0000-000000000001"),
                ReportingUserId = LilyUserId,
                ReportedPageUrl = "https://localhost:5001/Sighting/Details/00000000-0000-0000-0000-888888888888",
                Reason          = "Incorrect information",
                Description     = "Species identification appears to be incorrect.",
                SubmittedAt     = DateTime.UtcNow.AddDays(-10),
                IsResolved      = true,
            },

            // Lily — open report on a distinct URL
            new Report
            {
                Id              = new Guid("d2000000-0000-0000-0000-000000000002"),
                ReportingUserId = LilyUserId,
                ReportedPageUrl = "https://localhost:5001/Sighting/Details/00000000-0000-0000-0000-777777777777",
                Reason          = "Spam",
                Description     = "This appears to be a duplicate sighting submitted multiple times.",
                SubmittedAt     = DateTime.UtcNow.AddDays(-2),
                IsResolved      = false,
            }
        );

        await db.SaveChangesAsync(token);
    }
}
