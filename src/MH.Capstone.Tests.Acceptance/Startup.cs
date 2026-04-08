using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Reqnroll.Microsoft.Extensions.DependencyInjection;

namespace MH.Capstone.Tests.Acceptance
{
    [ExcludeFromCodeCoverage]
    public static class Startup
    {
        public static WebApplication ConfigureWebApp()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Acceptance"
            });

            return WebApp.Program.Configure(builder);
        }

        [ScenarioDependencies]
        public static IServiceCollection RegisterTestDependencies()
        {
            var services = new ServiceCollection();

            // Register any dependencies needed for the tests here.
            services.AddScoped<IWebDriver>(sp => new ChromeDriver());

            return services;
        }
    }
}
