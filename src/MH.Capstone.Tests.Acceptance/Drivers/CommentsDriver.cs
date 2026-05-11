using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace MH.Capstone.Tests.Acceptance.Drivers;

// CSP-187: Selenium driver for the comments section on /Sighting/Details/{id}.
[ExcludeFromCodeCoverage]
public class CommentsDriver
{
    private readonly IWebDriver _webDriver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;

    public CommentsDriver(IWebDriver webDriver, AcceptanceTestSettings settings, WebDriverWait wait)
    {
        _webDriver = webDriver;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _wait = wait;
    }

    public void NavigateToSightingDetails(Guid sightingId)
    {
        _webDriver.Navigate().GoToUrl($"{_baseUrl}/Sighting/Details/{sightingId}");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    public void PostComment(string body)
    {
        var textarea = _wait.Until(d => d.FindElement(By.Id("commentBody")));
        textarea.Clear();
        textarea.SendKeys(body);
        _webDriver.FindElement(By.Id("postCommentButton")).Click();
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }

    public IReadOnlyList<string> GetVisibleCommentBodies()
        => _webDriver.FindElements(By.CssSelector(".comment-item .comment-body"))
            .Where(e => e.Displayed)
            .Select(e => (e.Text ?? string.Empty).Trim())
            .ToList();

    public int GetVisibleCommentCount()
        => _webDriver.FindElements(By.CssSelector(".comment-item")).Count(e => e.Displayed);

    public void HideFirstVisibleComment()
    {
        // Submit the parent form directly so floating page elements (Report / AI Companion
        // buttons fixed in the lower corners) cannot intercept the click.
        var formAction = ((IJavaScriptExecutor)_webDriver).ExecuteScript(
            "var f = document.querySelector('.comment-item form[action*=\\\"/Comments/\\\"][action$=\\\"/Hide\\\"]');" +
            "if (!f) return null;" +
            "f.submit();" +
            "return f.getAttribute('action');");
        if (formAction == null)
            throw new InvalidOperationException("No hide-comment form found on the page; admin role missing?");
        _wait.Until(d =>
        {
            try
            {
                var ready = ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.ToString();
                return string.Equals(ready, "complete", StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        });
    }
}
