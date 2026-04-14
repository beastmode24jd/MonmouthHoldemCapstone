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
public class CSP42StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly ScenarioContext _scenarioContext;

    // CONST FIELD
    private const string ExpectedDefaultImagePath = "/imgs/profileDefault.jpg";
    const long MAX_IMG_SIZE = 2 * 1024 * 1024;

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

        // Enter Alex's credentials
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
        var navProfileImg = (IWebElement)_scenarioContext["NavProfileElement"];

        // Get the 'src' attribute [cite: 141]
        string? actualSrc = navProfileImg.GetAttribute("src");

        // Assert image source ends with default placeholder path
        // Selenium often returns the full absolute URL, check if it ends with relative path const
        actualSrc.Should().EndWith(ExpectedDefaultImagePath, 
            "because a user without a custom upload should see the default placeholder.");
    }

    [Given("I am logged in")]
    public void GivenIAmLoggedIn()
    {
        // Log in user who has not submitted a custom profile image
        _driver.Navigate().GoToUrl("https://localhost:7147/account/login");

        // Provide valid username and password params
        var emailInput = _driver.FindElement(By.Id("emailField"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var loginButton = _driver.FindElement(By.Id("submitBtn"));

        // Enter Alex's credentials
        emailInput.SendKeys("alex@test.com");
        passwordInput.SendKeys("Capstone26!");

        // Submit the form [cite: 205]
        loginButton.Click();
    }

    [When("I navigate to the Profile Customization part of my Dashboard")]
    public void WhenINavigateToTheProfileCustomizationPartOfMyDashboard()
    {
        // Locate the validation summary alert (wait 5 seconds)
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));

        // Make sure the profile customization form exists on the Dashboard
        var customProfileForm = wait.Until(d => d.FindElement(By.Id("uploadForm")));

        customProfileForm.Displayed.Should().BeTrue();
    }

    [Then("I can select a profile image to upload from my device")]
    public void ThenICanSelectAProfileImageToUploadFromMyDevice()
    {
        // Find the fileUpload element
        var fileUpload = _driver.FindElement(By.Id("fileInput"));

        fileUpload.Displayed.Should().BeTrue();
        fileUpload.Enabled.Should().BeTrue();

        // Verify that it accepts image file types
        string? acceptAttribute = fileUpload.GetAttribute("accept");

        // Check for "image/*" to ensure the browser filter is restricted to images
        acceptAttribute.Should().Be("image/*", 
            "because the profile upload should only allow image files to prevent invalid formats.");
    }

    [Given("I have selected a valid image under 2 MB")]
    public void GivenIHaveSelectedAValidImageUnder2MB()
    {
        // Log in Lily, who we will use for upload testing
        _driver.Navigate().GoToUrl("https://localhost:7147/account/login");

        // Provide valid username and password params
        var emailInput = _driver.FindElement(By.Id("emailField"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var loginButton = _driver.FindElement(By.Id("submitBtn"));

        // Enter Lily's credentials
        emailInput.SendKeys("lily@test.com");
        passwordInput.SendKeys("Capstone26!");

        // Submit the form [cite: 205]
        loginButton.Click();
        
        // Get a valid jpg file, <= 2 MB
        string validImagePath = Path.GetFullPath("../../../../MH.Capstone.WebApp/wwwroot/imgs/badge/BadgeIcon1.jpg");

        // Verify the file exists and check its size
        FileInfo fileInfo = new FileInfo(validImagePath);
        fileInfo.Exists.Should().BeTrue("the test image file must exist at the specified path.");

        fileInfo.Length.Should().BeLessThanOrEqualTo(MAX_IMG_SIZE, 
            $"the test image should be under 2MB, but was {fileInfo.Length} bytes.");

        // Store it in scenario context for "When", using _scenarioContext
        _scenarioContext["ValidIconUpload"] = validImagePath;
    }

    [When("I click the Upload Image button")]
    public void WhenIClickTheUploadImageButton()
    {
        // Retrieve file path from the context
        string imagePath = (string)_scenarioContext["ValidIconUpload"];

        // Get file input and "send" the file path to it
        // Populates the input field, bypasses OS file picker
        var fileInput = _driver.FindElement(By.Id("fileInput"));
        fileInput.SendKeys(imagePath);

        // Submit the form
        var uploadForm = _driver.FindElement(By.Id("uploadForm"));
        uploadForm.Submit(); 

        // Add for time buffer, so page is guaranteed to load for next step
        _driver.Navigate().Refresh();
    }

    [Then("the image is displayed as my new avatar")]
    public void ThenTheImageIsDisplayedAsMyNewAvatar()
    {
        // Retrieve the user icon from the dashboard
        var navProfileImg = _driver.FindElement(By.Id("navProfile"));

        // Get the 'src' attribute [cite: 141]
        string? actualSrc = navProfileImg.GetAttribute("src");

        // Get the original image path from current context
        string storedFilePath = (string)_scenarioContext["ValidIconUpload"];

        // Check that Dashboard image is no longer placeholder image
            actualSrc.Should().NotBe(ExpectedDefaultImagePath);

        // Because the app converts the file to a Base64 string for display,
        //      check if the 'src' starts with the data:image prefix.
        actualSrc.Should().StartWith("data:image/", 
            "because the uploaded image should be rendered as a Base64 data string.");

        // Assert image uploaded and image displayed are the exact same
        //  by comparing the file bytes
        byte[] fileBytes = File.ReadAllBytes(storedFilePath);
        string base64File = Convert.ToBase64String(fileBytes);
        actualSrc.Should().Contain(base64File, 
            "because the displayed image should match the bytes of the file we uploaded.");
    }

    [Given("I have selected an image larger than 2 MB")]
    public void GivenIHaveSelectedAnImageLargerThan2MB()
    {
        // Log in Lily
        _driver.Navigate().GoToUrl("https://localhost:7147/account/login");

        var emailInput = _driver.FindElement(By.Id("emailField"));
        var passwordInput = _driver.FindElement(By.Id("passwordField"));
        var loginButton = _driver.FindElement(By.Id("submitBtn"));

        // Enter credentials
        emailInput.SendKeys("lily@test.com");
        passwordInput.SendKeys("Capstone26!");

        // Submit the form [cite: 205]
        loginButton.Click();

        // Get the current profile image path/src, save to scenario context for later comparison
        var navProfileImg = _driver.FindElement(By.Id("navProfile"));
        string? initialSrc = navProfileImg.GetAttribute("src");

        _scenarioContext["InitialProfileImage"] = initialSrc;
        
        // Get an invalid jpg file, > 2 MB
        string invalidImagePath = Path.GetFullPath("../../../../MH.Capstone.WebApp/wwwroot/imgs/ValleyPhoto1.jpg");

        // Verify the file exists and check its size
        FileInfo fileInfo = new FileInfo(invalidImagePath);
        fileInfo.Exists.Should().BeTrue("the test image file must exist at the specified path.");

        fileInfo.Length.Should().BeGreaterThan(MAX_IMG_SIZE, 
            $"the test image should be over 2MB, but was {fileInfo.Length} bytes.");

        // Store it in scenario context for "When", using _scenarioContext
        _scenarioContext["InvalidIconUpload"] = invalidImagePath;
    }

    [When("I save the invalid image")]
    public void WhenISaveTheInvalidImage()
    {
        // Retrieve file path from the context
        string invalidImgPath = (string)_scenarioContext["InvalidIconUpload"];

        // Get file input and "send" the file path to it
        // Populates the input field, bypasses OS file picker
        var fileInput = _driver.FindElement(By.Id("fileInput"));
        fileInput.SendKeys(invalidImgPath);

        // Submit the form
        var uploadForm = _driver.FindElement(By.Id("uploadForm"));
        uploadForm.Submit(); 
    }

    [Then("the system should show me a clear and informative error message")]
    public void ThenTheSystemShouldShowMeAClearAndInformativeErrorMessage()
    {
        // Find the alert.
        IAlert alert = _driver.SwitchTo().Alert();

        // Read the message.
        string? alertText = alert.Text;

        // Check that the message mentions the 2MB limit
        alertText.Should().Contain("2MB", "because the alert should specify the file size limit.");
        alertText.Should().Contain("exceeds", "because the alert should state the file is too large.");

        // Click 'OK' on the alert for the next step
        alert.Accept();
    }

    // "And" in Gherkin test becomes the Step that came before it
    [Then("the profile image should remain unchanged")]
    public void ThenTheProfileImageShouldRemainUnchanged()
    {
        // Verify the profile image in the nav bar is still the default 
        // and hasn't changed to the invalid upload
        string initialSrc = (string)_scenarioContext["InitialProfileImage"];

        // Get the profile image, post-upload attempt
        var navProfileImg = _driver.FindElement(By.Id("navProfile"));
        string? currentSrc = navProfileImg.GetAttribute("src");

        // Image should be the same
        currentSrc.Should().Be(initialSrc, 
            "because an invalid upload should not modify the existing profile image.");
    }
}