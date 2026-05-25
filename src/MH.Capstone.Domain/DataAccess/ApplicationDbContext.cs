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
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;
        public DbSet<EmailQueue> EmailQueue { get; set; } = null!;
        public DbSet<Club> Clubs { get; set; } = null!;
        public DbSet<ClubMembership> ClubMemberships { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; } = null!;
        public DbSet<UserFollow> UserFollows { get; set; } = null!;
        public DbSet<UserBlock> UserBlocks { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<CommentModerationLog> CommentModerationLogs { get; set; } = null!;
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

            // CSP-187: a user can only follow another user once.
            modelBuilder.Entity<UserFollow>()
                .HasIndex(f => new { f.FollowerIdentityId, f.FolloweeIdentityId })
                .IsUnique();

            // SQL Server forbids multiple cascade paths from AspNetUsers to UserFollow,
            // so the second user FK uses NoAction.
            modelBuilder.Entity<UserFollow>()
                .HasOne(f => f.Follower)
                .WithMany()
                .HasForeignKey(f => f.FollowerIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<UserFollow>()
                .HasOne(f => f.Followee)
                .WithMany()
                .HasForeignKey(f => f.FolloweeIdentityId)
                .OnDelete(DeleteBehavior.NoAction);

            // CSP-187: a user can only block another user once.
            modelBuilder.Entity<UserBlock>()
                .HasIndex(b => new { b.BlockerIdentityId, b.BlockedIdentityId })
                .IsUnique();
            modelBuilder.Entity<UserBlock>()
                .HasOne(b => b.Blocker)
                .WithMany()
                .HasForeignKey(b => b.BlockerIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<UserBlock>()
                .HasOne(b => b.Blocked)
                .WithMany()
                .HasForeignKey(b => b.BlockedIdentityId)
                .OnDelete(DeleteBehavior.NoAction);

            // CSP-187: Comment has two paths from AspNetUsers (direct Author + via Sighting).
            // Author FK uses NoAction; cascade lives on Sighting -> Comment so deleting a
            // sighting still removes its comments.
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Comment>()
                .HasIndex(c => c.SightingId);

            // CSP-187: moderation log has paths via Comment (-> Author user) and direct Moderator user.
            // Both user-related FKs use NoAction; the audit row survives if the comment row is deleted.
            modelBuilder.Entity<CommentModerationLog>()
                .HasOne(l => l.Moderator)
                .WithMany()
                .HasForeignKey(l => l.ModeratorIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<CommentModerationLog>()
                .HasOne(l => l.Comment)
                .WithMany()
                .HasForeignKey(l => l.CommentId)
                .OnDelete(DeleteBehavior.NoAction);

            // CSP-213: audit log has three FKs (performing admin, optional target user, optional target report).
            // All use NoAction so the audit row survives if the referenced user or report is deleted.
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.PerformingUser)
                .WithMany()
                .HasForeignKey(a => a.PerformingUserIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.TargetUser)
                .WithMany()
                .HasForeignKey(a => a.TargetUserIdentityId)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.TargetReport)
                .WithMany()
                .HasForeignKey(a => a.TargetReportId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}