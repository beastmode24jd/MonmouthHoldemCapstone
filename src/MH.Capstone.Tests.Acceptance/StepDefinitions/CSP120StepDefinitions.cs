using System.IO;
using System.Text.Json;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Tests.Acceptance.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "ai-companion")]
public class CSP120StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    private const string DefaultPassword = "Test@1234";
    private static readonly Lazy<string?> _connectionString = new(LoadConnectionString);

    private readonly List<string> _createdUserIds = new();
    private readonly Dictionary<string, ApplicationUser> _personaUsers = new();

    public CSP120StepDefinitions(IWebDriver driver, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;
    }

    #region Given

    [Given("{word} is logged in and viewing any page on the site")]
    public void GivenPersonaIsLoggedInAndViewingAnyPageOnTheSite(string name)
    {
        if (_connectionString.Value is null)
        {
            Assert.Ignore("Skipping: appsettings.Development.json not found.");
        }

        var user = EnsurePersona(name);
        LoginUser(user.Email!, DefaultPassword);

        _driver.Navigate().GoToUrl(Hooks.BaseUrl);
        _wait.Until(d => d.FindElement(By.TagName("body")));
    }

    [Given("James is not logged in")]
    public void GivenJamesIsNotLoggedIn()
    {
        // No user created, no login.
    }

    #endregion

    #region When

    [When("James visits a page on the site")]
    public void WhenJamesVisitsAPageOnTheSite()
    {
        _driver.Navigate().GoToUrl(Hooks.BaseUrl);
        _wait.Until(d => d.FindElement(By.TagName("body")));
    }

    [When("{word} opens the AI Companion chat")]
    public void WhenPersonaOpensTheAICompanionChat(string name)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);");

        var button = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
            return (el.Displayed && el.Enabled) ? el : null;
        });

        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", button!);

        _wait.Until(d =>
        {
            var modal = d.FindElement(By.Id("aiCompanionModal"));
            var classes = modal.GetAttribute("class") ?? string.Empty;
            var ariaHidden = modal.GetAttribute("aria-hidden");
            return classes.Contains("show") && ariaHidden != "true";
        });

        _wait.Until(d =>
        {
            var input = d.FindElement(By.Id("aiCompanionQuestion"));
            return input.Displayed && input.Enabled;
        });
    }

    [When("{word} asks {string}")]
    public void WhenPersonaAsks(string name, string question)
    {
        var input = _driver.FindElement(By.Id("aiCompanionQuestion"));
        input.Clear();
        input.SendKeys(question);

        var submit = _driver.FindElement(By.Id("aiCompanionSubmitBtn"));
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", submit);
    }

    #endregion

    #region Then

    [Then("{word} should see an {string} button")]
    public void ThenPersonaShouldSeeAButton(string name, string buttonLabel)
    {
        var button = _driver.FindElement(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
        Assert.That(button.Displayed, Is.True,
            $"The '{buttonLabel}' button should be visible to authenticated users");
    }

    [Then("James should not see the {string} button")]
    public void ThenJamesShouldNotSeeTheButton(string buttonLabel)
    {
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(1);
        var buttons = _driver.FindElements(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        Assert.That(buttons, Is.Empty,
            "Anonymous users should not see the AI Companion button");
    }

    [Then("{word} should see a reply from the AI Companion")]
    public void ThenPersonaShouldSeeAReplyFromTheAICompanion(string name)
    {
        // Gemini call may take a few seconds; use a longer wait.
        var longerWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        var reply = longerWait.Until(d =>
        {
            var elements = d.FindElements(By.CssSelector("#aiCompanionMessages .ai-reply"));
            var last = elements.LastOrDefault();
            if (last == null) return null;
            var text = last.Text?.Trim() ?? string.Empty;
            return text.Length > 0 && text != "Thinking..." ? last : null;
        });

        Assert.That(reply, Is.Not.Null, $"{name} should see a non-empty reply from the AI Companion");
    }

    [Then("{word} should see a reply redirecting the conversation back to wildlife topics")]
    public void ThenPersonaShouldSeeAReplyRedirectingBackToWildlife(string name)
    {
        var longerWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        var reply = longerWait.Until(d =>
        {
            var elements = d.FindElements(By.CssSelector("#aiCompanionMessages .ai-reply"));
            var last = elements.LastOrDefault();
            if (last == null) return null;
            var text = last.Text?.Trim() ?? string.Empty;
            return text.Length > 0 && text != "Thinking..." ? last : null;
        });

        Assert.That(reply, Is.Not.Null);
        var replyText = reply!.Text;

        var staysOnTopic =
            replyText.Contains("wildlife", StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("animal", StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("nature", StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("outdoor", StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("conservation", StringComparison.OrdinalIgnoreCase);

        Assert.That(staysOnTopic, Is.True,
            $"Off-topic replies should redirect to wildlife. Actual: '{replyText}'");
    }

    #endregion

    #region Cleanup

    [AfterScenario("@ai-companion")]
    public void CleanupTestUsers()
    {
        if (_createdUserIds.Any() && _connectionString.Value is not null)
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
    }

    #endregion

    #region Helpers

    private ApplicationUser EnsurePersona(string name)
    {
        if (_personaUsers.TryGetValue(name, out var existing))
            return existing;

        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"Test{name}{suffix}@test.com";

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
            throw new Exception($"Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        _createdUserIds.Add(user.Id);
        _personaUsers[name] = user;
        return user;
    }

    private void LoginUser(string email, string password)
    {
        _driver.Navigate().GoToUrl($"{Hooks.BaseUrl}/Account/Login");
        _wait.Until(d => d.FindElement(By.Id("Email")));

        _driver.FindElement(By.Id("Email")).SendKeys(email);
        _driver.FindElement(By.Id("passwordField")).SendKeys(password);

        var submit = _driver.FindElement(By.Id("submitBtn"));
        _wait.Until(d => submit.Enabled);
        submit.Click();

        _wait.Until(d => !d.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
    }

    private static IServiceScope GetServiceScope()
    {
        var connectionString = _connectionString.Value!;

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

    private static string? LoadConnectionString()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "MH.Capstone.WebApp", "appsettings.Development.json");
            if (File.Exists(candidate))
            {
                using var stream = File.OpenRead(candidate);
                using var doc = JsonDocument.Parse(stream);

                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("DataDb", out var dataDb) &&
                    dataDb.ValueKind == JsonValueKind.String)
                {
                    return dataDb.GetString();
                }
            }

            dir = dir.Parent;
        }

        return null;
    }

    #endregion
}
