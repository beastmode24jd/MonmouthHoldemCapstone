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
    public class SightingsDriver
    {
        private readonly IWebDriver _webDriver;

        public SightingsDriver(IWebDriver webDriver)
        {
            _webDriver = webDriver;
        }

        // TODO - Put this URL in a config file or something similar so that it can be run against different environments
        public void NavigateToSightingsUpload() => 
            _webDriver.Navigate().GoToUrl("https://localhost:7147/Sighting/Create");
    }
}
