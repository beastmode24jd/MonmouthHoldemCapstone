using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

/// <summary>
/// CSP-142: Element selectors for the personal Anidex page (/anidex).
/// </summary>
[ExcludeFromCodeCoverage]
public class AnidexPageObject
{
    private readonly IWebDriver _webDriver;

    public AnidexPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    public IReadOnlyCollection<IWebElement> Entries =>
        _webDriver.FindElements(By.CssSelector(".anidex-entry"));

    public IReadOnlyCollection<IWebElement> SpeciesNameLabels =>
        _webDriver.FindElements(By.CssSelector(".anidex-species-name"));

    public IReadOnlyCollection<IWebElement> RarityBadges =>
        _webDriver.FindElements(By.CssSelector(".anidex-rarity-badge"));

    public IReadOnlyCollection<IWebElement> EmptyStates =>
        _webDriver.FindElements(By.Id("anidexEmptyState"));
}
