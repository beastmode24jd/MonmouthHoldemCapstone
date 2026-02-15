using MH.Capstone.Domain.DataAccess.Contexts;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using MH.Capstone.WebApp.Services;
using MH.Capstone.WebApp.Data;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string appConnStrName = "DataDb"; // For application data
            
            // Register Local DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Register updated SQL service
            builder.Services.AddScoped<IAuthenticationService, MockAuthenticationService>();
            builder.Services.AddScoped<IProfileImageService, ProfileImageService>();
            // ... other services like IProfileImageService

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

            builder.Services.AddDbContext<ApplicationDbContext>(opt => opt
                .UseLazyLoadingProxies()
                .UseSqlServer(
                    builder.Configuration.GetConnectionString(appConnStrName)
                        ?? throw new InvalidOperationException($"Connection string {appConnStrName} not found in app settings file.\n\t" +
                                                               $"ENV is {builder.Environment.EnvironmentName}."),
                    sqlOptions => sqlOptions.EnableRetryOnFailure()) // Handle transient Azure SQL failures
            );

            // Add AuthDbContext for Identity
            builder.Services.AddDbContext<AuthDbContext>(opt => opt
                .UseSqlServer(
                    builder.Configuration.GetConnectionString(appConnStrName) // Using same database
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
                .AddEntityFrameworkStores<AuthDbContext>()
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

            // Register real authentication service with Identity
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Register the mocked Profile Image Service
            builder.Services.AddScoped<IProfileImageService, ProfileImageService>();

            // Add services to the container.
            builder.Services.AddControllersWithViews();

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
