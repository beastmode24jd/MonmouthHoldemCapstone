using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class LeaderboardSteps
{
    private IWebDriver _driver = null!;
    private WebDriverWait _wait = null!;
    private const string BaseUrl = "https://localhost:7147";

    // Test data tracking for cleanup
    private readonly List<string> _createdUserIds = new();
    private readonly string _testRunId = Guid.NewGuid().ToString("N")[..8];

    // Named persona tracking
    private readonly Dictionary<string, ApplicationUser> _personas = new();
    private ApplicationUser? _loggedInUser;

    #region Setup and Teardown

    [BeforeScenario]
    public void SetupBrowser()
    {
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
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = CreateTestUser(userManager, dbContext, $"{name}_{_testRunId}", "Test@1234", points);
        _personas[name] = user;
    }

    [Given(@"(.+) is logged in with (\d+) points")]
    public void GivenPersonaIsLoggedInWithPoints(string name, int points)
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var user = CreateTestUser(userManager, dbContext, $"{name}_{_testRunId}", "Test@1234", points);
        _personas[name] = user;
        _loggedInUser = user;

        LoginUser($"{name}_{_testRunId}", "Test@1234");
    }

    [Given(@"there are more than 30 users in the system")]
    public void GivenThereAreMoreThan30UsersInTheSystem()
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        for (int i = 1; i <= 35; i++)
        {
            CreateTestUser(userManager, dbContext, $"User{i:D3}_{_testRunId}", "Test@1234", 1000 - (i * 10));
        }
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

        Assert.That(matchingLink, Is.Not.Null,
            $"Navigation bar should contain a '{linkText}' link");
        Assert.That(matchingLink!.Displayed, Is.True,
            $"'{linkText}' link should be visible");
    }

    [Then(@"users should be displayed in descending order by points")]
    public void ThenUsersShouldBeDisplayedInDescendingOrderByPoints()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        Assert.That(rows.Count, Is.GreaterThan(0), "Leaderboard should have user entries");

        var points = ExtractPointsFromRows(rows);

        for (int i = 0; i < points.Count - 1; i++)
        {
            Assert.That(points[i], Is.GreaterThanOrEqualTo(points[i + 1]),
                $"Points at position {i} ({points[i]}) should be >= points at position {i + 1} ({points[i + 1]})");
        }
    }

    [Then(@"(.+) should appear above (.+)")]
    public void ThenPersonaShouldAppearAbovePersona(string higher, string lower)
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        int higherIndex = -1;
        int lowerIndex = -1;

        for (int i = 0; i < rows.Count; i++)
        {
            var cells = rows[i].FindElements(By.TagName("td"));
            var username = cells[1].Text.Trim();

            if (username == $"{higher}_{_testRunId}") higherIndex = i;
            if (username == $"{lower}_{_testRunId}") lowerIndex = i;
        }

        Assert.That(higherIndex, Is.GreaterThanOrEqualTo(0), $"{higher} should be on the leaderboard");
        Assert.That(lowerIndex, Is.GreaterThanOrEqualTo(0), $"{lower} should be on the leaderboard");
        Assert.That(higherIndex, Is.LessThan(lowerIndex),
            $"{higher} (row {higherIndex}) should appear above {lower} (row {lowerIndex})");
    }

    [Then(@"I should see a maximum of 30 user entries")]
    public void ThenIShouldSeeAMaximumOf30UserEntries()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        Assert.That(rows.Count, Is.LessThanOrEqualTo(30),
            $"Leaderboard should show maximum 30 entries, but showed {rows.Count}");
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

        Assert.That(displayedUsers.Count, Is.EqualTo(30),
            "Should display exactly 30 users when there are more than 30 in the system");

        for (int i = 0; i < displayedUsers.Count - 1; i++)
        {
            Assert.That(displayedUsers[i].Points, Is.GreaterThanOrEqualTo(displayedUsers[i + 1].Points),
                "Displayed users should be in descending order by points");
        }
    }

    [Then(@"(.+)'s entry should be visually highlighted")]
    public void ThenPersonaEntryHighlighted(string name)
    {
        Assert.That(_personas.ContainsKey(name), Is.True, $"{name} should exist as a test persona");
        var user = _personas[name];

        var userRow = _driver.FindElement(By.Id($"user-{user.Id}"));
        Assert.That(userRow, Is.Not.Null, $"{name}'s row should exist in leaderboard");

        var rowClass = userRow.GetAttribute("class");
        Assert.That(rowClass, Does.Contain("table-primary"),
            $"{name}'s row should have 'table-primary' class for highlighting");
        Assert.That(rowClass, Does.Contain("fw-bold"),
            $"{name}'s row should have 'fw-bold' class for emphasis");
    }

    [Then(@"(.+)'s point total of (\d+) should be visible")]
    public void ThenPersonaPointTotalVisible(string name, int expectedPoints)
    {
        Assert.That(_personas.ContainsKey(name), Is.True, $"{name} should exist as a test persona");
        var user = _personas[name];

        var userRow = _driver.FindElement(By.Id($"user-{user.Id}"));
        var cells = userRow.FindElements(By.TagName("td"));

        Assert.That(cells.Count, Is.GreaterThanOrEqualTo(3), "User row should have points column");

        var pointsText = cells[2].Text.Trim();
        Assert.That(int.TryParse(pointsText, out int points), Is.True,
            "Points should be displayed as a number");
        Assert.That(points, Is.EqualTo(expectedPoints),
            $"{name}'s points should be {expectedPoints}");
    }

    [Then(@"(.+) should be able to locate their entry easily")]
    public void ThenPersonaCanLocateEntry(string name)
    {
        var jumpButton = _driver.FindElements(By.CssSelector("a.btn.btn-primary"))
            .FirstOrDefault(b => b.Text.Contains("Jump to My Rank"));

        Assert.That(jumpButton, Is.Not.Null,
            "Page should have a 'Jump to My Rank' button for easy navigation");
        Assert.That(jumpButton!.Displayed, Is.True,
            "Jump to My Rank button should be visible");
        Assert.That(jumpButton.Text, Does.Match(@"#\d+"),
            "Button should display the user's rank number");
    }

    [Then(@"(.+) and (.+) should be included in the list with zero points")]
    public void ThenPersonasShouldBeIncludedWithZeroPoints(string name1, string name2)
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        var foundUsers = new List<string>();

        foreach (var row in rows)
        {
            var cells = row.FindElements(By.TagName("td"));
            var username = cells[1].Text.Trim();
            var points = int.Parse(cells[2].Text.Trim());

            if (username == $"{name1}_{_testRunId}" || username == $"{name2}_{_testRunId}")
            {
                Assert.That(points, Is.EqualTo(0), $"{username} should have zero points");
                foundUsers.Add(username);
            }
        }

        Assert.That(foundUsers.Count, Is.EqualTo(2),
            $"Both {name1} and {name2} should appear on the leaderboard");
    }

    [Then(@"they should appear after all users with positive points")]
    public void ThenTheyShouldAppearAfterAllUsersWithPositivePoints()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
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
                Assert.Fail($"Found user with {p} points after a zero-point user. " +
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
            {
                points.Add(pointValue);
            }
        }

        return points;
    }

    private IServiceScope GetServiceScope()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "MH.Capstone.WebApp"))
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var connectionString = configuration.GetConnectionString("DataDb")
                               ?? throw new InvalidOperationException(
                                   "Connection string 'DataDb' not found in appsettings.Development.json.");

        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddDbContext<CacheDbContext>(options =>
            options.UseSqlServer(connectionString));

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
            UserName = username,
            Email = $"{username}@test.com",
            EmailConfirmed = true,
            Points = points,
            IsDeactivated = false
        };

        var result = userManager.CreateAsync(user, password).GetAwaiter().GetResult();

        if (!result.Succeeded)
        {
            throw new Exception(
                $"Failed to create test user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _createdUserIds.Add(user.Id);
        return user;
    }

    private void LoginUser(string username, string password)
    {
        _driver.Navigate().GoToUrl($"{BaseUrl}/Account/Login");

        _wait.Until(d => d.FindElement(By.Id("Email")));

        var emailInput = _driver.FindElement(By.Id("Email"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var submitButton = _driver.FindElement(By.Id("submitBtn"));

        emailInput.SendKeys($"{username}@test.com");
        passwordInput.SendKeys(password);

        _wait.Until(d => submitButton.Enabled);
        submitButton.Click();

        _wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
