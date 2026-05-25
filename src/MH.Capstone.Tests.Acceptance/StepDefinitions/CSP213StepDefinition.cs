using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using MH.Capstone.Tests.Acceptance.Seeding;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "audit")]
[ExcludeFromCodeCoverage]
public class CSP213StepDefinitions
{
    private readonly DisplayNameDriver _displayNameDriver;
    private readonly AuthenticationDriver _authenticationDriver;
    private readonly EmailVerificationDriver _emailVerificationDriver;
    private readonly ProfileDriver _profileDriver;
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public CSP213StepDefinitions(
        DisplayNameDriver displayNameDriver,
        AuthenticationDriver authenticationDriver,
        EmailVerificationDriver emailVerificationDriver,
        ProfileDriver profileDriver,
        AccountSettingsDriver accountSettingsDriver,
        IWebDriver driver,
        WebDriverWait wait,
        AcceptanceTestSettings settings)
    {
        _displayNameDriver = displayNameDriver;
        _authenticationDriver = authenticationDriver;
        _emailVerificationDriver = emailVerificationDriver;
        _profileDriver = profileDriver;
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
    }

    // Wipe + re-seed before every @audit scenario so
    //      audit and report counts stay consistent.
    [BeforeScenario("audit")]
    public async Task BeforeProfileScenario()
    {
        await TestWebAppHost.ResetSeedDataAsync();
        _driver.Manage().Cookies.DeleteAllCookies();
    }

    // Scenario 1: Audit log not visible to standard user

