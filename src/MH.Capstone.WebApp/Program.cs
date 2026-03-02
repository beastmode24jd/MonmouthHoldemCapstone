using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Notifications;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.WebApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string appConnStrName = "DataDb"; // For application data
            
            // Configure cookie authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(30);
                    options.SlidingExpiration = true;
                    options.Cookie.HttpOnly = true;
                });

            // Add EF Core DbContexts
            builder.Services.AddDbContext<ApplicationDbContext>(opt => opt
                .UseLazyLoadingProxies()
                .UseSqlServer(
                    builder.Configuration.GetConnectionString(appConnStrName)
                        ?? throw new InvalidOperationException($"Connection string {appConnStrName} not found in app settings file.\n\t" +
                                                               $"ENV is {builder.Environment.EnvironmentName}."),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()) // Handle transient Azure SQL failures
            );

            // Configure Identity for authentication
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    // Password requirements (from your user story)
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 8;
                    
                    // Sign-in settings
                    options.SignIn.RequireConfirmedEmail = false; // For MVP, no email confirmation required
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            // Configure Identity cookie settings (Remember Me functionality)
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(30); // 30-day expiration from user story
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
            });

            // Register Generic Repository
            builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            // Register the User Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
            builder.Services.AddScoped<IUserService, UserService>();

            // Register Additional Services - Business Logic Layer
            builder.Services.AddScoped<INotificationService, InAppNotificationService>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IScoringService, ScoringService>();
            builder.Services.AddScoped<ISightingsService, SightingsService>();

            // Register the Badge Service
            

            // Add services to the container.
            builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson();

            // Configure Logging, with some based on environment
            // Note: DO NOT REMOVE THE CONSOLE LOGGER OR AZURE.
            // Azure App Service relies on it for log collection.
            builder.Logging.AddConsole();
            if (!builder.Environment.IsDevelopment())
            {
                // Staging or Production - add Azure App Service diagnostics logging
                builder.Logging.AddAzureWebAppDiagnostics();
            }

            var app = builder.Build();

            // INITIALIZING BADGE BLOCK
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var badgeService = services.GetRequiredService<IBadgeService>();
                    await badgeService.EnsureStandardBadgesCreated();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the Badges table.");
                }
            }
            // ----------------

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}