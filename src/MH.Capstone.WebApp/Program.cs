using MH.Capstone.Domain.ApiContracts.Gemini;
using MH.Capstone.Domain.ApiContracts.Ninja;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Services.Api;
using MH.Capstone.Domain.Services.Background;
using MH.Capstone.Domain.Services.Notifications;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MH.Capstone.Domain.Tools;
using MH.Capstone.Domain.Constants.Configurables;

namespace MH.Capstone.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = Configure(builder);
            app.Run();
        }

        public static WebApplication Configure(WebApplicationBuilder builder, ILoggerFactory? startupLoggerFactory = null)
        {
            // Configure logging based on environment first so that it is available during app startup and for all services.
            builder.Logging.ConfigureLogging(builder.Environment);
            var entryLogger = startupLoggerFactory?.CreateLogger("ProgramEntry") ?? CreateProgramEntryLogger();

            // Register FeatureFlags from configuration so features can be checked through DI
            var featureFlags = new FeatureFlags(builder.Configuration);
            builder.Services.AddSingleton(featureFlags);
            entryLogger.LogInformation("FeatureFlags registered from configuration.");

            // Obtain DB Connection String
            const string appConnStrName = "DataDb"; // For application data
            string? appConnStr = builder.Configuration.GetConnectionString(appConnStrName);
            if (string.IsNullOrEmpty(appConnStr) && !EF.IsDesignTime)
            {
                // Throw is the connection string was not found and EF is not in design-time
                throw new InvalidOperationException($"Connection string {appConnStrName} not found in app settings file.\n\t" +
                    $"ENV is {builder.Environment.EnvironmentName}.");
            }
            
            // Add EF Core DbContexts
            builder.Services.AddDbContext<ApplicationDbContext>(opt => opt
                .UseLazyLoadingProxies()
                .UseSqlServer(appConnStr, sqlOptions =>
                    // Handle transient Azure SQL failures
                    sqlOptions.EnableRetryOnFailure()
                        .MigrationsHistoryTable("__EFMigrationsHistory_ApplicationDbContext"))
                    // Must implement the synchronous SeedData method for EF Core Tooling.
                    .UseSeeding((context, _) => {
                        if (context is ApplicationDbContext appSyncContext)
                        {
                            ApplicationDbContextSeeding.SeedDataAsync(appSyncContext, _, CancellationToken.None).GetAwaiter().GetResult();
                        }
                    })
                    // This is the preferred call by any part of EF Core that can support Async calls.
                    .UseAsyncSeeding(async (context, _, token) => {
                        if (context is ApplicationDbContext appAsyncContext)
                        {
                            await ApplicationDbContextSeeding.SeedDataAsync(appAsyncContext, _, token);
                        }
                    })
            );

            // Register second DbContext for caching (uses same connection string/options as ApplicationDbContext, no seeding)
            builder.Services.AddDbContext<CacheDbContext>(opt => opt
                .UseLazyLoadingProxies()
                .UseSqlServer(appConnStr, sqlOptions => 
                    // Handle transient Azure SQL failures
                    sqlOptions.EnableRetryOnFailure()
                        .MigrationsHistoryTable("__EFMigrationsHistory_CacheDbContext"))
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
            builder.Services.AddScoped<IPhotoQualityService, PhotoQualityService>();

            // AI Companion (CSP-120) — Gemini-backed wildlife education chat
            if (featureFlags.IsEnabled("EnableGeminiAIService") && !EF.IsDesignTime)
            {
                entryLogger.LogInformation("EnableGeminiAIService flag is ON. Registering GeminiAIService.");
                builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
                builder.Services.AddHttpClient<IAIService, GeminiAIService>();
            }
            else
            {
                entryLogger.LogInformation("EnableGeminiAIService flag is OFF. GeminiAIService will not be registered.");
            }

            // Configure Azure Communication Services Email client depending on feature flag state and when EF is not in design-time
            var emailConnectionString = builder.Configuration.GetConnectionString("AzureCommunicationServices");
            var emailSender = builder.Configuration.GetValue<string>("Email:SenderAddress");
            var useRealEmailer = featureFlags.IsEnabled("UseRealEmailerService");

            if (useRealEmailer && !EF.IsDesignTime)
            {
                if (string.IsNullOrWhiteSpace(emailConnectionString) || string.IsNullOrWhiteSpace(emailSender))
                {
                    throw new InvalidOperationException($"UseRealEmailerService is enabled but Azure Communication Email is not configured. Please set ConnectionStrings:AzureCommunicationServices and Email:SenderAddress in appsettings.{{Environment}}.json.");
                }

                builder.Services.AddSingleton<IEmailService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<AzureCommunicationEmailService>>();
                    return new AzureCommunicationEmailService(emailConnectionString, emailSender, logger);
                });

                // Configure Email dispatcher options and background service
                builder.Services.Configure<EmailDispatcherOptions>(builder.Configuration.GetSection("EmailDispatcher"));
                builder.Services.AddHostedService<EmailDispatcherService>();

                entryLogger.LogInformation("AzureCommunicationEmailService and EmailDispatcherService registered (feature flag enabled).");
            }
            else
            {
                builder.Services.AddSingleton<IEmailService>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<NoOpEmailService>>();
                    return new NoOpEmailService(emailSender ?? "no-reply@localhost", logger);
                });

                entryLogger.LogInformation("NoOpEmailService registered (feature flag disabled).");
            }

            // Configure Ninja API Caller when EF is not running for design-time
            if (!EF.IsDesignTime)
            {
                const string ninjasApiConfigSectionPath = "Api:External:Ninjas";
                builder.Services.AddExternalApiCaller<NinjaApiConfigValues>(builder.Configuration,
                    builder.Environment, entryLogger, ninjasApiConfigSectionPath,
                    opts => opts.UseCacheProxy<NinjaAnimalCacheEntity>());
            }

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

            return app;
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