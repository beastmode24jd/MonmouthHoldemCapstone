using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // Required for TempDataDictionary
using Microsoft.AspNetCore.Mvc.Routing; // Required for UrlActionContext class

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
public class AccountControllerTests
{
    private Mock<IAuthenticationService> _mockAuthService;
    private Mock<IUserService> _mockUserService;
    private Mock<ILogger<AccountController>> _mockLogger;
    private AccountController _controller;
    private Mock<UserManager<ApplicationUser>> _mockUserManager;
    private Mock<IUrlHelper> _mockUrlHelper;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _mockUrlHelper = new Mock<IUrlHelper>();

        // Mock UserManager (requires a Mock UserStore)
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new AccountController(
            _mockAuthService.Object,
            _mockUserService.Object,
            _mockUserManager.Object,
            _mockLogger.Object);

        // Setup the Mock URL Helper to return a dummy string
        _mockUrlHelper
            .Setup(x => x.Action(It.IsAny<UrlActionContext>()))
            .Returns("/account/login");

        // Assign the mock to the controller
        _controller.Url = _mockUrlHelper.Object;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Initialize TempData to prevent NullReferenceException on redirects
        var itdp = new Mock<ITempDataProvider>();
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, itdp.Object);
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

        // Mock user exists and is not deactivated
        var user = new ApplicationUser { Email = loginModel.Email, IsDeactivated = false };
        _mockUserService.Setup(s => s.GetUserByEmailAsync(loginModel.Email)).ReturnsAsync(user);

        // Mock password check passes
        _mockAuthService.Setup(s => s.CheckPasswordAsync(loginModel.Email, loginModel.Password)).ReturnsAsync(true);
        _mockAuthService.Setup(s => s.SignInUserAsync(It.IsAny<HttpContext>(), loginModel.Email, loginModel.RememberMe)).Returns(Task.CompletedTask);

        // Mock UserManager behavior when Login() looks up user to update streaks/timezone
        _mockUserManager.Setup(m => m.FindByEmailAsync(loginModel.Email))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Login(loginModel, null);

        var redirectResult = result as RedirectToActionResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult!.ActionName, Is.EqualTo("Index"));
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

        // Mock user exists
        var user = new ApplicationUser { Email = loginModel.Email, IsDeactivated = false };
        _mockUserService.Setup(s => s.GetUserByEmailAsync(loginModel.Email)).ReturnsAsync(user);

        // Mock password check fails
        _mockAuthService.Setup(s => s.CheckPasswordAsync(loginModel.Email, loginModel.Password)).ReturnsAsync(false);

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

        _mockUserService.Setup(s => s.UserExistsAsync(registerModel.Email)).ReturnsAsync(true);

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
}