using System.Runtime.CompilerServices;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;
using Moq;
using OpenQA.Selenium;
using Reqnroll;
using FluentAssertions;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP42StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly ScenarioContext _scenarioContext;

    // CONST FIELD
    private const string ExpectedDefaultImagePath = "/imgs/profileDefault.jpg";

    public CSP42StepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        // Retrieve the driver initialized in the Hook
        _driver = (IWebDriver)scenarioContext["WebDriver"];
    }

    [Given("I have not submitted a custom profile image")]
    public void GivenIHaveNotSubmittedACustomProfileImage()
    {
        // Log in user who has not submitted a custom profile image
        _driver.Navigate().GoToUrl("https://localhost:7147/account/login");

        // Provide valid username and password params
        var emailInput = _driver.FindElement(By.Id("emailField"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var loginButton = _driver.FindElement(By.Id("submitBtn"));

        // Enter Alex's persona credentials
        emailInput.SendKeys("alex@test.com");
        passwordInput.SendKeys("Capstone26!");

        // Submit the form [cite: 205]
        loginButton.Click();
    }

    [When("I look at the menu bar at the top of the page")]
    public void WhenILookAtTheMenuBarAtTheTopOfThePage()
    {
        // Check icon element while logged in.
        // Uses ID from _Layout.cshtml [cite: 142]
        // Assign result of FindElement to a variable
        var navProfileImg = _driver.FindElement(By.Id("navProfile"));

        // Store it in scenario context for "Then", using _scenarioContext
        _scenarioContext["NavProfileElement"] = navProfileImg;
    }

    [Then("I should see a placeholder image")]
    public void ThenIShouldSeeAPlaceholderImage()
    {
        // Retrieve the element from the context
        var navProfileImg = (IWebElement)ScenarioContext.Current["NavProfileElement"];

        // Get the 'src' attribute [cite: 141]
        string actualSrc = navProfileImg.GetAttribute("src");

        // Assert image source ends with default placeholder path
        // Selenium often returns the full absolute URL, check if it ends with relative path const
        actualSrc.Should().EndWith(ExpectedDefaultImagePath, 
            "because a user without a custom upload should see the default placeholder.");
    }

    /* USEFUL FOR LATER TESTING

        // Takes them to the dashboard
        _driver.Navigate().GoToUrl("https://localhost:7147/dashboard");

    */
}