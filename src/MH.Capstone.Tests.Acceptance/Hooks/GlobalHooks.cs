using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.Hooks
{
    [Binding]
    [ExcludeFromCodeCoverage]
    public sealed class GlobalHooks
    {
        // Add any global setup logic here that needs to be run before any tests are executed
        [BeforeTestRun]
        public static async Task BeforeTestRun(WebApplication webApp)
        {
            await webApp.StartAsync();
        }

        // Add any global teardown logic here that needs to be run after all tests have finished executing
        [AfterTestRun]
        public static async Task AfterTestRun(WebApplication webApp)
        {
            await webApp.StopAsync();
            await webApp.DisposeAsync();
        }
    }
}