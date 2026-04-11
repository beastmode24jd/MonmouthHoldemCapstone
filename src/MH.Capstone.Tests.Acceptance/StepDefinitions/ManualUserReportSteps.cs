using System.IO;
using System.Text.Json;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class ManualUserReportSteps
{
    private IWebDriver _driver = null!;
    private WebDriverWait _wait = null!;
    private const string BaseUrl = "https://localhost:7147";
    private const string ReportablePath = "/Map";
    private const string DefaultReason = "Inappropriate content";
    private const string DefaultPassword = "Test@1234";

    private readonly List<string> _createdUserIds = new();
    private readonly Dictionary<string, ApplicationUser> _personaUsers = new();
    private string _currentPersona = string.Empty;

    private static readonly Lazy<string?> _connectionString = new(LoadConnectionString);

    #region Setup and Teardown

    [BeforeScenario]
    public void SetupBrowser()
    {
        // Skip cleanly in CI (or anywhere the dev settings file isn't present) instead of hard-failing.
        if (_connectionString.Value is null)
        {
            Assert.Ignore(
                "Skipping: could not locate MH.Capstone.WebApp/appsettings.Development.json. " +
                "These BDD tests need the local dev connection string to seed/verify the database.");
        }

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--ignore-certificate-errors");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        _driver = new ChromeDriver(options);
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(15));
    }

    [AfterScenario]
    public void Cleanup()
    {
        if (_createdUserIds.Any())
        {
            using var scope = GetServiceScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // Reports FK ReportingUserId -> AspNetUsers.Id; delete reports first to avoid FK violation.
            var reportsToDelete = dbContext.Reports
                .Where(r => _createdUserIds.Contains(r.ReportingUserIdentityId))
                .ToList();
            dbContext.Reports.RemoveRange(reportsToDelete);

            var notificationsToDelete = dbContext.Notifications
                .Where(n => _createdUserIds.Contains(n.LinkedUserIdentityId))
                .ToList();
            dbContext.Notifications.RemoveRange(notificationsToDelete);

            dbContext.SaveChanges();

            foreach (var userId in _createdUserIds)
            {
                var user = dbContext.Users.Find(userId);
                if (user != null)
                {
                    userManager.DeleteAsync(user).GetAwaiter().GetResult();
                }
            }

            dbContext.SaveChanges();
        }

        _driver?.Quit();
        _driver?.Dispose();
    }

    #endregion

    #region Given Steps

    [Given("{word} is logged in and viewing a sighting page")]
    public void GivenPersonaIsLoggedInAndViewingASightingPage(string name)
    {
        var user = EnsurePersona(name);
        LoginUser(user.Email!, DefaultPassword);
        NavigateToReportablePage();
    }

    [Given("{word} submits a report on a sighting page")]
    public void GivenPersonaSubmitsAReportOnASightingPage(string name)
    {
        GivenPersonaIsLoggedInAndViewingASightingPage(name);
        OpenReportModal();
        FillReportForm(DefaultReason, $"Test report submitted by {name}");
        SubmitReportForm();
        WaitForReportSuccessMessage();
    }

    [Given("{word} has already submitted a report for a specific page")]
    public void GivenPersonaHasAlreadySubmittedAReportForASpecificPage(string name)
    {
        GivenPersonaSubmitsAReportOnASightingPage(name);
        // Success closes the modal after ~2s; wait it out so the next action starts clean.
        WaitForReportModalHidden();
    }

    [Given("James is not logged in")]
    public void GivenJamesIsNotLoggedIn()
    {
        _currentPersona = "James";
        // No user created, no login performed.
    }

    [Given("{word} has submitted a report")]
    public void GivenPersonaHasSubmittedAReport(string name)
    {
        GivenPersonaSubmitsAReportOnASightingPage(name);
    }

    #endregion

    #region When Steps

    [When("{word} clicks {string}")]
    public void WhenPersonaClicksButton(string name, string buttonLabel)
    {
        Assert.That(_currentPersona, Is.EqualTo(name), "Scenario persona mismatch");
        Assert.That(buttonLabel, Is.EqualTo("Report this page"));
        OpenReportModal();
    }

    [When("{word} selects a reason and optionally enters a description")]
    public void WhenPersonaSelectsAReasonAndOptionallyEntersADescription(string name)
    {
        FillReportForm(DefaultReason, $"Test report submitted by {name}");
    }

    [When("{word} submits the form")]
    public void WhenPersonaSubmitsTheForm(string name)
    {
        SubmitReportForm();
        WaitForReportSuccessMessage();
    }

    [When("the report is saved")]
    public void WhenTheReportIsSaved()
    {
        // Success message was already awaited in the submit step.
    }

    [When("{word} attempts to submit another report for the same page")]
    public void WhenPersonaAttemptsToSubmitAnotherReportForTheSamePage(string name)
    {
        OpenReportModal();
        FillReportForm("Spam", $"Duplicate attempt by {name}");
        SubmitReportForm();
        WaitForReportErrorMessage();
    }

    [When("James visits a page on the site")]
    public void WhenJamesVisitsAPageOnTheSite()
    {
        _driver.Navigate().GoToUrl(BaseUrl);
        _wait.Until(d => d.FindElement(By.TagName("body")));
    }

    [When("Patricia checks the admin review queue")]
    public void WhenPatriciaChecksTheAdminReviewQueue()
    {
        // No admin UI yet (CSP-101 covers reporting; admin queue UI is a future story).
        // The Then step verifies persistence via a DB query — that's the contract the
        // future admin panel will read from.
    }

    #endregion

    #region Then Steps

    [Then("the report should be saved to the database")]
    public void ThenTheReportShouldBeSavedToTheDatabase()
    {
        var user = _personaUsers[_currentPersona];

        using var scope = GetServiceScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = dbContext.Reports
            .AsNoTracking()
            .FirstOrDefault(r =>
                r.ReportingUserIdentityId == user.Id &&
                r.ReportedPageUrl == ReportablePath);

        Assert.That(report, Is.Not.Null, $"Report for {_currentPersona} on {ReportablePath} should be persisted");
    }

    [Then("{word} should receive an in-app notification confirming the report was received")]
    public void ThenPersonaShouldReceiveAnInAppNotification(string name)
    {
        var user = _personaUsers[name];

        using var scope = GetServiceScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var notification = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.LinkedUserIdentityId == user.Id && n.Title == "Report Received")
            .OrderByDescending(n => n.SentAt)
            .FirstOrDefault();

        Assert.That(notification, Is.Not.Null, $"{name} should have a 'Report Received' notification");
        Assert.That(notification!.Message, Does.Contain("has been received"));
    }

    [Then("it should contain {word}'s UserId, the page URL, the selected reason, and a SubmittedAt timestamp")]
    public void ThenItShouldContainPersonaUserIdMetadata(string name)
    {
        var user = _personaUsers[name];

        using var scope = GetServiceScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var report = dbContext.Reports
            .AsNoTracking()
            .FirstOrDefault(r =>
                r.ReportingUserIdentityId == user.Id &&
                r.ReportedPageUrl == ReportablePath);

        Assert.That(report, Is.Not.Null, "Report should exist in the database");
        Assert.That(report!.ReportingUserIdentityId, Is.EqualTo(user.Id), $"Should have {name}'s UserId");
        Assert.That(report.ReportedPageUrl, Is.EqualTo(ReportablePath), "Should have correct page URL");
        Assert.That(report.Reason, Is.EqualTo(DefaultReason), "Should have correct reason");
        Assert.That(report.SubmittedAt, Is.Not.EqualTo(default(DateTime)), "Should have SubmittedAt timestamp");
        Assert.That(report.SubmittedAt, Is.LessThanOrEqualTo(DateTime.UtcNow.AddMinutes(1)),
            "Timestamp should not be meaningfully in the future");
    }

    [Then("the system should reject the duplicate")]
    public void ThenTheSystemShouldRejectTheDuplicate()
    {
        var user = _personaUsers[_currentPersona];

        using var scope = GetServiceScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var reportCount = dbContext.Reports
            .AsNoTracking()
            .Count(r =>
                r.ReportingUserIdentityId == user.Id &&
                r.ReportedPageUrl == ReportablePath &&
                !r.IsResolved);

        Assert.That(reportCount, Is.EqualTo(1),
            $"Only one unresolved report should exist for {_currentPersona} on {ReportablePath}");
    }

    [Then("{word} should see a message saying she has already reported this content")]
    public void ThenPersonaShouldSeeAlreadyReportedMessage(string name)
    {
        var messageDiv = _driver.FindElement(By.Id("reportMessage"));
        var messageClass = messageDiv.GetAttribute("class") ?? string.Empty;
        var messageText = messageDiv.Text ?? string.Empty;

        Assert.That(messageClass, Does.Contain("alert-danger"),
            "Duplicate submission should show an error alert");

        // The modal JS renders duplicates through one of two branches:
        //   else  -> "You have already reported this page." (from 409 JSON body)
        //   catch -> "Report submission failed. Please wait until your previous report is resolved."
        // Both communicate the same user-facing duplicate rejection.
        var indicatesDuplicate =
            messageText.Contains("already reported", StringComparison.OrdinalIgnoreCase) ||
            messageText.Contains("previous report", StringComparison.OrdinalIgnoreCase);

        Assert.That(indicatesDuplicate, Is.True,
            $"{name} should see an error indicating the report is a duplicate. Actual: '{messageText}'");
    }

    [Then("James should not see the {string} button")]
    public void ThenJamesShouldNotSeeTheReportThisPageButton(string buttonLabel)
    {
        var reportButtons = _driver.FindElements(By.CssSelector("button[data-bs-target='#reportModal']"));
        Assert.That(reportButtons, Is.Empty,
            "Anonymous users should not see the Report this page button");
    }

    [Then("Alex's report should appear with status {string}")]
    public void ThenAlexsReportShouldAppearWithStatus(string status)
    {
        Assert.That(status, Is.EqualTo("Unresolved"), "Only Unresolved status is implemented");
        var alex = _personaUsers["Alex"];

        using var scope = GetServiceScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var unresolved = dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ReportingUserIdentityId == alex.Id && !r.IsResolved)
            .ToList();

        Assert.That(unresolved, Is.Not.Empty, "Alex should have at least one unresolved report");
    }

    #endregion

    #region Helper Methods

    private ApplicationUser EnsurePersona(string name)
    {
        if (_personaUsers.TryGetValue(name, out var existing))
        {
            _currentPersona = name;
            return existing;
        }

        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"Test{name}{suffix}";
        var email = $"{username}@test.com";

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Points = 0,
            IsDeactivated = false
        };

        var result = userManager.CreateAsync(user, DefaultPassword).GetAwaiter().GetResult();
        if (!result.Succeeded)
        {
            throw new Exception(
                $"Failed to create test user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _createdUserIds.Add(user.Id);
        _personaUsers[name] = user;
        _currentPersona = name;
        return user;
    }

    private void LoginUser(string email, string password)
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");
        _wait.Until(d => d.FindElement(By.Id("Email")));

        var emailInput = _driver.FindElement(By.Id("Email"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var submitButton = _driver.FindElement(By.Id("submitBtn"));

        emailInput.SendKeys(email);
        passwordInput.SendKeys(password);

        _wait.Until(d => submitButton.Enabled);
        submitButton.Click();

        _wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    private void NavigateToReportablePage()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}{ReportablePath}");
        _wait.Until(d => d.FindElement(By.CssSelector("button[data-bs-target='#reportModal']")));
    }

    private void OpenReportModal()
    {
        // Scroll to top so nothing covers the fixed-position floating button.
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);");

        var openButton = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("button[data-bs-target='#reportModal']"));
            return (el.Displayed && el.Enabled) ? el : null;
        });

        // JS click sidesteps overlay interception from the Leaflet map tiles and tooltips.
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", openButton!);

        // Wait for Bootstrap modal to be fully shown (fade animation complete).
        _wait.Until(d =>
        {
            var modal = d.FindElement(By.Id("reportModal"));
            var classes = modal.GetAttribute("class") ?? string.Empty;
            var ariaHidden = modal.GetAttribute("aria-hidden");
            return classes.Contains("show") && ariaHidden != "true";
        });

        // Wait for the form controls inside the modal to be interactable.
        _wait.Until(d =>
        {
            var select = d.FindElement(By.Id("reportReason"));
            return select.Displayed && select.Enabled;
        });
    }

    private void FillReportForm(string reason, string description)
    {
        var reasonSelect = new SelectElement(_driver.FindElement(By.Id("reportReason")));
        reasonSelect.SelectByValue(reason);

        var descriptionBox = _driver.FindElement(By.Id("reportDescription"));
        descriptionBox.Clear();
        descriptionBox.SendKeys(description);
    }

    private void SubmitReportForm()
    {
        var submitBtn = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportSubmitBtn"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", submitBtn!);
    }

    private void WaitForReportSuccessMessage()
    {
        _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportMessage"));
            var classes = el.GetAttribute("class") ?? string.Empty;
            return classes.Contains("alert-success");
        });
    }

    private void WaitForReportErrorMessage()
    {
        _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportMessage"));
            var classes = el.GetAttribute("class") ?? string.Empty;
            return classes.Contains("alert-danger");
        });
    }

    private void WaitForReportModalHidden()
    {
        // The modal auto-hides ~2s after a successful submission.
        var longerWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        longerWait.Until(d =>
        {
            var modal = d.FindElement(By.Id("reportModal"));
            var classes = modal.GetAttribute("class") ?? string.Empty;
            return !classes.Contains("show");
        });
    }

    private IServiceScope GetServiceScope()
    {
        var connectionString = _connectionString.Value
                               ?? throw new InvalidOperationException(
                                   "Connection string unavailable. BeforeScenario should have skipped this test.");

        // Azure SQL serverless tiers can take ~30s to wake from pause. Give the first
        // connection plenty of headroom and retry transient failures automatically.
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = 120
        };
        connectionString = builder.ConnectionString;

        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(120);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null);
            }));

        services.AddDbContext<CacheDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(120);
                sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(15),
                    errorNumbersToAdd: null);
            }));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 1;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddLogging();

        return services.BuildServiceProvider().CreateScope();
    }

    private static string? LoadConnectionString()
    {
        var settingsPath = FindWebAppDevSettings();
        if (settingsPath is null)
        {
            return null;
        }

        using var stream = File.OpenRead(settingsPath);
        using var doc = JsonDocument.Parse(stream);

        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings) &&
            connectionStrings.TryGetProperty("DataDb", out var dataDb) &&
            dataDb.ValueKind == JsonValueKind.String)
        {
            return dataDb.GetString();
        }

        return null;
    }

    private static string? FindWebAppDevSettings()
    {
        // Walk up from the test bin directory until we find a parent that contains
        // MH.Capstone.WebApp/appsettings.Development.json (i.e. the src/ folder).
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "MH.Capstone.WebApp", "appsettings.Development.json");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    #endregion
}
