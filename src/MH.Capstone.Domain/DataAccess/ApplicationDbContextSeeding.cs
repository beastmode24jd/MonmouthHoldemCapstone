using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace MH.Capstone.Domain.DataAccess 
{
    public static class ApplicationDbContextSeeding
    {
        private static readonly RoleManager<IdentityRole> roleManager;

        public static async Task SeedDataAsync(ApplicationDbContext context, bool _, CancellationToken token) 
        {
            var badgeSeedList = new List<Badge>
            {
                new Badge
                {
                    BadgeID = Constants.BadgeId.ProfileBadgeGUID,
                    Title = "Custom Profile Badge",
                    Description = "Uploaded a custom profile image.",
                    PointValue = 10
                    // Default profile image will be dealt with by frontend
                },

                new Badge
                {
                    BadgeID = Constants.BadgeId.CustomBioBadgeGUID,
                    Title = "Custom Bio Badge",
                    Description = "Updated your profile with a custom description.",
                    PointValue = 10
                },

                new Badge
                {
                    BadgeID = Constants.BadgeId.FirstSightingBadgeGUID,
                    Title = "First Sighting Badge",
                    Description = "Uploaded your first Sighting!",
                    PointValue = 25
                }
            };

            // Loop through the list, and check if ApplicationDbContext has them already.
            // If it has them, process an update. If it does, it uses EF to seed them.
            foreach (var badge in badgeSeedList)
            {
                if (!await context.Set<Badge>().AnyAsync(b => b.BadgeID == badge.BadgeID, token))
                {
                    // Db doesn't have the badge, so we need to add it to the DB.
                    await context.AddAsync(badge, token);
                }
                else
                {
                    // Db has the badge, so we need to update it in the DB.
                    context.Update(badge);
                }
            }

            // Seed the data for UserRoles ****************

            string[] roles = {"User", "Admin"};

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        
            // Seed more data here later, if needed

            // Now save it to the DB!
            await context.SaveChangesAsync(token);
        }
    }
}