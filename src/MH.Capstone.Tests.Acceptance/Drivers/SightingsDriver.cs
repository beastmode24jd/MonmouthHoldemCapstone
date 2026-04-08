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


    }
}
