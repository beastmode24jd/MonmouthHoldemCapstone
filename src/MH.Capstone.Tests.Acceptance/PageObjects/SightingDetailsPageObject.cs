using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class SightingDetailsPageObject
{
    private readonly IWebDriver _webDriver;

    public SightingDetailsPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    public IWebElement Container => _webDriver.FindElement(By.Id("sightingDetailsContainer"));
    public IWebElement Image => _webDriver.FindElement(By.Id("sightingDetailsImage"));
    public IWebElement UploaderName => _webDriver.FindElement(By.Id("sightingDetailsUploaderName"));
    public IWebElement UploaderIcon => _webDriver.FindElement(By.Id("sightingDetailsUploaderIcon"));
    public IWebElement Timestamp => _webDriver.FindElement(By.Id("sightingDetailsTimestamp"));
    public IWebElement Location => _webDriver.FindElement(By.Id("sightingDetailsLocation"));
    public IWebElement Description => _webDriver.FindElement(By.Id("sightingDetailsDescription"));
    public IWebElement Species => _webDriver.FindElement(By.Id("sightingDetailsSpecies"));
    public IWebElement FunFact => _webDriver.FindElement(By.Id("sightingDetailsFunFact"));
    public IWebElement BackToGallery => _webDriver.FindElement(By.Id("backToGalleryLink"));

    public IReadOnlyCollection<IWebElement> NotFoundMessages =>
        _webDriver.FindElements(By.Id("sightingNotFoundMessage"));

    public IReadOnlyCollection<IWebElement> NotFoundBackLinks =>
        _webDriver.FindElements(By.Id("notFoundBackToGalleryLink"));
}
