using Reqnroll;
using System.Diagnostics.CodeAnalysis;

namespace MH.Capstone.Tests.Acceptance.Automation
{
    [Binding]
    [ExcludeFromCodeCoverage]
    public sealed class SeleniumHooks
    {
        // Must run first to ensure Selenium is properly initialized before each scenario
        // or additional hooks that depend on Selenium are executed
        [BeforeScenario(Order = 0)]
        public void SetupSeleniumBeforeScenario()
        {
            // TODO: Setup Selenium WebDriver and any necessary configuration here
        }

        // Must run last to ensure Selenium is properly closed after each scenario
        // and to ensure any additional hooks that depend on Selenium are executed before it is closed
        [AfterScenario(Order = 100)]
        public void TearDownSeleniumAfterScenario()
        {
            // TODO: Teardown Selenium WebDriver and anything else here
        }
    }
}