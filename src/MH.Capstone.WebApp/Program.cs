using MH.Capstone.Domain.ApiContracts;
using MH.Capstone.Domain.ApiContracts.Ninjas;
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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MH.Capstone.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
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
                    // This is will be the perfered call by any part of EF Core that can support Async calls.
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
                options.ExpireTimeSpan = TimeSpan.FromDays(30); // 30-day expiration from user story
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
            });

            // Get the base URL and API key from configuration (appsettings.json or environment variables)
            // Done outside the HttpClient configuration to validate presence and provide clear and fast (at app startup)
            // error feedback if missing.
            const string ninjasApiConfigSection = "Api:External:Ninjas";
            var ninjasApiConfigValues = builder.Configuration.GetSection(ninjasApiConfigSection)
                .GetApiConfig<NinjaApiConfigValues>(out var apiKey);

            // "is not" pattern matching syntax checks if the config values class isn't null plus that
            // the required values are present and valid
            if (string.IsNullOrWhiteSpace(apiKey) || ninjasApiConfigValues is not { IsValid: true })
            {
                if (!builder.Environment.IsDevelopment())
                {
                    // In a non-development environment, we want to hide the API key value,
                    // so we will obscure it in the error message.
                    apiKey = string.IsNullOrWhiteSpace(apiKey)
                        ? "MISSING"
                        : $"{new string('X', apiKey.Length)}";
                }

                throw new InvalidConfigurationException(
                    $"Required API Configuration values {nameof(ninjasApiConfigValues.HttpClientKey)}," +
                    $"{nameof(ninjasApiConfigValues.BaseUrl)} and/or " +
                    $"{nameof(apiKey)} were missing or unset. This is a fatal error!\n" +
                    $"\t{nameof(ninjasApiConfigValues.HttpClientKey)} = {ninjasApiConfigValues?.HttpClientKey}\n" +
                    $"\t{nameof(ninjasApiConfigValues.BaseUrl)} = {ninjasApiConfigValues?.BaseUrl}\n" +
                    $"\t{nameof(apiKey)} = {apiKey}");
            }

            // Configure HttpClient for external API calls (e.g., AnimalApi, Emailer, etc.)
            builder.Services.AddSingleton(ninjasApiConfigValues);
            builder.Services.AddHttpClient(ninjasApiConfigValues.HttpClientKey, client =>
            {
                // BaseAddress and other settings can be configured when injecting the client
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
                client.BaseAddress = new Uri(ninjasApiConfigValues.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(15); // Set a reasonable timeout
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
            builder.Services.AddScoped(typeof(IApiCallerFactory<>), typeof(ApiCallerFactory<>));

            // Add controllers with views and configure Newtonsoft.Json for JSON serialization
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

// Exposes Program to the integration test project so WebApplicationFactory<Program> can access it.
public partial class Program { }
