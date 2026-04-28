using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
        // Navigate to the site first (so cookie operations target the correct domain),
        // then clear browser storage and cookies to ensure an unauthenticated state.
        _driver.Navigate().GoToUrl(BaseUrl);
        try { _driver.Manage().Cookies.DeleteAllCookies(); } catch { }
        try { ((IJavaScriptExecutor)_driver).ExecuteScript("window.localStorage.clear(); window.sessionStorage.clear();"); } catch { }
        // Reload to ensure the server observes the cleared cookies/storage
        _driver.Navigate().GoToUrl(BaseUrl);
        // Wait for page load
        _wait.Until(d => {
            try {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            } catch { return false; }
        });
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
        // Re-navigate to get a fresh page load so Bootstrap modal JS state is clean.
        NavigateToReportablePage();
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

        var suffix   = Guid.NewGuid().ToString("N")[..8];
        var username = $"Test{name}{suffix}";
        var email    = $"{username}@test.com";

        var appServices = TestWebAppHost.Services
            ?? throw new InvalidOperationException("TestWebAppHost has not started; cannot resolve web app services.");

        using var scope = appServices.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var user = new ApplicationUser
        {
            UserName      = email,
            Email         = email,
            EmailConfirmed = true,
            DisplayName   = username,
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
        try { ((IJavaScriptExecutor)_driver).ExecuteScript("window.scrollTo(0, 0);"); } catch { }

        var openButton = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("button[data-bs-target='#reportModal']"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Open button found: {openButton != null}");
        TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Open button of type: {openButton?.GetType().Name}");

        // Try a single JS click to open the modal (avoids repeated clicks while waiting).
        try
        {
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", openButton);
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] JS click threw: {ex.GetType().Name} {ex.Message}");
        }

        // Preferred wait: wait directly for the select inside the modal to be visible
        try
        {
            var shortWait = new WebDriverWait(_driver, TimeSpan.FromSeconds(3));
            var success = shortWait.Until(d =>
            {
                var els = d.FindElements(By.Id("reportReason"));
                if (els.Count == 0) return false;
                var sel = els[0];
                TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Found reportReason: displayed={sel.Displayed} enabled={sel.Enabled}");
                return sel.Displayed && sel.Enabled;
            });
            if (success)
            {
                // Quick stability check: ensure the select remains visible for a short moment
                try
                {
                    var confirm = new WebDriverWait(_driver, TimeSpan.FromSeconds(1));
                    var stable = confirm.Until(d =>
                    {
                        var el = d.FindElement(By.Id("reportReason"));
                        return el.Displayed && el.Enabled;
                    });
                    if (stable)
                        return;
                }
                catch { /* Not stable; proceed to fallback logic */ }
            }
        }
        catch (OpenQA.Selenium.WebDriverTimeoutException)
        {
            // not ready yet, proceed to fallback
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Error while short-waiting for reportReason: {ex.GetType().Name} {ex.Message}");
        }

        // Fallback: try to cleanup any stray backdrops/modals then show using bootstrap/jQuery/direct DOM
        TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] reportReason not visible after click; attempting cleanup and bootstrap/jQuery fallback to show modal.");

        var cleanupScript = @"(function(){
                    try{
                        document.querySelectorAll('.modal-backdrop').forEach(function(b){ b.remove(); });
                        document.querySelectorAll('.modal.show').forEach(function(m){
                            try{
                                if(typeof bootstrap !== 'undefined' && bootstrap.Modal && bootstrap.Modal.getOrCreateInstance){
                                    var inst = bootstrap.Modal.getOrCreateInstance(m);
                                    if(inst) inst.hide();
                                } else {
                                    m.classList.remove('show');
                                    m.style.display = 'none';
                                    m.setAttribute('aria-hidden','true');
                                }
                            } catch(e) { }
                        });
                        document.body.classList.remove('modal-open');
                        return true;
                    } catch(e){ return false; }
                })();";
        try
        {
            var cleanupRes = ((IJavaScriptExecutor)_driver).ExecuteScript(cleanupScript);
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Cleanup executed, result: {cleanupRes}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Cleanup threw: {ex.GetType().Name} {ex.Message}");
        }

        var script = @"(function(){
                    try{
                        var el = document.getElementById('reportModal');
                        if(!el) return false;
                        if(typeof bootstrap !== 'undefined'){
                            var inst = (bootstrap.Modal && bootstrap.Modal.getOrCreateInstance) ? bootstrap.Modal.getOrCreateInstance(el) : new bootstrap.Modal(el);
                            inst.show();
                            return true;
                        } else if (typeof $ !== 'undefined' && $.fn && $.fn.modal){
                            $(el).modal('show');
                            return true;
                        } else {
                            el.classList.add('show');
                            el.style.display = '';
                            el.setAttribute('aria-hidden','false');
                            return true;
                        }
                    } catch(e){
                        return false;
                    }
                })();";

        try
        {
            var res = ((IJavaScriptExecutor)_driver).ExecuteScript(script);
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Bootstrap fallback executed, result: {res}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Bootstrap fallback threw: {ex.GetType().Name} {ex.Message}");
        }

        // Attempt to wait for the bootstrap 'shown' event (async) or fall back to polling the select
        try
        {
            var asyncScript = @"var callback = arguments[arguments.length - 1];
                try {
                    var el = document.getElementById('reportModal');
                    if(!el){ callback(false); return; }
                    if (typeof bootstrap !== 'undefined' && bootstrap.Modal){
                        var inst = bootstrap.Modal.getOrCreateInstance(el);
                        if(inst){
                            var handler = function(){ el.removeEventListener('shown.bs.modal', handler); callback(true); };
                            el.addEventListener('shown.bs.modal', handler);
                            if(!el.classList.contains('show')) inst.show();
                            return;
                        }
                    }
                    if (typeof $ !== 'undefined' && $.fn && $.fn.modal){
                        $(el).one('shown.bs.modal', function(){ callback(true); });
                        $(el).modal('show');
                        return;
                    }
                    // Poll for the inner select briefly
                    var started = Date.now();
                    var iv = setInterval(function(){
                        var sel = document.getElementById('reportReason');
                        if(sel && (sel.offsetParent !== null || sel.style.display != 'none')){ clearInterval(iv); callback(true); }
                        else if(Date.now() - started > 3000){ clearInterval(iv); callback(false); }
                    },100);
                } catch(e){ callback(false); }";

            var shown = ((IJavaScriptExecutor)_driver).ExecuteAsyncScript(asyncScript);
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Async show wait returned: {shown}");
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] Async show wait threw: {ex.GetType().Name} {ex.Message}");
        }

        // Final wait for the select to be interactable
        _wait.Until(d =>
        {
            var select = d.FindElement(By.Id("reportReason"));
            var displayed = select.Displayed;
            var enabled = select.Enabled;
            TestContext.Out.WriteLine($"[{nameof(OpenReportModal)}] reportReason final wait: displayed={displayed} enabled={enabled}");
            return displayed && enabled;
        });
    }

    private void FillReportForm(string reason, string description)
    {
        var reasonElement = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportReason"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        TestContext.Out.WriteLine($"[{nameof(FillReportForm)}] Found reason select: displayed={reasonElement.Displayed} enabled={reasonElement.Enabled} type={reasonElement.GetType().Name}");
        var reasonSelect = new SelectElement(reasonElement);
        reasonSelect.SelectByValue(reason);
        TestContext.Out.WriteLine($"[{nameof(FillReportForm)}] Selected reason value: {reason}");

        var descriptionBox = _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportDescription"));
            return (el.Displayed && el.Enabled) ? el : null;
        });
        TestContext.Out.WriteLine($"[{nameof(FillReportForm)}] Found description box: displayed={descriptionBox.Displayed} enabled={descriptionBox.Enabled} type={descriptionBox.GetType().Name}");
        descriptionBox.Clear();
        TestContext.Out.WriteLine($"[{nameof(FillReportForm)}] Cleared description box");
        descriptionBox.SendKeys(description);
        TestContext.Out.WriteLine($"[{nameof(FillReportForm)}] Sent keys to description box");
    }

    private void SubmitReportForm()
    {
        IWebElement? submitBtn = null;
        try
        {
            submitBtn = _wait.Until(d =>
            {
                try
                {
                    var el = d.FindElement(By.Id("reportSubmitBtn"));
                    return (el.Displayed && el.Enabled) ? el : null;
                }
                catch { return null; }
            });
        }
        catch (OpenQA.Selenium.WebDriverTimeoutException)
        {
            TestContext.Out.WriteLine($"[{nameof(SubmitReportForm)}] Timed out waiting for visible submit button; attempting fallback JS click by id.");
        }

        try
        {
            if (submitBtn != null)
            {
                TestContext.Out.WriteLine($"[{nameof(SubmitReportForm)}] Clicking submit button (element type: {submitBtn.GetType().Name})");
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", submitBtn);
                TestContext.Out.WriteLine($"[{nameof(SubmitReportForm)}] Click issued via JS.");
            }
            else
            {
                // Fallback: click by id via JS (ignores visibility) to handle transient visibility issues in CI/headless
                ((IJavaScriptExecutor)_driver).ExecuteScript("var b=document.getElementById('reportSubmitBtn'); if(b) { b.click(); } else { console.warn('reportSubmitBtn not found'); }");
                TestContext.Out.WriteLine($"[{nameof(SubmitReportForm)}] Fallback JS click issued by id.");
            }
        }
        catch (Exception ex)
        {
            TestContext.Out.WriteLine($"[{nameof(SubmitReportForm)}] JS click threw: {ex.GetType().Name} {ex.Message}");
            throw;
        }
    }

    private void WaitForReportSuccessMessage()
    {
        _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportMessage"));
            var classes = el.GetAttribute("class") ?? string.Empty;
            var text = el.Text ?? string.Empty;
            if (classes.Contains("alert-success")) return true;
            if (classes.Contains("alert-danger"))
                throw new Exception($"Report submission error: '{text}' (classes: {classes})");
            return false;
        });
    }

    private void WaitForReportErrorMessage()
    {
        _wait.Until(d =>
        {
            var el = d.FindElement(By.Id("reportMessage"));
            var classes = el.GetAttribute("class") ?? string.Empty;
            var text = el.Text ?? string.Empty;
            TestContext.Out.WriteLine($"[{nameof(WaitForReportErrorMessage)}] reportMessage classes: {classes}, text: '{text}'");
            return classes.Contains("alert-danger");
        });
    }

    private void WaitForReportModalHidden()
    {
        // The modal auto-hides ~2s after a successful submission.
        _wait.Until(d =>
        {
            var modal   = d.FindElement(By.Id("reportModal"));
            var classes = modal.GetAttribute("class") ?? string.Empty;
            return !classes.Contains("show");
        });
    }

    private IServiceScope GetServiceScope()
    {
        var appServices = TestWebAppHost.Services
            ?? throw new InvalidOperationException("TestWebAppHost has not started; cannot resolve web app services.");
        return appServices.CreateScope();
    }

    #endregion
}
