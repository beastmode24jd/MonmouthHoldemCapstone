using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataAccess.Contexts
{
    // Database context for ASP.NET Core Identity.
    // Manages user authentication and authorization tables.
    // Inherits from IdentityDbContext to get built-in Identity tables
    // (AspNetUsers, AspNetRoles, AspNetUserClaims, etc.)
    public class AuthDbContext : IdentityDbContext<ApplicationUser>
    {
        public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
        {
            
        }

        // Identity tables are automatically included.
        // Add additional auth-related DbSets here if needed.
    }
}