    [Given("Alex is logged in")]
    public void GivenAlexIsLoggedIn()
    {
        try { _authenticationDriver.LogoutUser(); } catch { /* already logged out */ }
        _authenticationDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("Alex navigates directly to the audit log page URL")]
    public void WhenAlexNavigatesDirectlyToTheAuditLogPageURL()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Audit-Logs");
    }

    [Then("Alex receives an access denied response")]
    public void ThenAlexReceivesAnAccessDeniedResponse()
    {
        // Assert that the user was redirected to the path set in Program.cs.
        // We use .Contain() because ASP.NET Core often appends a ?ReturnUrl= parameter.
        _wait.Until(d => d.Url.Contains("/Account/AccessDenied"));
        _driver.Url.Should().Contain("/Account/AccessDenied");
    }

    // Scenario 2: Resolving a report creates an audit entry

    // This first Given step also repeats for Scenarios 3 and 4
    [Given("an admin is logged in")]
    public void GivenAnAdminIsLoggedIn()
    {
        try { _authenticationDriver.LogoutUser(); } catch { /* already logged out */ }
        _authenticationDriver.PreformLoginForUser("patricia@test.com", "Capstone26!");
    }

    [Given("an unresolved report exists")]
    public void GivenAnUnresolvedReportExists()
    {
        // Navigate to the reports queue
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/Reports");

        // Wait for the table to populate
        _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);

        // Find all rows, then find the first one where the resolution checkbox is NOT selected
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        var unresolvedRow = rows.FirstOrDefault(r => !r.FindElement(By.CssSelector(".resolution-toggle")).Selected);

        unresolvedRow.Should().NotBeNull("Because AcceptanceTestSeeder seeds 2 unresolved reports, one should be found.");
    }

    [When("the admin resolves the report")]
    public void WhenTheAdminResolvesTheReport()
    {
        // Re-find the unresolved row right before clicking to avoid a StaleElementReferenceException
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        var unresolvedRow = rows.First(r => !r.FindElement(By.CssSelector(".resolution-toggle")).Selected);

        // Click the details button on THAT specific row
        unresolvedRow.FindElement(By.CssSelector(".details-btn")).Click();

        // Wait for modal, check the box, and confirm (Reusing logic from CSP-179)
        _wait.Until(d => d.FindElement(By.Id("reportDetailsModal")).Displayed);
        
        _driver.FindElement(By.Id("modalIsResolved")).Click();

        // Grab a reference to the button BEFORE clicking it
        var confirmBtn = _driver.FindElement(By.Id("confirmResolveBtn"));
        confirmBtn.Click();

        // Catch the page reload triggered by reportModal.js to prevent a StaleElementReferenceException
        _wait.Until(d => 
        {
            try 
            {
                // If we can still access properties on the element, the page hasn't reloaded yet
                return !confirmBtn.Displayed;
            }
            catch (StaleElementReferenceException) 
            {
                // This exception proves the old DOM was successfully destroyed by the reload
                return true; 
            }
        });
    }

    // Re-used step in Scenarios 3 and 4.
    [When("the admin navigates to the audit log page")]
    public void WhenTheAdminNavigatesToTheAuditLogPage()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Audit-Logs");
    }

    [Then("an entry is visible for the Report Resolved action")]
    public void ThenAnEntryIsVisibleForTheReportResolvedAction()
    {
        var pageText = _driver.FindElement(By.TagName("body")).Text;
        
        // Assert that the specific action type is rendered on the screen
        pageText.Should().Contain("Report Resolved", "The audit log should display the Report Resolved action type.");
    }

    [Then("the entry shows the admin's display name and a recent timestamp")]
    public void ThenTheEntryShowsTheAdminsDisplayNameAndARecentTimestamp()
    {
        var pageText = _driver.FindElement(By.TagName("body")).Text;
        
        // Patricia is our seeded admin's DisplayName
        pageText.Should().Contain("Patricia", "The audit log should record the admin who performed the action.");
        
        // Because of timezones and rendering formats, checking for the current year/month or 'Just now' 
        // is usually safer than an exact DateTime match in Acceptance Tests.
        var currentYear = DateTime.UtcNow.Year.ToString();
        pageText.Should().Contain(currentYear, "The audit log should display a recent timestamp.");
    }

    // Scenario 3: Locking a user creates an audit log entry

    // Admin log-in handled in Scenario 2 steps

    [Given("an active user account exists")]
    public void GivenAnActiveUserAccountExists()
    {
        // Navigate to User Management page
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/Manage");

        // Search for "Alex"
        var searchInput = _driver.FindElement(By.Id("activeUserSearch"));
        var hiddenEmailInput = _driver.FindElement(By.CssSelector("#lockForm .selected-email"));

        searchInput.Clear();
        searchInput.SendKeys("Alex");

        // Wait until the JS has populated the hidden email field based on the search
        _wait.Until(d => {
            var val = hiddenEmailInput.GetAttribute("value");
            return !string.IsNullOrEmpty(val) && val.Contains("@");
        });

        _driver.FindElement(By.Id("lockBtn")).Click();
    }

    [When("the admin locks that user account")]
    public void WhenTheAdminLocksThatUserAccount()
    {
        // Wait for the password confirmation modal to show
        _wait.Until(d => d.FindElement(By.Id("adminPasswordModal")).Displayed);

        var adminPasswordInput = _driver.FindElement(By.Id("modalAdminPassword"));
        adminPasswordInput.SendKeys("Capstone26!");

        _driver.FindElement(By.Id("confirmAuthBtn")).Click();
    }

    // Audit page navigation step reused from Scenario 2

    [Then("an entry is visible for the locking action")]
    public void ThenAnEntryIsVisibleForTheLockingAction()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Audit-Logs");

        // Wait to ensure the table has loaded
        _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);

        // Grab the 5th cell (td) of the first row (tr)
        var actionCell = _driver.FindElement(By.CssSelector("table tbody tr:first-child td:nth-child(5)"));
        
        // Check the Audit logs for "User Locked"
        actionCell.Text.Should().Contain("User Locked");
    }

    [Then("the entry references the locked user")]
    public void ThenTheEntryReferencesTheLockedUser()
    {
        // Grab the 3rd cell (td) of the first row (tr)
        var targetUserCell = _driver.FindElement(By.CssSelector("table tbody tr:first-child td:nth-child(3)"));
        
        // Verify the locked user's name is in the cell
        targetUserCell.Text.Should().Contain("Alex");
    }

    // Scenario 4: Unlocking a user creates an audit log entry

    [Given("a locked user account exists")]
    public void GivenALockedUserAccountExists()
    {
        // Navigate to User Management page
        _driver.Navigate().GoToUrl($"{_baseUrl}/Admin/Manage");

        // Lock lily out of her account for setup.
        // We check audit logs by Category and Target User, so this won't confuse
        //      the later steps.
        var searchInput = _driver.FindElement(By.Id("activeUserSearch"));
        var hiddenEmailInput = _driver.FindElement(By.CssSelector("#lockForm .selected-email"));

        searchInput.Clear();
        searchInput.SendKeys("Lily");

        // Wait until the JS has populated the hidden email field based on the search
        _wait.Until(d => {
            var val = hiddenEmailInput.GetAttribute("value");
            return !string.IsNullOrEmpty(val) && val.Contains("@");
        });

        _driver.FindElement(By.Id("lockBtn")).Click();

        // Wait for the password confirmation modal to show
        _wait.Until(d => d.FindElement(By.Id("adminPasswordModal")).Displayed);

        var adminPasswordInput = _driver.FindElement(By.Id("modalAdminPassword"));
        adminPasswordInput.SendKeys("Capstone26!");

        // Grab a reference to the button BEFORE clicking it
        var confirmBtn = _driver.FindElement(By.Id("confirmAuthBtn"));
        
        // Click the button to trigger form.submit()
        confirmBtn.Click();

        // Catch the page reload to prevent race conditions in the next step
        _wait.Until(d => 
        {
            try 
            {
                // If we can still access properties on the element, the page hasn't reloaded yet
                return !confirmBtn.Displayed;
            }
            catch (StaleElementReferenceException) 
            {
                // This exception proves the old DOM was successfully destroyed by the reload
                return true; 
            }
        });
    }

    [When("the admin unlocks that user account")]
    public void WhenTheAdminUnlocksThatUserAccount()
    {
        // Flip it.
        var unlockSearchInput = _driver.FindElement(By.Id("lockedUserSearch"));
        unlockSearchInput.Clear();
        unlockSearchInput.SendKeys("Lily");

        // Wait for the JavaScript debounce and fetch to complete
        // We know it's done when the hidden email field inside the unlock form gets a value
        var hiddenEmailField = _driver.FindElement(By.CssSelector("#unlockForm .selected-email"));
        _wait.Until(d => !string.IsNullOrEmpty(hiddenEmailField.GetAttribute("value")));

        // Now that the email is populated, click the Restore Access button
        _driver.FindElement(By.Id("unlockBtn")).Click();

        // Wait for Bootstrap Modal to finish its animation and become visible
        var modalPasswordInput = _wait.Until(d => 
        {
            var element = d.FindElement(By.Id("modalAdminPassword"));
            return element.Displayed ? element : null;
        });

        var adminPasswordInput = _driver.FindElement(By.Id("modalAdminPassword"));
        adminPasswordInput.SendKeys("Capstone26!");

        _driver.FindElement(By.Id("confirmAuthBtn")).Click();
    }

    // Audit Log page navigation step given in Scenario 2

    [Then("an entry is visible for the unlocking action")]
    public void ThenAnEntryIsVisibleForTheUnlockingAction()
    {
        _driver.Navigate().GoToUrl($"{_baseUrl}/Audit-Logs");

        // Wait to ensure the table has loaded
        _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);

        // Grab the 5th cell (td) of the first row (tr)
        var actionCell = _driver.FindElement(By.CssSelector("table tbody tr:first-child td:nth-child(5)"));
        
        // Check the Audit logs for "User Unocked"
        actionCell.Text.Should().Contain("User Unlocked");
    }

    [Then("the entry references the unlocked user")]
    public void ThenTheEntryReferencesTheUnlockedUser()
    {
        // Grab the 3rd cell (td) of the first row (tr)
        var targetUserCell = _driver.FindElement(By.CssSelector("table tbody tr:first-child td:nth-child(3)"));
        
        // Verify the locked user's name is in the cell
        targetUserCell.Text.Should().Contain("Lily");
    }
}