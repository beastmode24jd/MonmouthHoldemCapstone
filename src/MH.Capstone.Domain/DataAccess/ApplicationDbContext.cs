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
        }
    }
}