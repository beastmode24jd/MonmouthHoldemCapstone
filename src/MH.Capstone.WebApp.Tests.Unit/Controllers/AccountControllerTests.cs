using MH.Capstone.Domain.Services;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
public class AccountControllerTests
{
    private Mock<IAuthenticationService> _mockAuthService;
    private Mock<ILogger<AccountController>> _mockLogger;
    private AccountController _controller;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _controller = new AccountController(_mockAuthService.Object, _mockLogger.Object);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
    }

    [Test]
    public void Login_Get_ReturnsViewResult()
    {
        var result = _controller.Login(returnUrl: null);
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Login_Post_WithValidCredentials_RedirectsToDashboard()
    {
        var loginModel = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Test@123",
            RememberMe = false
        };

        _mockAuthService.Setup(s => s.ValidateCredentialsAsync(loginModel.Email, loginModel.Password)).ReturnsAsync(true);
        _mockAuthService.Setup(s => s.SignInUserAsync(It.IsAny<HttpContext>(), loginModel.Email, loginModel.RememberMe)).Returns(Task.CompletedTask);

        var result = await _controller.Login(loginModel);

        var redirectResult = result as RedirectToActionResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
        Assert.That(redirectResult.ControllerName, Is.EqualTo("Dashboard"));
        _mockAuthService.Verify(s => s.SignInUserAsync(It.IsAny<HttpContext>(), loginModel.Email, loginModel.RememberMe), Times.Once);
    }

    [Test]
    public async Task Login_Post_WithInvalidCredentials_ReturnsViewWithError()
    {
        var loginModel = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _mockAuthService.Setup(s => s.ValidateCredentialsAsync(loginModel.Email, loginModel.Password)).ReturnsAsync(false);

        var result = await _controller.Login(loginModel);

        var viewResult = result as ViewResult;
        Assert.That(viewResult, Is.Not.Null);
        Assert.That(_controller.ModelState.IsValid, Is.False);
        _mockAuthService.Verify(s => s.SignInUserAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public void Register_Get_ReturnsViewResult()
    {
        var result = _controller.Register();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Register_Post_WithValidData_RedirectsToDashboard()
    {
        var registerModel = new RegisterViewModel
        {
            Email = "newuser@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        _mockAuthService.Setup(s => s.RegisterUserAsync(registerModel.Email, registerModel.Password)).ReturnsAsync(true);
        _mockAuthService.Setup(s => s.SignInUserAsync(It.IsAny<HttpContext>(), registerModel.Email, false)).Returns(Task.CompletedTask);

        var result = await _controller.Register(registerModel);

        var redirectResult = result as RedirectToActionResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
        Assert.That(redirectResult.ControllerName, Is.EqualTo("Dashboard"));
        _mockAuthService.Verify(s => s.RegisterUserAsync(registerModel.Email, registerModel.Password), Times.Once);
        _mockAuthService.Verify(s => s.SignInUserAsync(It.IsAny<HttpContext>(), registerModel.Email, false), Times.Once);
    }

    [Test]
    public async Task Register_Post_WithDuplicateEmail_ReturnsViewWithError()
    {
        var registerModel = new RegisterViewModel
        {
            Email = "existing@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        _mockAuthService.Setup(s => s.UserExistsAsync(registerModel.Email)).ReturnsAsync(true);

        var result = await _controller.Register(registerModel);

        var viewResult = result as ViewResult;
        Assert.That(viewResult, Is.Not.Null);
        Assert.That(_controller.ModelState.IsValid, Is.False);
        _mockAuthService.Verify(s => s.SignInUserAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Test]
    public async Task Logout_Post_SignsOutAndRedirectsToHome()
    {
        _mockAuthService.Setup(s => s.SignOutUserAsync(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        var result = await _controller.Logout();

        var redirectResult = result as RedirectToActionResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
        Assert.That(redirectResult.ControllerName, Is.EqualTo("Home"));
        _mockAuthService.Verify(s => s.SignOutUserAsync(It.IsAny<HttpContext>()), Times.Once);
    }

    [Test]
    public async Task Upload_Successful_UploadsAndSavesProfileImage()
    {
        /*
        // Arrange
        var mockService = new Mock<IImageService>();
        mockService.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("https://fake.blob.core/image.jpg");

        _controller = new AccountController(mockService.Object, _logger);

        // Act
        await _controller.Upload(1, someFakeFile);

        // Assert
        var profile = _context.Profiles.Find(1);
        Assert.AreEqual("https://fake.blob.core/image.jpg", profile.ProfilePictureUrl);
        */
    }

    [Test]
    public async Task Upload_NotSuccessful_ReturnsErrorMessage()
    {
        /*
        // Arrange
        var mockService = new Mock<IImageService>();
        mockService.Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>()))
                .ReturnsAsync("https://fake.blob.core/image.jpg");

        _controller = new AccountController(mockService.Object, _logger);

        // Act
        await _controller.Upload(1, someFakeFile);
        // "Some fake file" should be over the set file size limit.

        // Assert
        var profile = _context.Profiles.NotFound(1);
        */
    }

    [Test]
    public async Task SaveBio_Successful_UpdatesProfileAttributes()
    {
        // Arrange
        // Set up Dashboard view with dummy bio text variable

        // Act
        // Run through the Account Controller SaveBio method,
        //      with the bio text variable

        // Assert
        // Check that the bio text matches the aspect field.
        
    }

    [Test]
    public async Task SaveBio_EmptyString_DisplaysErrorMessage()
    {
        // Arrange
        // Set up Dashboard view with dummy bio text variable as empty string

        // Act
        // Run through the Account Controller SaveBio method,
        //      with empty string

        // Assert
        // Check that an error message is displayed to the user,
        //  and that the bio field in the Model remains blank.
        
    }
}