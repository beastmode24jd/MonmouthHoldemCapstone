using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class WildlifeSearchPageObject
{
    private readonly IWebDriver _webDriver;

    public IWebElement NameInput     => _webDriver.FindElement(By.Id("nameInput"));
    public IWebElement SearchButton  => _webDriver.FindElement(By.CssSelector("#searchForm button[type='submit']"));
    public IWebElement ClearButton   => _webDriver.FindElement(By.Id("clearBtn"));
    public IWebElement PrevButton    => _webDriver.FindElement(By.Id("prevBtn"));
    public IWebElement NextButton    => _webDriver.FindElement(By.Id("nextBtn"));
    public IWebElement ResultCounter => _webDriver.FindElement(By.Id("resultCounter"));
    public IWebElement ResultCard    => _webDriver.FindElement(By.Id("resultCard"));
    public IWebElement SearchStatus  => _webDriver.FindElement(By.Id("searchStatus"));

    public WildlifeSearchPageObject(IWebDriver webDriver, string baseUrl)
    {
        _webDriver = webDriver;
        var url = $"{baseUrl.TrimEnd('/')}/Species/Search";

        if (!string.Equals(webDriver.Url, url, StringComparison.InvariantCultureIgnoreCase))
            webDriver.Navigate().GoToUrl(url);
    }
}
