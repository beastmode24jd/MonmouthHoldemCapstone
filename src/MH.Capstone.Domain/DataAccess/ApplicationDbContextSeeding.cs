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
            var sp = serviceProvider.GetRequiredService<IConfiguration>();

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
        
            // Seed more data here
            await context.SaveChangesAsync.SeedIdentityAsync(sp);


            // Now save it to the DB!
            await context.SaveChangesAsync(token);
        }

        private static async Task SeedIdentityAsync(IServiceProvider serviceProvider)
        {
            // Needs to go after this in Program.cs: var app = builder.Build();

            // Seed the data for UserRoles ****************

            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

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
            }

            await context.SaveChangesAsync();

            // Initialize an Admin-level account for testing.
            var adminEmail = configuration.GetSection("AdminAccount:Hidden")["Email"];
            var adminPassword = configuration.GetSection("AdminAccount:Hidden")["Password"];

            if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
            {
                var normalizedEmail = adminEmail.ToUpper();
                var adminUser = await context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        UserName = adminEmail,
                        NormalizedUserName = normalizedEmail,
                        Email = adminEmail,
                        EmailConfirmed = true
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