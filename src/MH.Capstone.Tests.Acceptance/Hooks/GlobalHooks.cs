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
        private WebApplication? _webApp;

        // Add any global setup logic here that needs to be run before any tests are executed
        [BeforeTestRun]
        public async Task BeforeTestRun()
        { 
            _webApp = Startup.ConfigureWebApp();
            await _webApp.StartAsync();
        }

        // Add any global teardown logic here that needs to be run after all tests have finished executing
        [AfterTestRun]
        public async Task AfterTestRun()
        {
            if (_webApp != null)
            {
                await _webApp.StopAsync();
                await _webApp.DisposeAsync();
                _webApp = null;
            }
        }
    }
}