using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.Hooks
{
    [Binding]
    [ExcludeFromCodeCoverage]
    public sealed class GlobalHooks
    {
        [BeforeTestRun]
        public static void BeforeTestRun()
        {
            // TODO - Add any global setup logic here that needs to be run before any tests are executed
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            // TODO - Add any global teardown logic here that needs to be run after all tests have finished executing
        }
    }
}