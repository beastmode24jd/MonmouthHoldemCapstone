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
    private ApplicationUser? _currentTestUser;

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
        // Clean up test users from the database
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

    [Given(@"there are multiple users with different point totals")]
    public void GivenThereAreMultipleUsersWithDifferentPointTotals()
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        CreateTestUser(userManager, dbContext, "TestHighScorer", "Test@1234", 500);
        CreateTestUser(userManager, dbContext, "TestMidScorer", "Test@1234", 250);
        CreateTestUser(userManager, dbContext, "TestLowScorer", "Test@1234", 100);
        CreateTestUser(userManager, dbContext, "TestBeginner", "Test@1234", 50);
    }

    [Given(@"there are more than 30 users in the system")]
    public void GivenThereAreMoreThan30UsersInTheSystem()
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create 35 users to exceed the 30-entry limit
        for (int i = 1; i <= 35; i++)
        {
            CreateTestUser(userManager, dbContext, $"TestUser{i:D3}", "Test@1234", 1000 - (i * 10));
        }
    }

    [Given(@"I am logged in as a user with points")]
    public void GivenIAmLoggedInAsAUserWithPoints()
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Create the current user with 300 points
        _currentTestUser = CreateTestUser(userManager, dbContext, "TestCurrentUser", "Test@1234", 300);

        // Create other users for context
        CreateTestUser(userManager, dbContext, "TestTopUser", "Test@1234", 500);
        CreateTestUser(userManager, dbContext, "TestOtherUser", "Test@1234", 200);

        // Log in as the current test user via the browser
        LoginUser("TestCurrentUser", "Test@1234");
    }

    [Given(@"there are users with zero points in the system")]
    public void GivenThereAreUsersWithZeroPointsInTheSystem()
    {
        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        CreateTestUser(userManager, dbContext, "TestVeteran", "Test@1234", 150);
        CreateTestUser(userManager, dbContext, "TestNewbie1", "Test@1234", 0);
        CreateTestUser(userManager, dbContext, "TestNewbie2", "Test@1234", 0);
        CreateTestUser(userManager, dbContext, "TestActive", "Test@1234", 75);
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

        // Wait for leaderboard table to load
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

        // Verify descending order
        for (int i = 0; i < points.Count - 1; i++)
        {
            Assert.That(points[i], Is.GreaterThanOrEqualTo(points[i + 1]),
                $"Points at position {i} ({points[i]}) should be >= points at position {i + 1} ({points[i + 1]})");
        }
    }

    [Then(@"the user with the most points should appear first")]
    public void ThenTheUserWithTheMostPointsShouldAppearFirst()
    {
        var firstRow = _driver.FindElement(By.CssSelector("table tbody tr:first-child"));
        var cells = firstRow.FindElements(By.TagName("td"));

        Assert.That(cells.Count, Is.GreaterThanOrEqualTo(3),
            "First row should have rank, name, and points columns");

        // Verify rank column shows 1
        var rank = cells[0].Text.Trim();
        Assert.That(rank, Is.EqualTo("1"), "First entry should have rank #1");

        // Verify no other visible user has more points
        var firstUserPoints = int.Parse(cells[2].Text.Trim());
        var allRows = _driver.FindElements(By.CssSelector("table tbody tr"));

        foreach (var row in allRows.Skip(1))
        {
            var rowCells = row.FindElements(By.TagName("td"));
            var rowPoints = int.Parse(rowCells[2].Text.Trim());
            Assert.That(firstUserPoints, Is.GreaterThanOrEqualTo(rowPoints),
                "First user should have the highest points");
        }
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

        // Verify they're sorted correctly (descending)
        for (int i = 0; i < displayedUsers.Count - 1; i++)
        {
            Assert.That(displayedUsers[i].Points, Is.GreaterThanOrEqualTo(displayedUsers[i + 1].Points),
                "Displayed users should be in descending order by points");
        }
    }

    [Then(@"my user entry should be visually highlighted")]
    public void ThenMyUserEntryShouldBeVisuallyHighlighted()
    {
        Assert.That(_currentTestUser, Is.Not.Null, "Current test user should exist");

        // The view renders each row with id="user-{Id}"
        var currentUserRow = _driver.FindElement(By.Id($"user-{_currentTestUser!.Id}"));

        Assert.That(currentUserRow, Is.Not.Null, "Current user's row should exist in leaderboard");

        // The view applies "table-primary fw-bold" when entry.Id == Model.CurrentUserId
        var rowClass = currentUserRow.GetAttribute("class");
        Assert.That(rowClass, Does.Contain("table-primary"),
            "Current user's row should have 'table-primary' class for highlighting");
        Assert.That(rowClass, Does.Contain("fw-bold"),
            "Current user's row should have 'fw-bold' class for emphasis");
    }

    [Then(@"my current point total should be visible")]
    public void ThenMyCurrentPointTotalShouldBeVisible()
    {
        Assert.That(_currentTestUser, Is.Not.Null, "Current test user should exist");

        var currentUserRow = _driver.FindElement(By.Id($"user-{_currentTestUser!.Id}"));
        var cells = currentUserRow.FindElements(By.TagName("td"));

        Assert.That(cells.Count, Is.GreaterThanOrEqualTo(3), "User row should have points column");

        var pointsText = cells[2].Text.Trim();
        Assert.That(int.TryParse(pointsText, out int points), Is.True,
            "Points should be displayed as a number");
        Assert.That(points, Is.EqualTo(300),
            "Current user's points should match the expected value of 300");
    }

    [Then(@"I should be able to locate my entry easily")]
    public void ThenIShouldBeAbleToLocateMyEntryEasily()
    {
        // The view renders a "Jump to My Rank (#N)" button when Model.UserRank > 0
        var jumpButton = _driver.FindElements(By.CssSelector("a.btn.btn-primary"))
            .FirstOrDefault(b => b.Text.Contains("Jump to My Rank"));

        Assert.That(jumpButton, Is.Not.Null,
            "Page should have a 'Jump to My Rank' button for easy navigation");
        Assert.That(jumpButton!.Displayed, Is.True,
            "Jump to My Rank button should be visible");

        // The button text is "Jump to My Rank (#N)" — verify it includes a rank number
        Assert.That(jumpButton.Text, Does.Match(@"#\d+"),
            "Button should display the user's rank number");
    }

    [Then(@"users with zero points should be included in the list")]
    public void ThenUsersWithZeroPointsShouldBeIncludedInTheList()
    {
        var rows = _driver.FindElements(By.CssSelector("table tbody tr"));
        var points = ExtractPointsFromRows(rows);

        var zeroPointCount = points.Count(p => p == 0);

        Assert.That(zeroPointCount, Is.GreaterThan(0),
            "Leaderboard should include users with zero points");
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

        // Wait for login form — the view uses asp-for="Email" which generates id="Email"
        _wait.Until(d => d.FindElement(By.Id("Email")));

        var emailInput = _driver.FindElement(By.Id("Email"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var submitButton = _driver.FindElement(By.Id("submitBtn"));

        emailInput.SendKeys($"{username}@test.com");
        passwordInput.SendKeys(password);

        // The submit button is disabled until JS validation runs; wait for it to enable
        _wait.Until(d => submitButton.Enabled);
        submitButton.Click();

        // Wait for redirect after login
        _wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
