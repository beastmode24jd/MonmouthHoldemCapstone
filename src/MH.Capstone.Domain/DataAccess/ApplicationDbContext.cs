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
        // Add additional auth-related DbSets here if needed.

        public DbSet<Sighting> Sightings { get; set; } = null!;

        public DbSet<Notification> Notifications { get; set; } = null!;

        public DbSet<Badge> Badges { get; set; } = null!;

        public DbSet<UserBadge> UserBadges { get; set; } = null!;
    }
}