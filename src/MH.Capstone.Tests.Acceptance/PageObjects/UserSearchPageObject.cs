// Sprint 5, CSP-54
using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class UserSearchPageObject
{
    private readonly IWebDriver _driver;

    public UserSearchPageObject(IWebDriver driver)
    {
        _driver = driver;
    }

    public IWebElement SearchInput => _driver.FindElement(By.Id("userSearchInput"));
    public IWebElement PagingInfo => _driver.FindElement(By.Id("pagingInfo"));
    public IWebElement PrevBtn => _driver.FindElement(By.Id("prevBtn"));
    public IWebElement NextBtn => _driver.FindElement(By.Id("nextBtn"));
    public IWebElement ResultsContainer => _driver.FindElement(By.Id("searchResultsContainer"));

    public bool NoResultsMessageVisible =>
        _driver.FindElements(By.Id("noUsersFoundMessage")).Count > 0;

    public IReadOnlyList<IWebElement> ResultItems =>
        _driver.FindElements(By.CssSelector(".user-result-item"));

    public void TypeSearch(string query)
    {
        SearchInput.Clear();
        SearchInput.SendKeys(query);
    }
}
