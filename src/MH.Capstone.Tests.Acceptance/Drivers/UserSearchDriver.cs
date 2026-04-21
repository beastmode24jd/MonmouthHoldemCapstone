// Sprint 5, CSP-54
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Helpers;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class UserSearchDriver
{
    private readonly IWebDriver _driver;
    private readonly string _baseUrl;
    private readonly WebDriverWait _wait;
    private readonly UserSearchPageObject _page;

    public UserSearchDriver(IWebDriver driver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _driver = driver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
        _page = new UserSearchPageObject(driver);
    }

    public void NavigateToSearch()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/user/search");
        _wait.Until(d => d.FindElement(By.Id("userSearchInput")));
    }

    public void EnterSearchQuery(string query)
    {
        _page.TypeSearch(query);
        // Wait for AJAX debounce + response (debounce is 300ms)
        System.Threading.Thread.Sleep(600);
        _wait.Until(d => d.FindElement(By.Id("pagingInfo")));
    }

    public bool IsNoResultsMessageVisible() => _page.NoResultsMessageVisible;

    public bool IsPrevButtonDisabled() =>
        _page.PrevBtn.GetAttribute("disabled") != null;

    public bool IsNextButtonDisabled() =>
        _page.NextBtn.GetAttribute("disabled") != null;

    public bool IsPrevButtonEnabled() => !IsPrevButtonDisabled();
    public bool IsNextButtonEnabled() => !IsNextButtonDisabled();

    public string GetPagingInfoText() => _page.PagingInfo.Text;

    public IReadOnlyList<IWebElement> GetResultItems() => _page.ResultItems;

    public int GetResultCount() => _page.ResultItems.Count;

    public string GetResultUsernameAt(int index) =>
        _page.ResultItems[index].FindElement(By.CssSelector(".user-result-username")).Text;

    public void ClickResultAt(int index) =>
        _page.ResultItems[index].Click();

    public string GetCurrentUrl() => _driver.Url;
}
