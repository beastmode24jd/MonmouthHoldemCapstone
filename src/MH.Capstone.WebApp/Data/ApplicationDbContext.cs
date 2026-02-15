using Microsoft.EntityFrameworkCore;
using MH.Capstone.WebApp.Models;

namespace MH.Capstone.WebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<ApplicationUser> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Setting Email as the primary key since it's used for lookups
            modelBuilder.Entity<ApplicationUser>().HasKey(u => u.Email);
        }
    }
}