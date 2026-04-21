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
using OpenQA.Selenium.Support.UI;
using Reqnroll;
using FluentAssertions;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP124StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly ScenarioContext _scenarioContext;

    // CONST FIELD WILL GO HERE IF NEEDED

    public CSP124StepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        // Retrieve the driver initialized in the Hook
        _driver = (IWebDriver)scenarioContext["WebDriver"];
    }

    [Given("I am on the front page")]
    public void GivenIAmOnTheFrontPage()
    {
        // Should not be able to see Clubs in nav bar if not logged in.
    }
}