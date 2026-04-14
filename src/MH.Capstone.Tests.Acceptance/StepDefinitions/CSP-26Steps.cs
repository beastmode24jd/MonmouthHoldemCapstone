using System.Runtime.CompilerServices;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;
using System.Linq.Expressions;
using Moq;
using OpenQA.Selenium.Interactions; // Required for Hover
using OpenQA.Selenium;
using Reqnroll;
using FluentAssertions;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP26StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly ScenarioContext _scenarioContext;


    public CSP26StepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        // Retrieve the driver initialized in the Hook
        _driver = (IWebDriver)scenarioContext["WebDriver"];
    }

    [Given("I am on the Login Page")]
    public void GivenIAmOnTheLoginPage()
    {
        // Access the page.
        _driver.Navigate().GoToUrl("https://localhost:7147/account/login");

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

    // Gets the ForgotPassword page.
    // _driver.Navigate().GoToUrl("https://localhost:7147/account/ForgotPassword");

    // Gets the Forgot Password form from the page.
    //var forgotPasswordForm = _driver.FindElement(By.Id("forgotPasswordForm"));

    // Need to submit email and click search button, then write the new password twice.
    // Redirects to Login page if successful.
}