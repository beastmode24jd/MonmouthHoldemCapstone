using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP26StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly AcceptanceTestSettings _settings;

    public CSP26StepDefinitions(IWebDriver driver, AcceptanceTestSettings settings)
    {
        _driver = driver;
        _settings = settings;
    }

    [Given("I am on the Login Page")]
    public void GivenIAmOnTheLoginPage()
    {
        // Access the page.
        _driver.Navigate().GoToUrl($"{_settings.BaseUrl}/account/login");

    }

    [When("I look at the Login input form")]
    public void WhenILookAtTheLoginInputForm()
    {
        // Check for the Login input form on the page
        _driver.FindElement(By.Id("loginForm")).Displayed.Should().BeTrue();
    }

    [Then("I should see a Forgot Password link")]
    public void ThenIShouldSeeAForgotPasswordLink()
    {
        // Look for the Forgot Password using exact text "Forgot Password?"
        var forgotPasswordLink = _driver.FindElement(By.LinkText("Forgot Password?"));
        forgotPasswordLink.Displayed.Should().BeTrue();
    }

    [Then("it should change colors and my mouse cursor when I hover over it")]
    public void ThenItShouldChangeColorsAndMyMouseCursorWhenIHoverOverIt()
    {
        // Get the link element
        var link = _driver.FindElement(By.LinkText("Forgot Password?"));

        // Capture initial color before hovering
        string initialColor = link.GetCssValue("color");

        // Hover
        var actions = new Actions(_driver);
        actions.MoveToElement(link).Perform();

        // Capture the CSS values during the hover
        string hoverColor = link.GetCssValue("color");
        string hoverCursor = link.GetCssValue("cursor");

        // Verify color changed (Bootstrap's link-primary changes on hover)
        hoverColor.Should().NotBe(initialColor, "The link color should change when hovered.");
        
        // Verify cursor changes to a pointer
        hoverCursor.Should().Be("pointer", "The mouse cursor should be a pointer when hovering over the link.");
    }

    [Given("I am on the Forgot Password page")]
    public void GivenIAmOnTheForgotPasswordPage()
    {
        // Access the page.
        _driver.Navigate().GoToUrl($"{_settings.BaseUrl}/account/ForgotPassword");
    }

    [When("I submit an account search for an account that does not exist")]
    public void WhenISubmitAnAccountSearchForAnAccountThatDoesNotExist()
    {
        var emailField = _driver.FindElement(By.Id("forgotPasswordEmail"));
        emailField.SendKeys("nonexistent@example.com");

        _driver.FindElement(By.Id("sendResetEmailBtn")).Click();
    }

    [When("I search for a valid account that exists")]
    public void WhenISearchForAValidAccountThatExists()
    {
        var emailField = _driver.FindElement(By.Id("forgotPasswordEmail"));
        emailField.SendKeys("alex@test.com");

        _driver.FindElement(By.Id("sendResetEmailBtn")).Click();
    }

    [Then("I should see a password reset confirmation message")]
    public void ThenIShouldSeeAPasswordResetConfirmationMessage()
    {
        // The controller always shows this message regardless of whether the account exists,
        // intentionally preventing account enumeration.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var confirmation = wait.Until(d => d.FindElement(By.Id("resetEmailSentMessage")));

        confirmation.Displayed.Should().BeTrue("the reset confirmation message should appear after any form submission.");
    }

    [Given("I am on the Confirm New Password page")]
    public void GivenIAmOnTheConfirmNewPasswordPage()
    {
        // Access the page.
        _driver.Navigate().GoToUrl($"{_settings.BaseUrl}/account/ForgotPassword");

        // Search a valid email, then submit.
        var emailField = _driver.FindElement(By.Id("emailInput"));
        emailField.SendKeys("alex@test.com");

        var searchButton = _driver.FindElement(By.Id("submitBtn"));
        searchButton.Click();

        // Wait, then locate the Confirm Password fields by ID, confirm they are visible
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var newPasswordField = wait.Until(d => d.FindElement(By.Id("newPasswordField")));
        var confirmPasswordField = wait.Until(d => d.FindElement(By.Id("confirmPasswordField")));

        newPasswordField.Displayed.Should().BeTrue("The New Password field should be visible after a successful account search.");
        confirmPasswordField.Displayed.Should().BeTrue("The Confirm Password field should be visible after a successful account search.");
    }

    [When("I submit passwords that do not match")]
    public void WhenISubmitPasswordsThatDoNotMatch()
    {
        // Get the password fields
        var newPasswordField = _driver.FindElement(By.Id("newPasswordField"));
        var confirmPasswordField = _driver.FindElement(By.Id("confirmPasswordField"));

        // Assign the password inputs
        newPasswordField.SendKeys("Capstone27!");
        confirmPasswordField.SendKeys("Capstone28!");
    }

    [When("I click Save")]
    public void WhenIClickSave()
    {
        // Click the button
        var saveButton = _driver.FindElement(By.Id("submitBtn"));
        saveButton.Click();
    }

    [Then("I should see an error message telling me the inputs do not match")]
    public void ThenIShouldSeeAnErrorMessageTellingMeTheInputsDoNotMatch()
    {
        // Locate the validation summary alert (wait 5 seconds)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var errorSpan = wait.Until(d => d.FindElement(By.Id("confirmPasswordError")));
        
        // This string must match the ErrorMessage in ForgotPasswordViewModel.cs
        string expectedMessage = "The two passwords do not match.";
        
        errorSpan.Text.Should().Be(expectedMessage);
        errorSpan.Displayed.Should().BeTrue();
    }

    // Reference line 139 for Given I am on the Confirm New Password page

    [When("I submit two matching passwords")]
    public void WhenISubmitTwoMatchingPasswords()
    {
        // Get the password fields
        var newPasswordField = _driver.FindElement(By.Id("newPasswordField"));
        var confirmPasswordField = _driver.FindElement(By.Id("confirmPasswordField"));

        // Assign the password inputs
        newPasswordField.SendKeys("Capstone27!");
        confirmPasswordField.SendKeys("Capstone27!");

        // Click the button
        var saveButton = _driver.FindElement(By.Id("submitBtn"));
        saveButton.Click();
    }

    [Then("I should be redirected to the Login page")]
    public void ThenIShouldBeRedirectedToTheLoginPage()
    {
        // Wait so the page can load
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        
        // Check for the Login input form on the page
        _driver.FindElement(By.Id("loginForm")).Displayed.Should().BeTrue();

        var loginField = wait.Until(d => d.FindElement(By.Id("loginForm")));
        loginField.Displayed.Should().BeTrue("The New Password field should be visible after a successful account search.");
    }

    [Then("have my new password")]
    public void ThenHaveMyNewPassword()
    {
        // Provide the username and new password
        var emailInput = _driver.FindElement(By.Id("emailField"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var loginButton = _driver.FindElement(By.Id("submitBtn"));

        // Enter Alex's credentials
        emailInput.SendKeys("alex@test.com");
        passwordInput.SendKeys("Capstone27!");

        // Submit the form
        loginButton.Click();
    }
}