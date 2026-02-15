using MH.Capstone.Domain.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataAccess.Contexts
{
    // This class represents the Entity Framework Core database context for the application.
    // It is responsible for managing the connection to the database and providing access to
    // the data models through DbSet properties.
    // For use with DataDb only, not AuthDb - which is for the user authentication/authorization data.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Define DbSet properties for db entities below
         public DbSet<Sighting> Sightings { get; set; }
    }
}
