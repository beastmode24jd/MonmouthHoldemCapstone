using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.ApiContracts.Ninja;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Api;
using MH.Capstone.Domain.Services.Notifications;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging based on environment first so that it is available during app startup and for all services.
            builder.Logging.ConfigureLogging(builder.Environment);
            var entryLogger = CreateProgramEntryLogger();

            const string appConnStrName = "DataDb"; // For application data
            
            // Add EF Core DbContexts
            builder.Services.AddDbContext<ApplicationDbContext>(opt => opt
                .UseLazyLoadingProxies()
                .UseSqlServer(
                    builder.Configuration.GetConnectionString(appConnStrName)
                    ?? throw new InvalidOperationException($"Connection string {appConnStrName} not found in app settings file.\n\t" +
                        $"ENV is {builder.Environment.EnvironmentName}."),
                    sqlOptions => 
                        // Handle transient Azure SQL failures
                        sqlOptions.EnableRetryOnFailure())
                    // Must implement the synchronous SeedData method for EF Core Tooling.
                    .UseSeeding((context, _) => {
                        if (context is ApplicationDbContext appSyncContext)
                        {
                            ApplicationDbContextSeeding.SeedDataAsync(appSyncContext, _, CancellationToken.None).GetAwaiter().GetResult();
                        }
                    })
                    // This is the preferred call by any part of EF Core that can support Async calls.
                    .UseAsyncSeeding(async (context, _, token) =>
                    {
                        if (context is ApplicationDbContext appAsyncContext)
                        {
                            await ApplicationDbContextSeeding.SeedDataAsync(appAsyncContext, _, token);
                        }
                    })
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
                options.Cookie.HttpOnly = true;
                options.SlidingExpiration = true;
                // Remove global ExpireTimeSpan to allow session cookies when 'Remember Me' is not checked
                // ExpireTimeSpan will be set by SignInAsync's isPersistent parameter
            });

            // Configure Dependency Injection for Repositories and Services
            // Register Generic Repository
            builder.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

            // Register the User Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
            builder.Services.AddScoped<IProfileImageService, ProfileImageService>();

            // Register Additional Services - Business Logic Layer
            builder.Services.AddScoped<INotificationService, InAppNotificationService>();
            builder.Services.AddScoped<IBadgeService, BadgeService>();
            builder.Services.AddScoped<IScoringService, ScoringService>();
            builder.Services.AddScoped<ISightingsService, SightingsService>();
            builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
            builder.Services.AddScoped<IReportService, ReportService>();

            // Configure Ninja API Caller
            const string ninjasApiConfigSectionPath = "Api:External:Ninjas";
            builder.Services.AddExternalApiCaller<NinjaApiConfigValues>(builder.Configuration,
                builder.Environment, entryLogger, ninjasApiConfigSectionPath, 
                opts => opts.UseCacheProxy<>());

            // Add controllers with views and configure Newtonsoft.Json for JSON serialization
            builder.Services.AddControllersWithViews()
                .AddNewtonsoftJson();

            var app = builder.Build();

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

        public static ILogger CreateProgramEntryLogger()
        {
            var loggerFactory = LoggerFactory.Create(config => config.AddConsole());
            return loggerFactory.CreateLogger("ProgramEntry");
        }
    }
}

// Exposes Program to the integration test project so WebApplicationFactory<Program> can access it.
public partial class Program { }
