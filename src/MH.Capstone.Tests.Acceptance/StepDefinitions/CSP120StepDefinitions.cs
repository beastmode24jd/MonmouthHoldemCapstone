using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using System.Text.Json;
using System.Linq;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "ai-companion")]
[ExcludeFromCodeCoverage]
public class CSP120StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly AcceptanceTestSettings _settings;
    private readonly AuthenticationDriver _authDriver;

    private const string DefaultPassword = "Test@1234";
    private static readonly Lazy<string?> _connectionString = new(LoadConnectionString);

    private readonly List<string> _createdUserIds = new();
    private readonly Dictionary<string, ApplicationUser> _personaUsers = new();

    public CSP120StepDefinitions(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, AuthenticationDriver authDriver)
    {
        _driver   = driver;
        _wait     = wait;
        _settings = settings;
        _authDriver = authDriver;
    }

    #region Given

    [Given("{word} is logged in and viewing any page on the site")]
    public void GivenPersonaIsLoggedInAndViewingAnyPageOnTheSite(string name)
    {
        if (_connectionString.Value is null)
            Assert.Ignore("Skipping: appsettings.Acceptance.json not found.");

        var user = EnsurePersona(name);
        _authDriver.PreformLoginForUser(user.Email!, DefaultPassword);

        _driver.Navigate().GoToUrl(_settings.BaseUrl);
        _wait.Until(d => d.FindElement(By.TagName("body")));
    }

    [Given("James is not logged in")]
    public void GivenJamesIsNotLoggedIn()
    {
        // Ensure any previously logged-in user is logged out so the unauthenticated
        // scenario starts from a known state.
        try
        {
            _authDriver.LogoutUser();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CSP120] Logout attempt failed or no user logged in: {ex.Message}");
        }

        // Navigate to the base URL to render the anonymous view
        _driver.Navigate().GoToUrl(_settings.BaseUrl);
        _wait.Until(d => d.FindElement(By.TagName("body")));
    }

    #endregion

    #region When

    [When("James visits a page on the site")]
    public void WhenJamesVisitsAPageOnTheSite()
    {
        _driver.Navigate().GoToUrl(_settings.BaseUrl);
        _wait.Until(d => d.FindElement(By.TagName("body")));
    }

    [When("{word} opens the AI Companion chat")]
    public void WhenPersonaOpensTheAICompanionChat(string name)
    {
        ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);");

        // Inject a fetch stub in the browser to intercept AICompanion/Ask and return a deterministic reply
        try
        {
            var patch = @"(function(){
                if(!window.fetch) { return 'no-fetch'; }
                if (window.__aiCompanionMockInjected) { return 'already-injected'; }
                var orig = window.fetch;
                window.__aiCompanionMockInjected = true;
                window.fetch = function(input, init){
                    var url = (typeof input === 'string') ? input : (input && input.url) || '';
                    if (url && url.indexOf('/AICompanion/Ask') !== -1){
                        var body = JSON.stringify({ reply: 'I can only help with wildlife...' });
                        var response = { ok: true, status: 200, json: function(){ return Promise.resolve(JSON.parse(body)); }, text: function(){ return Promise.resolve(body); } };
                        return Promise.resolve(response);
                    }
                    return orig.apply(this, arguments);
                };
                return 'injected';
            })();";

            var res = ((IJavaScriptExecutor)_driver).ExecuteScript(patch);
            Console.WriteLine($"AI companion fetch stub injection result: {res}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI companion fetch stub injection failed: {ex.GetType().Name}: {ex.Message}");
        }

        // Confirm the injection flag (if present) so tests fail fast if injection didn't take
        try
        {
            _wait.Until(d =>
            {
                var js = ((IJavaScriptExecutor)d).ExecuteScript("return !!window.__aiCompanionMockInjected;");
                return js is bool b && b;
            });
        }
        catch (WebDriverTimeoutException)
        {
            // Injection did not report back in time; continue and let later assertions surface failures
            Console.WriteLine("AI companion fetch stub did not confirm injection within wait period");
        }

        var button = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
            return (el.Displayed && el.Enabled) ? el : null;
        });

        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", button!);

        _wait.Until(d =>
        {
            var modal     = d.FindElement(By.Id("aiCompanionModal"));
            var classes   = modal.GetAttribute("class") ?? string.Empty;
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
        var input = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("aiCompanionQuestion"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        input.Clear();
        input.SendKeys(question);

        var submit = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("aiCompanionSubmitBtn"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", submit!);
    }

    #endregion

    #region Then

    [Then("{word} should see an {string} button")]
    public void ThenPersonaShouldSeeAButton(string name, string buttonLabel)
    {
        var button = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
            return (el.Displayed) ? el : null;
        });
        button.Displayed.Should().BeTrue(
            $"the '{buttonLabel}' button should be visible to authenticated users");
    }

    [Then("James should not see the {string} button")]
    public void ThenJamesShouldNotSeeTheButton(string buttonLabel)
    {
        // Use a short explicit wait for absence/hidden instead of toggling implicit waits
        var shortWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(1));
        shortWait.Until(d =>
        {
            var els = d.FindElements(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
            return els.Count == 0 || els.All(e => !e.Displayed);
        });

        var buttons = _driver.FindElements(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
        // Assert that there are no visible buttons
        buttons.All(b => !b.Displayed).Should().BeTrue("anonymous users should not see the AI Companion button");
    }

    [Then("{word} should see a reply from the AI Companion")]
    public void ThenPersonaShouldSeeAReplyFromTheAICompanion(string name)
    {
        var longerWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        var reply = longerWait.Until(d =>
        {
            var elements = d.FindElements(By.CssSelector("#aiCompanionMessages .ai-reply"));
            var last     = elements.LastOrDefault();
            if (last == null) return null;
            var text = last.Text?.Trim() ?? string.Empty;
            return text.Length > 0 && text != "Thinking..." ? last : null;
        });

        reply.Should().NotBeNull($"{name} should see a non-empty reply from the AI Companion");
    }

    [Then("{word} should see a reply redirecting the conversation back to wildlife topics")]
    public void ThenPersonaShouldSeeAReplyRedirectingBackToWildlife(string name)
    {
        var longerWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

        var reply = longerWait.Until(d =>
        {
            var elements = d.FindElements(By.CssSelector("#aiCompanionMessages .ai-reply"));
            var last     = elements.LastOrDefault();
            if (last == null) return null;
            var text = last.Text?.Trim() ?? string.Empty;
            return text.Length > 0 && text != "Thinking..." ? last : null;
        });

        reply.Should().NotBeNull($"{name} should see a non-empty reply");
        var replyText = reply!.Text;

        var staysOnTopic =
            replyText.Contains("wildlife",     StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("animal",       StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("nature",       StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("outdoor",      StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("conservation", StringComparison.OrdinalIgnoreCase);

        staysOnTopic.Should().BeTrue(
            $"off-topic replies should redirect to wildlife topics. Actual: '{replyText}'");
    }

    #endregion

    #region Cleanup

    [AfterScenario("ai-companion")]
    public void CleanupTestUsers()
    {
        if (!_createdUserIds.Any() || _connectionString.Value is null)
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

    #endregion

    #region Helpers

    private ApplicationUser EnsurePersona(string name)
    {
        if (_personaUsers.TryGetValue(name, out var existing))
            return existing;

        using var scope = GetServiceScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email  = $"Test{name}{suffix}@test.com";

        var user = new ApplicationUser
        {
            UserName       = email,
            Email          = email,
            EmailConfirmed = true,
            Points         = 0,
            IsDeactivated  = false
        };

        var result = userManager.CreateAsync(user, DefaultPassword).GetAwaiter().GetResult();
        if (!result.Succeeded)
            throw new Exception(
                $"Failed to create {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        _createdUserIds.Add(user.Id);
        _personaUsers[name] = user;
        return user;
    }

    private static IServiceScope GetServiceScope()
    {
        var connectionString = _connectionString.Value!;

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

    private static string? LoadConnectionString()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            // Look for appsettings.Acceptance.json — the config used by the in-process
            // acceptance test host (not appsettings.Development.json).
            var candidate = Path.Combine(dir.FullName, "MH.Capstone.WebApp", "appsettings.Acceptance.json");
            if (File.Exists(candidate))
            {
                using var stream = File.OpenRead(candidate);
                using var doc    = JsonDocument.Parse(stream);

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
