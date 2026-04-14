using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using MH.Capstone.Tests.Acceptance.Configuration;
using NUnit.Framework;
using OpenQA.Selenium;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.Hooks;

[ExcludeFromCodeCoverage]
[Binding]
public class FailureHooks
{
    private readonly IWebDriver _webDriver;
    private readonly ScenarioContext _scenarioContext;

    public FailureHooks(IWebDriver webDriver, ScenarioContext scenarioContext)
    {
        _webDriver = webDriver;
        _scenarioContext = scenarioContext;
    }

    // Runs after each scenario. If the scenario failed, save the current page HTML
    // to a temp file and write the path and HTML to the test output/console.
    [AfterScenario(Order = 100)]
    public void AfterScenario_SaveHtmlOnFailure()
    {
        try
        {
            if (_scenarioContext.TestError == null)
                return; // test passed

            // Guard if the driver is not available or browser closed
            if (_webDriver == null)
            {
                TestContext.Out.WriteLine("[FailureHooks] no IWebDriver available to capture page source.");
                return;
            }

            string pageSource;
            try
            {
                pageSource = _webDriver.PageSource ?? string.Empty;
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"[FailureHooks] unable to read PageSource from IWebDriver: {ex.Message}");
                pageSource = string.Empty;
            }

            // Build a safe filename using scenario title and timestamp
            var title = _scenarioContext.ScenarioInfo?.Title ?? "unnamed_scenario";
            var safeTitle = MakeSafeFileName(title);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"spec_failure_{safeTitle}_{timestamp}.html";
            var filePath = Path.Combine(Path.GetTempPath(), fileName);

            try
            {
                File.WriteAllText(filePath, pageSource);
                TestContext.Out.WriteLine($"[FailureHooks] Saved HTML page source to: {filePath}");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"[FailureHooks] Failed writing HTML to disk: {ex.Message}");
            }

            // For now, also output the full HTML to the test console/output
            TestContext.Out.WriteLine("[FailureHooks] Begin page HTML output:");
            TestContext.Out.WriteLine(pageSource);
            TestContext.Out.WriteLine("[FailureHooks] End page HTML output.");
        }
        catch (Exception ex)
        {
            // Never throw from a hook; just log.
            TestContext.Out.WriteLine($"[FailureHooks] Unexpected error in AfterScenario hook: {ex}");
        }
    }

    private static string MakeSafeFileName(string name)
    {
        // Replace invalid path chars with underscore and collapse whitespace.
        var invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
        var invalidRegex = new Regex($"[{Regex.Escape(invalid)}]");
        var cleaned = invalidRegex.Replace(name, "_");
        cleaned = Regex.Replace(cleaned, @"\s+", "_");
        return cleaned;
    }
}