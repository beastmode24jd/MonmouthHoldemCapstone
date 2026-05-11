using System.Diagnostics.CodeAnalysis;
using OpenQA.Selenium;

namespace MH.Capstone.Tests.Acceptance.PageObjects;

[ExcludeFromCodeCoverage]
public class LeaderboardPageObject
{
    private readonly IWebDriver _webDriver;

    public LeaderboardPageObject(IWebDriver webDriver)
    {
        _webDriver = webDriver;
    }

    public IWebElement RowFor(string userId) =>
        _webDriver.FindElement(By.Id($"user-{userId}"));

    public IReadOnlyCollection<IWebElement> RowsFor(string userId) =>
        _webDriver.FindElements(By.Id($"user-{userId}"));

    public IWebElement? LiveStatusBanner =>
        _webDriver.FindElements(By.Id("liveStatusBanner")).FirstOrDefault();

    public IReadOnlyCollection<IWebElement> LiveNotificationToasts =>
        _webDriver.FindElements(By.Id("liveNotificationToast"));

    public string PointsTextFor(string userId)
    {
        var row = RowFor(userId);
        var pointsCell = row.FindElement(By.CssSelector("td:last-child"));
        return pointsCell.Text.Trim();
    }
}
