using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class OfflineQueuePageObject
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;

    public OfflineQueuePageObject(IWebDriver driver, string baseUrl)
    {
        _driver = driver;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public bool IsOnOfflineQueuePage() =>
        _driver.Url.Contains("/Sighting/OfflineQueue", StringComparison.OrdinalIgnoreCase);

    public bool IsEmptyStateVisible()
    {
        var els = _driver.FindElements(By.Id("offlineQueueEmpty"));
        return els.Count > 0 && els[0].Displayed;
    }

    public IReadOnlyCollection<IWebElement> GetQueueItemRows() =>
        _driver.FindElements(By.CssSelector(".queue-item-row"));

    public bool HasDeleteButtons() =>
        _driver.FindElements(By.CssSelector(".deleteQueueItemBtn")).Count > 0;

    public string GetCurrentUserId()
    {
        // .Text returns "" for display:none elements; use textContent attribute instead.
        var els = _driver.FindElements(By.Id("currentUserId"));
        foreach (var el in els)
        {
            var val = el.GetAttribute("textContent")?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(val)) return val;
        }
        return string.Empty;
    }
}
