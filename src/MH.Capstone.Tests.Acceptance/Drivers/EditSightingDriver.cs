using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class EditSightingDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public EditSightingDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToEdit(Guid sightingId)
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Edit/{sightingId}");
        WaitForPageReady();
    }

    public bool IsOnEditPage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
                d.Url.Contains("/Sighting/Edit/", StringComparison.InvariantCultureIgnoreCase)
                && d.FindElements(By.Id("editSightingForm")).Count > 0);
        }
        catch
        {
            return false;
        }
    }

    public string GetSpeciesValue() =>
        new EditSightingPageObject(_webDriver).SpeciesField.GetAttribute("value") ?? string.Empty;

    public string GetDescriptionValue() =>
        new EditSightingPageObject(_webDriver).DescriptionField.GetAttribute("value") ?? string.Empty;

    public void SetSpecies(string value)
    {
        var field = new EditSightingPageObject(_webDriver).SpeciesField;
        field.Clear();
        if (!string.IsNullOrEmpty(value)) field.SendKeys(value);
    }

    public void SetDescription(string value)
    {
        var field = new EditSightingPageObject(_webDriver).DescriptionField;
        field.Clear();
        if (!string.IsNullOrEmpty(value)) field.SendKeys(value);
    }

    public void SubmitEdit()
    {
        new EditSightingPageObject(_webDriver).SaveButton.Click();
        WaitForPageReady();
    }

    // True when at least one validation message with visible text is present — covers both
    // jQuery-unobtrusive client-side messages and the server-rendered validation summary.
    public bool HasVisibleValidationError()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
            {
                var page = new EditSightingPageObject(d);
                return page.ValidationErrors.Any(e => !string.IsNullOrWhiteSpace(e.Text));
            });
        }
        catch
        {
            return false;
        }
    }

    private void WaitForPageReady()
    {
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }
}
