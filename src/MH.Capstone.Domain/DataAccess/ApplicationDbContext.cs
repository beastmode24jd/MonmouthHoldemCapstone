using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataAccess
{
    // Database context for ASP.NET Core Identity.
    // Manages user authentication and authorization tables.
    // Inherits from IdentityDbContext to get built-in Identity tables
    // (AspNetUsers, AspNetRoles, AspNetUserClaims, etc.)
    // But will also be extended to include application-specific tables, such as Sightings.
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // Identity tables are automatically included.
        // Add additional user-related DbSets here if needed.

        public DbSet<Sighting> Sightings { get; set; } = null!;
        public DbSet<Badge> Badges { get; set; } = null!;
        public DbSet<UserBadge> UserBadges { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<EmailQueue> EmailQueue { get; set; } = null!;
        public DbSet<Club> Clubs { get; set; } = null!;
        public DbSet<ClubMembership> ClubMemberships { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; } = null!;
        public DbSet<LiveNotificationPreference> LiveNotificationPreferences { get; set; } = null!; // CSP-180

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Filtered unique index: Enforces uniqueness on (User, URL) ONLY for unresolved reports
            // should prevent spam but allows users to report the same URL again after resolved 
            modelBuilder.Entity<Report>()
                .HasIndex(r => new { r.ReportingUserIdentityId, r.ReportedPageUrl })
                .IsUnique()
                .HasFilter("[IsResolved] = 0");
            
            // Configure EmailQueue table defaults and indexes
            modelBuilder.Entity<EmailQueue>()
                .HasIndex(e => new { e.IsSent, e.ScheduledAt });

            // Prevent a user from appearing in the same club more than once
            modelBuilder.Entity<ClubMembership>()
                .HasIndex(cm => new { cm.MemberIdentityId, cm.ClubId })
                .IsUnique();

            // SQL Server doesn't allow multiple cascade paths to the same table.
            // AspNetUsers -> Club -> ClubMembership and AspNetUsers -> ClubMembership
            // would be two paths, so the direct user FK must be NoAction.
            modelBuilder.Entity<ClubMembership>()
                .HasOne(cm => cm.Member)
                .WithMany(u => u.ClubMemberships)
                .HasForeignKey(cm => cm.MemberIdentityId)
                .OnDelete(DeleteBehavior.NoAction);

            // Same issue: AspNetUsers -> Club -> Message and AspNetUsers -> Message.
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Author)
                .WithMany()
                .HasForeignKey(m => m.AuthorIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            // Unique constraint: one preference row per user per notification type
            modelBuilder.Entity<UserNotificationPreference>()
                .HasIndex(p => new { p.UserId, p.NotificationType })
                .IsUnique();

            // CSP-177: sparse unique index prevents duplicate submissions from offline sync retries
            modelBuilder.Entity<Sighting>()
                .HasIndex(s => new { s.UserIdentityId, s.ClientSightingId })
                .IsUnique()
                .HasFilter("[ClientSightingId] IS NOT NULL");
        }
    }
}