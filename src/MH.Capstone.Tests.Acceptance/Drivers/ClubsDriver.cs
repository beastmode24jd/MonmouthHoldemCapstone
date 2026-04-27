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

    /// <summary>
    /// Opens the create-club modal only when it is not already visible.
    /// Safe to call when the modal may already be open (Scenario 2 opens it first,
    /// then WhenISelectValidOptions calls this as a guard).
    /// </summary>
    public void EnsureCreateClubModalOpen()
    {
        try
        {
            var isAlreadyOpen = _webDriver.FindElement(By.Id("newClubModal"))
                .GetAttribute("class")?.Contains("show") == true;

            if (!isAlreadyOpen)
                OpenCreateClubModal();
            else
                TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Create-Club modal already open — skipping open.");
        }
        catch
        {
            OpenCreateClubModal();
        }
    }

    /// <summary>
    /// Opens the Invite Member modal on a ClubPage, searches by display name,
    /// selects the first matching result, and submits the invite form.
    /// Waits for the success banner to confirm the POST completed.
    /// </summary>
    public void InviteMemberByDisplayName(string displayName)
    {
        var inviteBtn = _wait.Until(d => d.FindElement(By.Id("inviteMemberBtn")));
        ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].click();", inviteBtn);
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Clicked 'Invite Member' button.");

        _wait.Until(d =>
            d.FindElement(By.Id("inviteMemberModal"))
             .GetAttribute("class")?.Contains("show") == true);
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Invite modal visible.");

        var searchInput = _wait.Until(d => d.FindElement(By.Id("memberSearchInput")));
        searchInput.Clear();
        searchInput.SendKeys(displayName);
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Typed '{displayName}' into search input.");

        // Wait for the debounce + API call to populate results.
        var resultBtn = _wait.Until(d =>
        {
            var results = d.FindElements(By.CssSelector("#memberSearchResults .list-group-item"));
            return results.FirstOrDefault(r =>
                r.Text.Contains(displayName, StringComparison.OrdinalIgnoreCase) && r.Displayed);
        });

        resultBtn.Click();
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Selected search result for '{displayName}'.");

        // Wait for the "Sending invite to: X" confirmation banner inside the modal.
        _wait.Until(d =>
        {
            var display = d.FindElement(By.Id("selectedUserDisplay"));
            return !(display.GetAttribute("class")?.Contains("d-none") ?? true);
        });

        var sendBtn = _wait.Until(d => d.FindElement(By.Id("sendInviteBtn")));
        sendBtn.Click();
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Clicked 'Send Invite'.");

        // POST redirects back to ClubPage with a TempData success banner.
        _wait.Until(d => d.FindElements(By.CssSelector(".alert-success")).Count > 0);
        TestContext.Out.WriteLine($"[{nameof(ClubsDriver)}] Invite success banner confirmed.");
    }

    /// <summary>
    /// Returns true when a pending-invite card on the clubs landing page has a
    /// club name matching <paramref name="clubName"/> (case-insensitive).
    /// </summary>
    public bool IsPendingInviteVisible(string clubName)
    {
        try
        {
            return new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5)).Until(d =>
            {
                var cards = d.FindElements(By.CssSelector("[id^='pendingInvite_']"));
                return cards.Any(card =>
                    card.Displayed &&
                    card.FindElements(By.CssSelector("strong"))
                        .Any(t => t.Text.Contains(clubName, StringComparison.OrdinalIgnoreCase)));
            });
        }
        catch
        {
            return false;
        }
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
