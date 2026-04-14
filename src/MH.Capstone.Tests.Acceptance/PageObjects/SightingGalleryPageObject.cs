using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class SightingGalleryPageObject
{
    private readonly IWebDriver _webDriver;

    public IWebElement FilterAllBtn => _webDriver.FindElement(By.Id("filterAll"));
    public IWebElement FilterMineBtn => _webDriver.FindElement(By.Id("filterMine"));
    public IWebElement EmptyStateMine => _webDriver.FindElement(By.Id("emptyStateMine"));

    public IReadOnlyCollection<IWebElement> AllCardWrappers =>
        _webDriver.FindElements(By.CssSelector(".sighting-card-wrapper"));

    public IReadOnlyCollection<IWebElement> AttributionLabels =>
        _webDriver.FindElements(By.CssSelector(".sighting-attribution"));

    public SightingGalleryPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }
}
