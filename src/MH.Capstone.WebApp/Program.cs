using MH.Capstone.Domain.DataAccess.Contexts;
using Microsoft.AspNetCore.Authentication.Cookies;
using MH.Capstone.WebApp.Services;
using Microsoft.EntityFrameworkCore;

namespace MH.Capstone.WebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            string appConnStrName = "DataDb"; // For application data
            string authConnStrName = "AuthDb"; // For user authentication data

            builder.Services.AddDbContext<ApplicationDbContext>(opt => opt
                .UseLazyLoadingProxies()
                .UseSqlServer(builder.Configuration.GetConnectionString(appConnStrName)
                    ?? throw new InvalidOperationException($"Connection string {appConnStrName} not found in app settings file.\n\t" +
                                                           $"ENV is {builder.Environment.EnvironmentName}."))
            );

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

            // Register mock authentication service
            builder.Services.AddScoped<IAuthenticationService, MockAuthenticationService>();

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
