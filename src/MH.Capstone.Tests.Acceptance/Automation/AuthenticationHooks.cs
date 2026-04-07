using System.Diagnostics.CodeAnalysis;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.Automation
{
    [Binding]
    [ExcludeFromCodeCoverage]
    public sealed class AuthenticationHooks
    {
        // Is guaranteed to run before any scenario tagged with @auth, but after the setup of Selenium in SeleniumHooks,
        // so Selenium can be used within this hook
        [BeforeScenario("@auth")]
        public void LoginSpecificUserBeforeScenario(string username, string password)
        {
            //TODO: implement logic that has to run before executing each scenario
        }

        // Is guaranteed to run after any scenario tagged with @auth, but before the teardown of Selenium in SeleniumHooks.
        [AfterScenario("@auth")]
        public void AfterScenario()
        {
            //TODO: implement logic that has to run after executing each scenario
        }
    }
}