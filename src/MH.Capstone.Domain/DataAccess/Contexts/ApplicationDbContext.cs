using MH.Capstone.Domain.DataModels;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.Domain.DataAccess.Contexts
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        
        public DbSet<Sighting> Sightings { get; set; } = null!;
    }
}