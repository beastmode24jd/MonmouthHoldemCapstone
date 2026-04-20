using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP97StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AcceptanceTestSettings _settings;
    private readonly AuthenticationDriver _authDriver;

    private string BaseUrl => _settings.BaseUrl.TrimEnd('/');

    // Test data tracking for cleanup
    private readonly List<string> _createdUserIds = new();
    private readonly string _testRunId = Guid.NewGuid().ToString("N")[..8];

    // Named persona tracking
    private readonly Dictionary<string, ApplicationUser> _personas = new();

    public CSP97StepDefinitions(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, AuthenticationDriver authDriver)
    {
        _driver   = driver;
        _wait     = wait;
        _settings = settings;
        _authDriver = authDriver;
    }

    [AfterScenario("leaderboard")]
    public void CleanupTestUsers()
    {
        if (!_createdUserIds.Any())
            return;

        using var scope = GetServiceScope();
        var dbContext   = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var userId in _createdUserIds)
        {
            var user = dbContext.Users.Find(userId);
            if (user != null)
                userManager.DeleteAsync(user).GetAwaiter().GetResult();
        }

        dbContext.SaveChanges();
    }

    #region Given Steps

    [Given(@"I am on the home page")]
    public void GivenIAmOnTheHomePage()
    {
        _driver.Navigate().GoToUrl(BaseUrl);
        _wait.Until(d => d.Title != "");
    }

    [Given(@"(.+) has (\d+) points")]
    public void GivenPersonaHasPoints(string name, int points)
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext   = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = CreateTestUser(userManager, dbContext, $"{name}_{_testRunId}", "Test@1234", points);
        _personas[name] = user;
    }

    [Given(@"(.+) is logged in with (\d+) points")]
    public void GivenPersonaIsLoggedInWithPoints(string name, int points)
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext   = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = CreateTestUser(userManager, dbContext, $"{name}_{_testRunId}", "Test@1234", points);
        _personas[name] = user;

        LoginUser($"{name}_{_testRunId}@test.com", "Test@1234");
    }

    [Given(@"there are more than 30 users in the system")]
    public void GivenThereAreMoreThan30UsersInTheSystem()
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext   = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (int i = 1; i <= 35; i++)
            CreateTestUser(userManager, dbContext, $"User{i:D3}_{_testRunId}", "Test@1234", 1000 - (i * 10));
    }

    #endregion

    #region When Steps

    [When(@"I view the navigation bar")]
    public void WhenIViewTheNavigationBar()
    {
        _wait.Until(d => d.FindElement(By.TagName("nav")));
    }

    [When(@"I view the leaderboard")]
    public void WhenIViewTheLeaderboard()
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");
        _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
    }

    [When(@"(.+) views the leaderboard")]
    public void WhenPersonaViewsTheLeaderboard(string name)
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Leaderboard");
        _wait.Until(d => d.FindElements(By.CssSelector("table tbody tr")).Count > 0);
    }

    #endregion

    #region Then Steps

    [Then(@"I should see a ""(.*)"" link")]
    public void ThenIShouldSeeALink(string linkText)
    {
        var navLinks = _driver.FindElements(By.CssSelector("nav a"));
        var matchingLink = navLinks.FirstOrDefault(l =>
            l.Text.Trim().Contains(linkText, StringComparison.OrdinalIgnoreCase));

        matchingLink.Should().NotBeNull($"the navigation bar should contain a '{linkText}' link");
        matchingLink!.Displayed.Should().BeTrue($"the '{linkText}' link should be visible");
    }

    [Then(@"users should be displayed in descending order by points")]
    public void ThenUsersShouldBeDisplayedInDescendingOrderByPoints()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        rows.Count.Should().BeGreaterThan(0, "the leaderboard should have user entries");

        var points = ExtractPointsFromRows(rows);
        for (int i = 0; i < points.Count - 1; i++)
        {
            points[i].Should().BeGreaterThanOrEqualTo(points[i + 1],
                $"points at position {i} ({points[i]}) should be >= position {i + 1} ({points[i + 1]})");
        }
    }

    [Then(@"(.+) should appear above (.+)")]
    public void ThenPersonaShouldAppearAbovePersona(string higher, string lower)
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        int higherIndex = -1;
        int lowerIndex  = -1;

        for (int i = 0; i < rows.Count; i++)
        {
            var cells    = rows[i].FindElements(By.TagName("td"));
            var username = cells[1].Text.Trim();

            if (username == $"{higher}_{_testRunId}") higherIndex = i;
            if (username == $"{lower}_{_testRunId}")  lowerIndex  = i;
        }

        higherIndex.Should().BeGreaterThanOrEqualTo(0, $"{higher} should be on the leaderboard");
        lowerIndex.Should().BeGreaterThanOrEqualTo(0, $"{lower} should be on the leaderboard");
        higherIndex.Should().BeLessThan(lowerIndex,
            $"{higher} (row {higherIndex}) should appear above {lower} (row {lowerIndex})");
    }

    [Then(@"I should see a maximum of 30 user entries")]
    public void ThenIShouldSeeAMaximumOf30UserEntries()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        rows.Count.Should().BeLessThanOrEqualTo(30,
            $"leaderboard should show at most 30 entries, but showed {rows.Count}");
    }

    [Then(@"the top 30 highest-scoring users should be shown")]
    public void ThenTheTop30HighestScoringUsersShouldBeShown()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        var displayedUsers = rows.Select(r =>
        {
            var cells = r.FindElements(By.TagName("td"));
            return new { Name = cells[1].Text.Trim(), Points = int.Parse(cells[2].Text.Trim()) };
        }).ToList();

        displayedUsers.Count.Should().Be(30, "should display exactly 30 users when more than 30 exist");

        for (int i = 0; i < displayedUsers.Count - 1; i++)
        {
            displayedUsers[i].Points.Should().BeGreaterThanOrEqualTo(displayedUsers[i + 1].Points,
                "displayed users should be in descending order by points");
        }
    }

    [Then(@"(.+)'s entry should be visually highlighted")]
    public void ThenPersonaEntryHighlighted(string name)
    {
        _personas.Should().ContainKey(name, $"{name} should exist as a test persona");
        var user = _personas[name];

        var userRow  = _driver.FindElement(By.Id($"user-{user.Id}"));
        var rowClass = userRow.GetAttribute("class");

        rowClass.Should().Contain("table-primary", $"{name}'s row should be highlighted with table-primary");
        rowClass.Should().Contain("fw-bold", $"{name}'s row should be bold");
    }

    [Then(@"(.+)'s point total of (\d+) should be visible")]
    public void ThenPersonaPointTotalVisible(string name, int expectedPoints)
    {
        _personas.Should().ContainKey(name, $"{name} should exist as a test persona");
        var user  = _personas[name];

        var userRow = _driver.FindElement(By.Id($"user-{user.Id}"));
        var cells   = userRow.FindElements(By.TagName("td"));

        cells.Count.Should().BeGreaterThanOrEqualTo(3, "the user row should have a points column");

        int.TryParse(cells[2].Text.Trim(), out int points).Should().BeTrue(
            "points should be displayed as a number");
        points.Should().Be(expectedPoints, $"{name}'s points should be {expectedPoints}");
    }

    [Then(@"(.+) should be able to locate their entry easily")]
    public void ThenPersonaCanLocateEntry(string name)
    {
        var jumpButton = _driver.FindElements(By.CssSelector("a.btn.btn-primary"))
            .FirstOrDefault(b => b.Text.Contains("Jump to My Rank"));

        jumpButton.Should().NotBeNull("the page should have a 'Jump to My Rank' button");
        jumpButton!.Displayed.Should().BeTrue("the 'Jump to My Rank' button should be visible");
        jumpButton.Text.Should().MatchRegex(@"#\d+", "the button should display the user's rank number");
    }

    [Then(@"(.+) and (.+) should be included in the list with zero points")]
    public void ThenPersonasShouldBeIncludedWithZeroPoints(string name1, string name2)
    {
        var rows       = _driver.FindElements(By.CssSelector("table tbody tr"));
        var foundUsers = new List<string>();

        foreach (var row in rows)
        {
            var cells    = row.FindElements(By.TagName("td"));
            var username = cells[1].Text.Trim();
            var points   = int.Parse(cells[2].Text.Trim());

            if (username == $"{name1}_{_testRunId}" || username == $"{name2}_{_testRunId}")
            {
                points.Should().Be(0, $"{username} should have zero points");
                foundUsers.Add(username);
            }
        }

        foundUsers.Count.Should().Be(2, $"both {name1} and {name2} should appear on the leaderboard");
    }

    [Then(@"they should appear after all users with positive points")]
    public void ThenTheyShouldAppearAfterAllUsersWithPositivePoints()
    {
        var rows   = _driver.FindElements(By.CssSelector("table tbody tr"));
        var points = ExtractPointsFromRows(rows);

        bool hasSeenZeroPoints = false;
        foreach (var p in points)
        {
            if (p == 0)
            {
                hasSeenZeroPoints = true;
            }
            else if (hasSeenZeroPoints)
            {
                Assert.Fail(
                    $"Found a user with {p} points after a zero-point user. " +
                    "All zero-point users should appear after positive-point users.");
            }
        }
    }

    #endregion

    #region Helper Methods

    private static List<int> ExtractPointsFromRows(IReadOnlyCollection<IWebElement> rows)
    {
        var points = new List<int>();
        foreach (var row in rows)
        {
            var cells = row.FindElements(By.TagName("td"));
            if (cells.Count >= 3 && int.TryParse(cells[2].Text.Trim(), out int pointValue))
                points.Add(pointValue);
        }
        return points;
    }

    private IServiceScope GetServiceScope()
    {
        var webAppPath  = _settings.WebAppContentRoot;
        var configFile  = Path.Combine(webAppPath, "appsettings.Acceptance.json");

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

        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(connectionString));
        services.AddDbContext<CacheDbContext>(o => o.UseSqlServer(connectionString));
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

    private ApplicationUser CreateTestUser(UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext, string username, string password, int points)
    {
        var user = new ApplicationUser
        {
            UserName       = username,
            Email          = $"{username}@test.com",
            EmailConfirmed = true,
            Points         = points,
            IsDeactivated  = false
        };

        var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();
        if (!result.Succeeded)
            throw new Exception(
                $"Failed to create test user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        _createdUserIds.Add(user.Id);
        return user;
    }

    private void LoginUser(string email, string password)
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");
        _wait.Until(d => d.FindElement(By.Id("emailField")));

        _driver.FindElement(By.Id("emailField")).SendKeys(email);
        _driver.FindElement(By.Id("passwordField")).SendKeys(password);

        var submitButton = _driver.FindElement(By.Id("submitBtn"));
        _wait.Until(d => submitButton.Enabled);
        submitButton.Click();

        _wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
