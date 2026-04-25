using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.PageObjects;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

[ExcludeFromCodeCoverage]
public class ClubsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public ClubsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToLandingPage()
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Clubs");
        WaitForPageReady();
    }

    /// <summary>
    /// Clicks the "Create your own Club" button to open the Bootstrap modal,
    /// then waits until the modal's 'show' class is applied by Bootstrap JS.
    /// </summary>
    public void OpenCreateClubModal()
    {
        // There can be two "Create your own Club" buttons on the page (one in the
        // empty-state div, one always visible). Click the first displayed one.
        var openBtn = _wait.Until(d =>
            d.FindElements(By.CssSelector("button[onclick='showClubModal()']"))
             .FirstOrDefault(e => e.Displayed));

        openBtn?.Click();
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Clicked 'Create your own Club' button.");

        // Bootstrap animates the modal in; wait for the 'show' class to settle.
        _wait.Until(d =>
            d.FindElement(By.Id("newClubModal"))
             .GetAttribute("class")?.Contains("show") == true);

        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Create-Club modal is visible.");
    }

    /// <summary>
    /// Fills the name, description, and visibility inputs inside the open modal.
    /// Does not submit.
    /// </summary>
    public void FillCreateClubModal(string name, string? description, bool isPublic)
    {
        var page = new ClubsLandingPageObject(_webDriver, _baseUrl);

        page.ModalClubNameInput.Clear();
        page.ModalClubNameInput.SendKeys(name);

        if (!string.IsNullOrEmpty(description))
        {
            page.ModalDescInput.Clear();
            page.ModalDescInput.SendKeys(description);
        }

        // Radio buttons inside a modal need JS click to avoid interactability issues.
        var radioId = isPublic ? "radioPublic" : "radioPrivate";
        ((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "document.getElementById(arguments[0]).click();", radioId);

        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Filled modal: name='{name}' public={isPublic}.");
    }

    /// <summary>
    /// Clicks the "Create Club" submit button inside the modal and waits for the
    /// POST to complete and the browser to redirect to the new ClubPage URL.
    /// </summary>
    public void SubmitCreateClubModal()
    {
        var page = new ClubsLandingPageObject(_webDriver, _baseUrl);
        ((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "arguments[0].click();", page.ModalConfirmBtn);

        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Submitted Create-Club form.");

        // Wait for redirect to the new club's detail page.
        _wait.Until(d =>
            d.Url.Contains("/Clubs/ClubPage/", StringComparison.InvariantCultureIgnoreCase));

        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Redirected to club page: {_webDriver.Url}");
    }

    /// <summary>Returns true when the browser is on a ClubPage URL.</summary>
    public bool IsOnClubPage()
    {
        try
        {
            return _wait.Until(d =>
                d.Url.Contains("/Clubs/ClubPage/", StringComparison.InvariantCultureIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Clicks the "My Clubs" filter button on the landing page.</summary>
    public void SwitchToMyClubsFilter()
    {
        var page = new ClubsLandingPageObject(_webDriver, _baseUrl);
        page.FilterMineBtn.Click();

        // Wait briefly for the JS filter to run.
        new WebDriverWait(_webDriver, TimeSpan.FromSeconds(2)).Until(d =>
            d.FindElement(By.Id("filterMine"))
             .GetAttribute("class")?.Contains("active") == true);

        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Switched to 'My Clubs' filter.");
    }

    /// <summary>
    /// Returns true when at least one visible club card on the current page
    /// has a title matching <paramref name="clubName"/> (case-insensitive).
    /// </summary>
    public bool IsClubCardVisible(string clubName)
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
            {
                var cards = d.FindElements(By.CssSelector(".club-card-wrapper"));
                return cards.Any(card =>
                    card.Displayed &&
                    card.FindElements(By.CssSelector(".card-title"))
                        .Any(t => t.Text.Contains(clubName, StringComparison.OrdinalIgnoreCase)));
            });
        }
        catch
        {
            return false;
        }
    }

    private void WaitForPageReady()
    {
        try
        {
            _wait.Until(d =>
            {
                try
                {
                    var ready = ((IJavaScriptExecutor)d)
                        .ExecuteScript("return document.readyState")?.ToString();
                    return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
                }
                catch { return false; }
            });
        }
        catch
        {
            // ignore — best-effort wait
        }
    }
}
