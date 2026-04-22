using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class CSP42StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly ScenarioContext _scenarioContext;
    private readonly AcceptanceTestSettings _settings;

    // CONST FIELD
    private const string ExpectedDefaultImagePath = "/imgs/profileDefault.jpg";
    const long MAX_IMG_SIZE = 2 * 1024 * 1024;

    public CSP42StepDefinitions(IWebDriver driver, ScenarioContext scenarioContext, AcceptanceTestSettings settings)
    {
        _driver = driver;
        _scenarioContext = scenarioContext;
        _settings = settings;
    }

    private void LoginAs(string email, string password)
    {
        // Navigate to the app domain first so DeleteAllCookies operates on the
        // correct domain context (about:blank has no domain, so cookies would not be cleared).
        _driver.Navigate().GoToUrl(_settings.BaseUrl);
        _driver.Manage().Cookies.DeleteAllCookies();
        _driver.Navigate().GoToUrl($"{_settings.BaseUrl}/account/login");
        _driver.FindElement(By.Id("emailField")).SendKeys(email);
        _driver.FindElement(By.Id("passwordField")).SendKeys(password);

        // The submit button starts disabled and is enabled by JS after fields are filled.
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));
        var submitBtn = wait.Until(d => {
            var btn = d.FindElement(By.Id("submitBtn"));
            return btn.Enabled ? btn : null;
        });
        submitBtn!.Click();
    }

    [Given("I have not submitted a custom profile image")]
    public void GivenIHaveNotSubmittedACustomProfileImage()
    {
        LoginAs("alex@test.com", "Capstone26!");
    }

    [When("I look at the menu bar at the top of the page")]
    public void WhenILookAtTheMenuBarAtTheTopOfThePage()
    {
        // Wait for the dashboard to load
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));

        // Check icon element while logged in.
        // Uses ID from _Layout.cshtml
        var navProfileImg = wait.Until(d => d.FindElement(By.Id("navProfile")));
        navProfileImg.Displayed.Should().BeTrue();

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
        LoginAs("alex@test.com", "Capstone26!");
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
        LoginAs("lily@test.com", "Capstone26!");
        
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

    }

    [Then("the image is displayed as my new avatar")]
    public void ThenTheImageIsDisplayedAsMyNewAvatar()
    {
        // Wait for the dashboard to load
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));

        // Check icon element while logged in.
        // Uses ID from _Layout.cshtml
        var navProfileImg = wait.Until(d => d.FindElement(By.Id("navProfile")));
        navProfileImg.Displayed.Should().BeTrue();

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
        LoginAs("lily@test.com", "Capstone26!");

        // Get the current profile image path/src, save to scenario context for later comparison
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(5));

        // Uses ID from _Layout.cshtml
        var navProfileImg = wait.Until(d => d.FindElement(By.Id("navProfile")));
        navProfileImg.Displayed.Should().BeTrue();

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