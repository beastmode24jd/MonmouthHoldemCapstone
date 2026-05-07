using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MH.Capstone.Domain.DataAccess 
{
    public static class ApplicationDbContextSeeding
    {
        public static async Task SeedDataAsync(ApplicationDbContext context, bool _, CancellationToken token) 
        {

            var badgeSeedList = new List<Badge>
            {
                new Badge
                {
                    BadgeID = Constants.BadgeId.ProfileBadgeGUID,
                    Title = "Custom Profile Badge",
                    Description = "Uploaded a custom profile image.",
                    PointValue = 10,
                    HintToEarn = "Upload a custom profile icon."
                    // Default profile image will be dealt with by frontend
                },

                new Badge
                {
                    BadgeID = Constants.BadgeId.CustomBioBadgeGUID,
                    Title = "Custom Bio Badge",
                    Description = "Updated your profile with a custom description.",
                    PointValue = 10,
                    HintToEarn = "Update your profile with a custom bio."
                },

                new Badge
                {
                    BadgeID = Constants.BadgeId.FirstSightingBadgeGUID,
                    Title = "First Sighting Badge",
                    Description = "Uploaded your first Sighting!",
                    PointValue = 25,
                    HintToEarn = "Upload a Sighting."
                }
            };

            // Loop through the list, and check if ApplicationDbContext has them already.
            // If it has them, process an update. If it doesn't, it uses EF to seed them.
            foreach (var seedBadge in badgeSeedList)
            {
                // Look for the existing badge in the database
                var existingBadge = await context.Set<Badge>()
                    .FirstOrDefaultAsync(b => b.BadgeID == seedBadge.BadgeID);

                if (existingBadge != null)
                {
                    // UPDATE: The badge exists, so update the new/changed fields
                    existingBadge.Title = seedBadge.Title;
                    existingBadge.Description = seedBadge.Description;
                    existingBadge.PointValue = seedBadge.PointValue;
                    existingBadge.HintToEarn = seedBadge.HintToEarn; // New field to update on older Badges
                    existingBadge.BadgeSteps = seedBadge.BadgeSteps;

                    context.Set<Badge>().Update(existingBadge);
                }
                else
                {
                    // INSERT: The badge is completely new
                    await context.Set<Badge>().AddAsync(seedBadge);
                }
            }
        
            // Seed identity data here
            await SeedIdentityAsync(context);


            // Now save it to the DB!
            await context.SaveChangesAsync(token);
        }

        private static async Task SeedIdentityAsync(ApplicationDbContext context)
        {
            // Seed the data for UserRoles ****************

            var configuration = context.GetService<IConfiguration>();
            var logger = context.GetService<ILogger<ApplicationDbContext>>();

            string[] roles = {"User", "Admin"};

            // Initialize roles.

            foreach (var role in roles)
            {
                var normalizedName = role.ToUpper();
                if (!await context.Roles.AnyAsync(r => r.NormalizedName == normalizedName))
                {
                    context.Roles.Add(new IdentityRole
                    {
                        // Id is an assigned GUID
                        Name = role,
                        NormalizedName = normalizedName
                    });

                    logger.LogInformation("Role created: {Role}", role);
                }
                else
                {
                    logger.LogInformation($"Role with {role} already exists in database. Skipping creation.");
                }
            }

            await context.SaveChangesAsync();

            // Initialize an Admin-level account for testing.
            var adminEmail = configuration.GetSection("AdminAccount:Hidden")["Email"];
            var adminPassword = configuration.GetSection("AdminAccount:Hidden")["Password"];

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var normalizedEmail = adminEmail.ToUpper();
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        NormalizedUserName = normalizedEmail,
                        Email = adminEmail,
                        NormalizedEmail = normalizedEmail,
                        EmailConfirmed = true,
                        DisplayName = "Admin"
                    };

                    // Manually hash the password
                    var hasher = new PasswordHasher<ApplicationUser>();
                    adminUser.PasswordHash = hasher.HashPassword(adminUser, adminPassword);

                    context.Users.Add(adminUser);
                    await context.SaveChangesAsync();

                    // Assign the admin role
                    var adminRole = await context.Roles.FirstAsync(r => r.NormalizedName == "ADMIN");
                    context.UserRoles.Add(new IdentityUserRole<string> 
                    { 
                        UserId = adminUser.Id, 
                        RoleId = adminRole.Id 
                    });
            
                    await context.SaveChangesAsync();
                    logger.LogInformation("Admin account and role assignment saved to database.");

                }
                else
                {
                    logger.LogInformation("Test Admin already exists. Skipping initialization.");
                }
            }

        }
    }
}