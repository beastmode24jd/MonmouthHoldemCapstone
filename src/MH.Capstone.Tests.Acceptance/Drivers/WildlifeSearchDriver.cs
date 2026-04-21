using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class WildlifeSearchDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    // The external Ninjas API can take a few seconds to respond; 20 s is generous
    // but keeps tests from hanging indefinitely.
    private static readonly TimeSpan SearchTimeout = TimeSpan.FromSeconds(20);

    public WildlifeSearchDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    /// <summary>Navigates to the species search page.</summary>
    public void NavigateToSearchPage()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Species/Search");
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

    /// <summary>Returns true when the browser is on the species search page.</summary>
    public bool IsOnSearchPage() =>
        _webDriver.Url.Contains("/Species/Search", StringComparison.InvariantCultureIgnoreCase)
        || _webDriver.Url.Contains("/animal/search", StringComparison.InvariantCultureIgnoreCase);

    // ─── UI element presence checks ───────────────────────────────────────────

    public bool IsSearchInputVisible()
    {
        var els = _webDriver.FindElements(By.Id("nameInput"));
        return els.Count > 0 && els[0].Displayed;
    }

    public bool IsSearchButtonVisible()
    {
        var els = _webDriver.FindElements(By.CssSelector("#searchForm button[type='submit']"));
        return els.Count > 0 && els[0].Displayed;
    }

    public bool IsClearButtonVisible()
    {
        var els = _webDriver.FindElements(By.Id("clearBtn"));
        return els.Count > 0 && els[0].Displayed;
    }

    // ─── Search interaction ────────────────────────────────────────────────────

    /// <summary>
    /// Types <paramref name="name"/> into the search input, clicks Search, and
    /// blocks until the AJAX fetch has completed (status indicator clears).
    /// Always performs a full page navigation before interacting so that the
    /// JavaScript state (results array, counter, input value) is guaranteed
    /// to be fresh, regardless of what a prior scenario may have left behind.
    /// </summary>
    public void SearchFor(string name)
    {
        // Force a full reload so JS state is never inherited from a prior scenario.
        NavigateToSearchPage();

        var page = new WildlifeSearchPageObject(_webDriver, _baseUrl);
        page.NameInput.Clear();
        page.NameInput.SendKeys(name);
        page.SearchButton.Click();

        // Give the browser a moment to process the click event and start the fetch,
        // so the status element transitions to "Searching…" before we poll it.
        Thread.Sleep(400);

        // Wait until the status indicator stops showing "Searching…", which is the
        // last thing cleared by doSearch() after both the search and render fetches
        // have completed.
        var wait = new WebDriverWait(_webDriver, SearchTimeout);
        wait.Until(d => !d.FindElement(By.Id("searchStatus")).Text
            .Contains("Searching", StringComparison.OrdinalIgnoreCase));
    }

    // ─── Result inspection ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when the result card contains at least one visible animal name
    /// (rendered by the <c>_AnimalResult</c> partial via the RenderPartial AJAX call).
    /// </summary>
    public bool HasResultWithAnimalName()
    {
        try
        {
            return _webDriver
                .FindElements(By.CssSelector(".result-title"))
                .Any(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));
        }
        catch (NoSuchElementException) { return false; }
    }

    /// <summary>
    /// Returns true when the result card shows the "No results." placeholder text.
    /// </summary>
    public bool HasNoResultsMessage()
    {
        var cards = _webDriver.FindElements(By.Id("resultCard"));
        return cards.Count > 0 && cards[0].Text.Contains("No results", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns the raw text of the result counter element, e.g. "1 / 3" or "0 / 0".
    /// </summary>
    public string GetCounterText()
    {
        var els = _webDriver.FindElements(By.Id("resultCounter"));
        return els.Count > 0 ? els[0].Text.Trim() : string.Empty;
    }

    /// <summary>
    /// Returns true when the counter shows at least one result (total &gt; 0).
    /// </summary>
    public bool CounterShowsAtLeastOneResult()
    {
        var text = GetCounterText(); // e.g., "1 / 3" or "0 / 0"
        var parts = text.Split('/');
        if (parts.Length != 2) return false;
        return int.TryParse(parts[1].Trim(), out var total) && total > 0;
    }

    // ─── Clear interaction ─────────────────────────────────────────────────────

    /// <summary>Clicks the Clear button and waits for the result card to reset.</summary>
    public void ClickClear()
    {
        _wait.Until(d => d.FindElement(By.Id("clearBtn"))).Click();

        // The clear handler is synchronous (no fetch involved), so the DOM updates
        // immediately. A short poll confirms the counter has reset to "0 / 0".
        var wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3));
        wait.Until(d => d.FindElement(By.Id("resultCounter")).Text.Trim() == "0 / 0");
    }

    /// <summary>Returns the current value of the search name input.</summary>
    public string GetSearchInputValue()
    {
        var els = _webDriver.FindElements(By.Id("nameInput"));
        return els.Count > 0 ? els[0].GetAttribute("value") ?? string.Empty : string.Empty;
    }
}
