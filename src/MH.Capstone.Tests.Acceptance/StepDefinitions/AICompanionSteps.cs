using MH.Capstone.Tests.Acceptance.Support;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

/// <summary>
/// Step definitions for the CSP-120 AI Companion feature.
/// Receives the Selenium driver via Reqnroll DI from <see cref="Hooks"/>.
///
/// RED phase: the chat UI does not exist yet, so every assertion that
/// references the button/modal/reply will fail on "element not found".
/// GREEN phase will wire up the layout UI and these step defs will flip green.
/// </summary>
[Binding]
public class AICompanionSteps
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public AICompanionSteps(IWebDriver driver, WebDriverWait wait)
    {
        _driver = driver;
        _wait = wait;
    }

    #region Given

    [Given("{word} is logged in and viewing any page on the site")]
    public void GivenPersonaIsLoggedInAndViewingAnyPageOnTheSite(string name)
    {
        // RED phase stub: intentionally does not perform a real login yet.
        // GREEN phase will reuse the login helper pattern from ManualUserReportSteps.
        _driver.Navigate().GoToUrl(Hooks.BaseUrl);
    }

    [Given("James is not logged in")]
    public void GivenJamesIsNotLoggedIn()
    {
        // No login performed.
    }

    #endregion

    #region When

    [When("James visits a page on the site")]
    public void WhenJamesVisitsAPageOnTheSite()
    {
        _driver.Navigate().GoToUrl(Hooks.BaseUrl);
    }

    [When("{word} opens the AI Companion chat")]
    public void WhenPersonaOpensTheAICompanionChat(string name)
    {
        // Expected selector for GREEN: a floating button like the report one.
        var button = _driver.FindElement(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
        button.Click();
    }

    [When("{word} asks {string}")]
    public void WhenPersonaAsks(string name, string question)
    {
        var input = _driver.FindElement(By.Id("aiCompanionQuestion"));
        input.Clear();
        input.SendKeys(question);

        var submit = _driver.FindElement(By.Id("aiCompanionSubmitBtn"));
        submit.Click();
    }

    #endregion

    #region Then

    [Then("{word} should see an {string} button")]
    public void ThenPersonaShouldSeeAButton(string name, string buttonLabel)
    {
        Assert.That(buttonLabel, Is.EqualTo("Ask the AI Companion"));

        var button = _driver.FindElement(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
        Assert.That(button.Displayed, Is.True,
            "The AI Companion button should be visible to authenticated users on every page");
    }

    [Then("James should not see the {string} button")]
    public void ThenJamesShouldNotSeeTheButton(string buttonLabel)
    {
        Assert.That(buttonLabel, Is.EqualTo("Ask the AI Companion"));

        var buttons = _driver.FindElements(By.CssSelector("button[data-bs-target='#aiCompanionModal']"));
        Assert.That(buttons, Is.Empty,
            "Anonymous users should not see the AI Companion button");
    }

    [Then("{word} should see a reply from the AI Companion")]
    public void ThenPersonaShouldSeeAReplyFromTheAICompanion(string name)
    {
        var reply = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("#aiCompanionMessages .ai-reply"));
            return !string.IsNullOrWhiteSpace(el.Text) ? el : null;
        });

        Assert.That(reply, Is.Not.Null, $"{name} should see a non-empty reply from the AI Companion");
    }

    [Then("{word} should see a reply redirecting the conversation back to wildlife topics")]
    public void ThenPersonaShouldSeeAReplyRedirectingBackToWildlife(string name)
    {
        var reply = _wait.Until(d =>
        {
            var el = d.FindElement(By.CssSelector("#aiCompanionMessages .ai-reply"));
            return !string.IsNullOrWhiteSpace(el.Text) ? el : null;
        });

        Assert.That(reply, Is.Not.Null);
        var replyText = reply!.Text;

        // The hidden system prompt should keep the assistant on-topic; the reply
        // should mention wildlife/animals/safety rather than answering the off-topic query.
        var staysOnTopic =
            replyText.Contains("wildlife", StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("animal", StringComparison.OrdinalIgnoreCase) ||
            replyText.Contains("nature", StringComparison.OrdinalIgnoreCase);

        Assert.That(staysOnTopic, Is.True,
            $"Off-topic replies should redirect to wildlife. Actual: '{replyText}'");
    }

    #endregion
}
