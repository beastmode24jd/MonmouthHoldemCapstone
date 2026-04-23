using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Configuration;
using MH.Capstone.Tests.Acceptance.Drivers;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[ExcludeFromCodeCoverage]
public class CSP42StepDefinitions
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;
    private readonly string _baseUrl;
    private readonly AuthenticationDriver _authDriver;
    private readonly DashboardDriver _dashboardDriver;

    private const string ExpectedDefaultImagePath = "/imgs/profileDefault.jpg";
    private const long MaxImgSize = 2 * 1024 * 1024;

    // Holds image paths between Given and When steps within the same scenario.
    private string? _validIconUploadPath;
    private string? _invalidIconUploadPath;
    private string? _initialProfileImageSrc;

    public CSP42StepDefinitions(IWebDriver driver, WebDriverWait wait,
        AcceptanceTestSettings settings, AuthenticationDriver authDriver, DashboardDriver dashboardDriver)
    {
        _driver = driver;
        _wait = wait;
        _baseUrl = settings.BaseUrl.TrimEnd('/');
        _authDriver = authDriver;
        _dashboardDriver = dashboardDriver;
    }

    [Given("I have not submitted a custom profile image")]
    public void GivenIHaveNotSubmittedACustomProfileImage()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("I look at the menu bar at the top of the page")]
    public void WhenILookAtTheMenuBarAtTheTopOfThePage()
    {
        var navProfileImg = _wait.Until(d => d.FindElement(By.Id("navProfile")));
        navProfileImg.Displayed.Should().BeTrue();
    }

    [Then("I should see a placeholder image")]
    public void ThenIShouldSeeAPlaceholderImage()
    {
        var navProfileImg = _wait.Until(d => d.FindElement(By.Id("navProfile")));
        string? actualSrc = navProfileImg.GetAttribute("src");
        actualSrc.Should().EndWith(ExpectedDefaultImagePath,
            "a user without a custom upload should see the default placeholder");
    }

    [Given("I am logged in")]
    public void GivenIAmLoggedIn()
    {
        _authDriver.PreformLoginForUser("alex@test.com", "Capstone26!");
    }

    [When("I navigate to the Profile Customization part of my Dashboard")]
    public void WhenINavigateToTheProfileCustomizationPartOfMyDashboard()
    {
        _dashboardDriver.NavigateToDashboard();

        var customProfileForm = _wait.Until(d => d.FindElement(By.Id("uploadForm")));
        customProfileForm.Displayed.Should().BeTrue();
    }

    [Then("I can select a profile image to upload from my device")]
    public void ThenICanSelectAProfileImageToUploadFromMyDevice()
    {
        var fileUpload = _wait.Until(d => d.FindElement(By.Id("fileInput")));
        fileUpload.Displayed.Should().BeTrue();
        fileUpload.Enabled.Should().BeTrue();

        string? acceptAttribute = fileUpload.GetAttribute("accept");
        acceptAttribute.Should().Be("image/*",
            "the profile upload should only allow image files");
    }

    [Given("I have selected a valid image under 2 MB")]
    public void GivenIHaveSelectedAValidImageUnder2MB()
    {
        _authDriver.PreformLoginForUser("lily@test.com", "Capstone26!");

        string validImagePath = Path.GetFullPath(
            "../../../../MH.Capstone.WebApp/wwwroot/imgs/badge/BadgeIcon1.jpg");

        var fileInfo = new FileInfo(validImagePath);
        fileInfo.Exists.Should().BeTrue("the test image file must exist at the specified path");
        fileInfo.Length.Should().BeLessThanOrEqualTo(MaxImgSize,
            $"the test image should be under 2 MB, but was {fileInfo.Length} bytes");

        _validIconUploadPath = validImagePath;
    }

    [When("I click the Upload Image button")]
    public void WhenIClickTheUploadImageButton()
    {
        _dashboardDriver.NavigateToDashboard();

        var fileInput = _wait.Until(d => d.FindElement(By.Id("fileInput")));
        fileInput.SendKeys(_validIconUploadPath!);

        var uploadForm = _wait.Until(d => d.FindElement(By.Id("uploadForm")));
        uploadForm.Submit();
    }

    [Then("the image is displayed as my new avatar")]
    public void ThenTheImageIsDisplayedAsMyNewAvatar()
    {
        var navProfileImg = _wait.Until(d => d.FindElement(By.Id("navProfile")));
        navProfileImg.Displayed.Should().BeTrue();

        string? actualSrc = navProfileImg.GetAttribute("src");
        actualSrc.Should().NotBe(ExpectedDefaultImagePath,
            "the uploaded image should replace the placeholder");
        actualSrc.Should().StartWith("data:image/",
            "the uploaded image should be rendered as a Base64 data URI");

        byte[] fileBytes = File.ReadAllBytes(_validIconUploadPath!);
        string base64File = Convert.ToBase64String(fileBytes);
        actualSrc.Should().Contain(base64File,
            "the displayed image should match the bytes of the uploaded file");
    }

    [Given("I have selected an image larger than 2 MB")]
    public void GivenIHaveSelectedAnImageLargerThan2MB()
    {
        _authDriver.PreformLoginForUser("lily@test.com", "Capstone26!");

        var navProfileImg = _wait.Until(d => d.FindElement(By.Id("navProfile")));
        navProfileImg.Displayed.Should().BeTrue();
        _initialProfileImageSrc = navProfileImg.GetAttribute("src");

        string invalidImagePath = Path.GetFullPath(
            "../../../../MH.Capstone.WebApp/wwwroot/imgs/ValleyPhoto1.jpg");

        var fileInfo = new FileInfo(invalidImagePath);
        fileInfo.Exists.Should().BeTrue("the oversized test image must exist at the specified path");
        fileInfo.Length.Should().BeGreaterThan(MaxImgSize,
            $"the test image should be over 2 MB, but was {fileInfo.Length} bytes");

        _invalidIconUploadPath = invalidImagePath;
    }

    [When("I save the invalid image")]
    public void WhenISaveTheInvalidImage()
    {
        _dashboardDriver.NavigateToDashboard();

        var fileInput = _wait.Until(d => d.FindElement(By.Id("fileInput")));
        fileInput.SendKeys(_invalidIconUploadPath!);

        var uploadForm = _wait.Until(d => d.FindElement(By.Id("uploadForm")));
        uploadForm.Submit();
    }

    [Then("the system should show me a clear and informative error message")]
    public void ThenTheSystemShouldShowMeAClearAndInformativeErrorMessage()
    {
        IAlert alert = _driver.SwitchTo().Alert();
        string? alertText = alert.Text;

        alertText.Should().Contain("2MB", "the alert should specify the file size limit");
        alertText.Should().Contain("exceeds", "the alert should state the file is too large");

        alert.Accept();
    }

    [Then("the profile image should remain unchanged")]
    public void ThenTheProfileImageShouldRemainUnchanged()
    {
        var navProfileImg = _wait.Until(d => d.FindElement(By.Id("navProfile")));
        string? currentSrc = navProfileImg.GetAttribute("src");

        currentSrc.Should().Be(_initialProfileImageSrc,
            "an invalid upload should not change the existing profile image");
    }
}
