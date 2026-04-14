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
        // Write more later
    }

    // Gets the ForgotPassword page.
    // _driver.Navigate().GoToUrl("https://localhost:7147/account/ForgotPassword");

    // Need to submit email and click search button, then write the new password twice.
    // Redirects to Login page if successful.
}