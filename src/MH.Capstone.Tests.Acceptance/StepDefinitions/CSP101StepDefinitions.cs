using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP101StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AcceptanceTestSettings _settings;
    private readonly AuthenticationDriver _authDriver;

    private string BaseUrl => _settings.BaseUrl.TrimEnd('/');
    private const string ReportablePath = "/About";
    private const string DefaultReason = "Inappropriate content";
    private const string DefaultPassword = "Capstone26!";

    private readonly List<string> _createdUserIds = new();
    private readonly Dictionary<string, ApplicationUser> _personaUsers = new();
    private string _currentPersona = string.Empty;

    #region Setup and Teardown

    public CSP101StepDefinitions(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, AuthenticationDriver authDriver)
    {
        _driver   = driver;
        _wait     = wait;
        _settings = settings;
        _authDriver = authDriver;
    }

    #endregion

    #region Given Steps

    [Given("{word} is logged in and viewing a sighting page")]
    public void GivenPersonaIsLoggedInAndViewingASightingPage(string name)
    {
        var user = EnsurePersona(name);
        _authDriver.PreformLoginForUser(user.Email!, DefaultPassword);
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
        _currentPersona.Should().Be(name, "scenario persona mismatch");
        buttonLabel.Should().Be("Report this page");
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

        report.Should().NotBeNull($"report for {_currentPersona} on {ReportablePath} should be persisted");
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

        notification.Should().NotBeNull($"{name} should have a 'Report Received' notification");
        notification!.Message.Should().Contain("has been received");
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

        report.Should().NotBeNull("the report should exist in the database");
        report!.ReportingUserIdentityId.Should().Be(user.Id, $"should have {name}'s UserId");
        report.ReportedPageUrl.Should().Be(ReportablePath, "should have the correct page URL");
        report.Reason.Should().Be(DefaultReason, "should have the correct reason");
        report.SubmittedAt.Should().NotBe(default, "should have a SubmittedAt timestamp");
        report.SubmittedAt.Should().BeBefore(DateTime.UtcNow.AddMinutes(1),
            "the timestamp should not be in the future");
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

        reportCount.Should().Be(1,
            $"only one unresolved report should exist for {_currentPersona} on {ReportablePath}");
    }

    [Then("{word} should see a message saying she has already reported this content")]
    public void ThenPersonaShouldSeeAlreadyReportedMessage(string name)
    {
        var messageDiv = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportMessage"));
            return (el.Displayed) ? el : null;
        });
        var messageClass = messageDiv.GetAttribute("class") ?? string.Empty;
        var messageText = messageDiv.Text ?? string.Empty;

        messageClass.Should().Contain("alert-danger",
            "a duplicate submission should show an error alert");

        // The modal JS renders duplicates through one of two branches:
        //   else  -> "You have already reported this page." (from 409 JSON body)
        //   catch -> "Report submission failed. Please wait until your previous report is resolved."
        // Both communicate the same user-facing duplicate rejection.
        var indicatesDuplicate =
            messageText.Contains("already reported", StringComparison.OrdinalIgnoreCase) ||
            messageText.Contains("previous report", StringComparison.OrdinalIgnoreCase);

        indicatesDuplicate.Should().BeTrue(
            $"{name} should see an error indicating the report is a duplicate. Actual: '{messageText}'");
    }

    [Then("James should not see the {string} button")]
    public void ThenJamesShouldNotSeeTheReportThisPageButton(string buttonLabel)
    {
        // Wait briefly to assert absence without changing global implicit waits
        var shortWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(1));
        shortWait.Until(d => d.FindElements(By.CssSelector("button[data-bs-target='#reportModal']")).Count == 0);
        var reportButtons = _driver.FindElements(By.CssSelector("button[data-bs-target='#reportModal']"));
        reportButtons.Should().BeEmpty("anonymous users should not see the 'Report this page' button");
    }

    [Then("Alex's report should appear with status {string}")]
    public void ThenAlexsReportShouldAppearWithStatus(string status)
    {
        status.Should().Be("Unresolved", "only Unresolved status is currently implemented");
        var alex = _personaUsers["Alex"];

        using var scope = GetServiceScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var unresolved = dbContext.Reports
            .AsNoTracking()
            .Where(r => r.ReportingUserIdentityId == alex.Id && !r.IsResolved)
            .ToList();

        unresolved.Should().NotBeEmpty("Alex should have at least one unresolved report");
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

        var suffix   = Guid.NewGuid().ToString("N")[..8];
        var username = $"Test{name}{suffix}";
        var email    = $"{username}@test.com";

        var user = new ApplicationUser
        {
            UserName      = email,
            Email         = email,
            EmailConfirmed = true,
            Points        = 0,
            IsDeactivated = false
        };

        var result = userManager.CreateAsync(user, DefaultPassword).GetAwaiter().GetResult();
        if (!result.Succeeded)
            throw new Exception(
                $"Failed to create test user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        _createdUserIds.Add(user.Id);
        _personaUsers[name] = user;
        _currentPersona = name;
        return user;
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
            var modal     = d.FindElement(By.Id("reportModal"));
            var classes   = modal.GetAttribute("class") ?? string.Empty;
            var ariaHidden = modal.GetAttribute("aria-hidden");
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Modal classes: {classes}, aria-hidden: {ariaHidden}");
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
        var reasonElement = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportReason"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        var reasonSelect = new SelectElement(reasonElement);
        reasonSelect.SelectByValue(reason);

        var descriptionBox = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportDescription"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
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
            var modal   = d.FindElement(By.Id("reportModal"));
            var classes = modal.GetAttribute("class") ?? string.Empty;
            return !classes.Contains("show");
        });
    }

    private IServiceScope GetServiceScope()
    {
        var webAppPath = _settings.WebAppContentRoot;
        var configFile = Path.Combine(webAppPath, "appsettings.Acceptance.json");

        if (!File.Exists(configFile))
            Assert.Ignore("Skipped: appsettings.Acceptance.json not found.");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webAppPath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Acceptance.json", optional: false)
            .AddJsonFile("appsettings.Acceptance.Local.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DataDb")
            ?? throw new InvalidOperationException("Connection string 'DataDb' not found.");

        // Azure SQL serverless tiers can take ~30s to wake from pause — give the first
        // connection plenty of headroom and retry transient failures automatically.
        var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = 120 };
        connectionString = builder.ConnectionString;

        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(120);
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(15), null);
            }));

        services.AddDbContext<CacheDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.CommandTimeout(120);
                sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(15), null);
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

    #endregion
}
