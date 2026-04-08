using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.Drivers
{
    [ExcludeFromCodeCoverage]
    public class DashboardDriver
    {
        private readonly IWebDriver _webDriver;

        public DashboardDriver(IWebDriver webDriver)
        {
            _webDriver = webDriver;
        }

        public bool IsOnDashboard()
        {
            // TODO - Put this URL in a config file or something similar so that it can be run against different environments
            const string url = "https://localhost:7147/Dashboard";
            return string.Equals(_webDriver.Url, url, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
