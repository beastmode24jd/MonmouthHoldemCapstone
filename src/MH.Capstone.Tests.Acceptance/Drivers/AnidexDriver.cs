using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

/// <summary>
/// CSP-142: Selenium driver for the personal Anidex page (/anidex).
/// Wraps navigation and read helpers so step definitions stay declarative.
/// </summary>
[ExcludeFromCodeCoverage]
public class AnidexDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public AnidexDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToAnidex()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/anidex");
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

    public bool IsOnAnidexPage()
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(3)).Until(d =>
                d.Url.Contains("/anidex", StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public bool IsEmptyStateVisible()
    {
        var els = _webDriver.FindElements(By.Id("anidexEmptyState"));
        return els.Count > 0 && els[0].Displayed;
    }

    public int GetVisibleEntryCount()
    {
        var entries = _webDriver.FindElements(By.CssSelector(".anidex-entry"));
        return entries.Count(e => e.Displayed);
    }

    public ISet<string> GetVisibleSpeciesNames()
    {
        var labels = _webDriver.FindElements(By.CssSelector(".anidex-species-name"));
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var label in labels.Where(l => l.Displayed))
        {
            var text = label.Text?.Trim();
            if (!string.IsNullOrWhiteSpace(text))
                names.Add(text);
        }
        return names;
    }

    /// <summary>Returns the discovery count shown for a species, or null if no entry matches.</summary>
    public int? GetDiscoveryCountFor(string speciesName)
    {
        var entries = _webDriver.FindElements(By.CssSelector(".anidex-entry"));
        foreach (var entry in entries.Where(e => e.Displayed))
        {
            var nameElement = entry.FindElements(By.CssSelector(".anidex-species-name")).FirstOrDefault();
            if (nameElement == null) continue;
            if (!string.Equals(nameElement.Text?.Trim(), speciesName, StringComparison.OrdinalIgnoreCase))
                continue;

            var countElement = entry.FindElements(By.CssSelector(".anidex-discovery-count")).FirstOrDefault();
            if (countElement == null) return null;
            if (int.TryParse(countElement.Text?.Trim(), out var count))
                return count;
        }
        return null;
    }

    /// <summary>Returns the rarity badge text for a species, or null if no matching entry is found.</summary>
    public string? GetRarityFor(string speciesName)
    {
        var entries = _webDriver.FindElements(By.CssSelector(".anidex-entry"));
        foreach (var entry in entries.Where(e => e.Displayed))
        {
            var nameElement = entry.FindElements(By.CssSelector(".anidex-species-name")).FirstOrDefault();
            if (nameElement == null) continue;
            if (!string.Equals(nameElement.Text?.Trim(), speciesName, StringComparison.OrdinalIgnoreCase))
                continue;

            var rarityElement = entry.FindElements(By.CssSelector(".anidex-rarity-badge")).FirstOrDefault();
            var text = rarityElement?.Text?.Trim();
            if (text == null) return null;
            var parenIdx = text.IndexOf('(');
            return parenIdx > 0 ? text[..parenIdx].Trim() : text;
        }
        return null;
    }

    public bool EveryEntryHasNameAndRarityBadge()
    {
        var entries = _webDriver.FindElements(By.CssSelector(".anidex-entry")).Where(e => e.Displayed).ToList();
        if (entries.Count == 0) return false;

        foreach (var entry in entries)
        {
            var name = entry.FindElements(By.CssSelector(".anidex-species-name")).FirstOrDefault();
            var rarity = entry.FindElements(By.CssSelector(".anidex-rarity-badge")).FirstOrDefault();

            if (name == null || string.IsNullOrWhiteSpace(name.Text)) return false;
            if (rarity == null || string.IsNullOrWhiteSpace(rarity.Text)) return false;
        }
        return true;
    }
}
