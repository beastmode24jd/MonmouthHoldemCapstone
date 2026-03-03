using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        
            // Seed more data here later, if needed

            // Now save it to the DB!
            await context.SaveChangesAsync(token);
        }

        public static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
        {
            // Needs to go after this in Program.cs: var app = builder.Build();

            // Seed the data for UserRoles ****************

            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            string[] roles = {"User", "Admin"};

            // Initialize roles.

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Role created: {Role}", role);
                }
            }

            // Default pre-existing accounts (before Identity implementation) as Users
            // Checks entire user list:
            //              not scalable, but will work for a testing/dev environment.
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                var existingRoles = await userManager.GetRolesAsync(user);
                if (existingRoles.Count == 0)
                {
                    await userManager.AddToRoleAsync(user, "User");
                    logger.LogInformation("Assigned default 'User' role to: {Email}", user.Email);
                }
            }

            // Initialize an Admin-level account for testing.
            var adminEmail = configuration.GetSection("AdminAccount:Hidden")["Email"];
            var adminPassword = configuration.GetSection("AdminAccount:Hidden")["Password"];

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
                    };

                    // CreateAsync handles the account Password here
                    var createAdmin = await userManager.CreateAsync(adminUser, adminPassword);
                    if (createAdmin.Succeeded)
                    {
                        await userManager.AddToRoleAsync(adminUser, "Admin");
                        logger.LogInformation("Test Admin was successfully created.");
                    }
                    else
                    {
                        logger.LogInformation("Test Admin creation was unsuccessful.");
                    }
                }
                else
                {
                    logger.LogInformation("Test Admin already exists. Skipping initialization.");
                }
            }

        }
    }
}