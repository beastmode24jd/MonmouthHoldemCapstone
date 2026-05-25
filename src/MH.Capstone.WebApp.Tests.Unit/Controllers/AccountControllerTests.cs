using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.Domain.Tools;
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
    private Mock<INotificationService> _mockNotificationService;
    private Mock<IFollowService> _mockFollowService;
    private Mock<IBlockService> _mockBlockService;
    private AccountController _controller;
    private Mock<UserManager<ApplicationUser>> _mockUserManager;
    private Mock<IUrlHelper> _mockUrlHelper;
    private Mock<IClubService> _mockClubService;
    private Mock<IBadgeService> _mockBadgeService;
    private Mock<ISightingsService> _mockSightingService;

    [SetUp]
    public void Setup()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<AccountController>>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockFollowService = new Mock<IFollowService>();
        _mockBlockService = new Mock<IBlockService>();
        _mockBadgeService = new Mock<IBadgeService>();
        _mockClubService = new Mock<IClubService>();
        _mockSightingService = new Mock<ISightingsService>();
        _mockUrlHelper = new Mock<IUrlHelper>();

        // Mock UserManager (requires a Mock UserStore)
        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockBadgeService.Setup(s => s.SortBadgesByTime(It.IsAny<List<UserBadge>>()))
        .ReturnsAsync(new List<UserBadge>());

        _mockClubService.Setup(s => s.GetRecentUserClubsAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<Club>());

        _mockSightingService.Setup(s => s.GetUserSightingsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new List<Sighting>());

        _controller = new AccountController(
            _mockAuthService.Object,
            _mockUserService.Object,
            _mockUserManager.Object,
            _mockNotificationService.Object,
            new FeatureFlags(),
            _mockFollowService.Object,
            _mockBlockService.Object,
            _mockBadgeService.Object,
            _mockClubService.Object,
            _mockSightingService.Object,
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

        // Mock user exists, is not deactivated, and has confirmed their email
        var user = new ApplicationUser { Email = loginModel.Email, IsDeactivated = false, EmailConfirmed = true };
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
    public async Task Register_Post_WithValidData_RedirectsToConfirmation()
    {
        var registerModel = new RegisterViewModel
        {
            Email = "newuser@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            DisplayName = "TestUser"
        };

        _mockAuthService.Setup(s => s.RegisterUserAsync(registerModel.Email, registerModel.Password, registerModel.DisplayName)).ReturnsAsync(true);

        // FindByEmailAsync returns null → email-sending block is skipped (no email service in unit tests)
        _mockUserManager.Setup(m => m.FindByEmailAsync(registerModel.Email))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.Register(registerModel);

        var redirectResult = result as RedirectToActionResult;
        Assert.That(redirectResult, Is.Not.Null);
        Assert.That(redirectResult.ActionName, Is.EqualTo("RegisterConfirmation"));
        Assert.That(redirectResult.ControllerName, Is.EqualTo("Account"));
        _mockAuthService.Verify(s => s.RegisterUserAsync(registerModel.Email, registerModel.Password, registerModel.DisplayName), Times.Once);
        // Registration no longer auto-signs in — user must verify email first
        _mockAuthService.Verify(s => s.SignInUserAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
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
    public async Task Register_Post_SendsVerificationNotification_WhenUserFound()
    {
        var registerModel = new RegisterViewModel
        {
            Email = "newuser@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123",
            DisplayName = "TestUser"
        };
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = registerModel.Email };

        _mockAuthService.Setup(s => s.RegisterUserAsync(registerModel.Email, registerModel.Password, registerModel.DisplayName)).ReturnsAsync(true);
        _mockUserManager.Setup(m => m.FindByEmailAsync(registerModel.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("test-token");
        _mockNotificationService.Setup(s => s.SendNotificationAsync(It.IsAny<Notification>(), NotificationType.SystemCritical)).Returns(Task.CompletedTask);

        await _controller.Register(registerModel);

        _mockNotificationService.Verify(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.Title == "Verify Your Email"),
            NotificationType.SystemCritical), Times.Once);
    }

    [Test]
    public async Task ForgotPassword_Post_SendsResetNotification_WhenUserExistsAndIsActive()
    {
        var model = new ForgotPasswordViewModel { Email = "test@example.com" };
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = model.Email, IsDeactivated = false };

        _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.UpdateSecurityStampAsync(user)).ReturnsAsync(IdentityResult.Success);
        _mockUserManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("test-reset-token");
        _mockNotificationService.Setup(s => s.SendNotificationAsync(It.IsAny<Notification>(), NotificationType.SystemCritical)).Returns(Task.CompletedTask);

        var result = await _controller.ForgotPassword(model);

        _mockNotificationService.Verify(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.Title == "Password Reset Requested"),
            NotificationType.SystemCritical), Times.Once);
        var viewResult = result as ViewResult;
        Assert.That(viewResult, Is.Not.Null);
        Assert.That(((ForgotPasswordViewModel)viewResult!.Model!).EmailSent, Is.True);
    }

    [Test]
    public async Task ResendVerification_Post_SendsVerificationNotification_WhenUserExistsAndNotConfirmed()
    {
        var model = new ResendVerificationViewModel { Email = "test@example.com" };
        var user = new ApplicationUser { Id = Guid.NewGuid().ToString(), Email = model.Email, EmailConfirmed = false };

        _mockUserManager.Setup(m => m.FindByEmailAsync(model.Email)).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GenerateEmailConfirmationTokenAsync(user)).ReturnsAsync("test-token");
        _mockNotificationService.Setup(s => s.SendNotificationAsync(It.IsAny<Notification>(), NotificationType.SystemCritical)).Returns(Task.CompletedTask);

        var result = await _controller.ResendVerification(model);

        _mockNotificationService.Verify(s => s.SendNotificationAsync(
            It.Is<Notification>(n => n.Title == "Verify Your Email"),
            NotificationType.SystemCritical), Times.Once);
        var viewResult = result as ViewResult;
        Assert.That(viewResult, Is.Not.Null);
        Assert.That(((ResendVerificationViewModel)viewResult!.Model!).EmailSent, Is.True);
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

    // -- CSP-211 ----------------------------------------------------------------
    // Counts and the first-page follower/following lists must populate on both
    // the viewer's own profile and on other users' profiles.

    [Test]
    public async Task Index_OwnProfile_PopulatesFollowerAndFollowingCountsAndLists()
    {
        var me = MakeUser("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "alex@test.com", "Alex");
        var follower1 = MakeUser(Guid.NewGuid().ToString(), "bob@test.com", "Bob");
        var follower2 = MakeUser(Guid.NewGuid().ToString(), "carol@test.com", "Carol");
        var followee1 = MakeUser(Guid.NewGuid().ToString(), "dave@test.com", "Dave");

        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(me);
        _mockFollowService.Setup(s => s.GetFollowerIdsAsync(me.GuidId))
            .ReturnsAsync(new[] { follower1.GuidId, follower2.GuidId });
        _mockFollowService.Setup(s => s.GetFolloweeIdsAsync(me.GuidId))
            .ReturnsAsync(new[] { followee1.GuidId });
        _mockUserService.Setup(s => s.GetUserByIdAsync(follower1.GuidId)).ReturnsAsync(follower1);
        _mockUserService.Setup(s => s.GetUserByIdAsync(follower2.GuidId)).ReturnsAsync(follower2);
        _mockUserService.Setup(s => s.GetUserByIdAsync(followee1.GuidId)).ReturnsAsync(followee1);

        var result = await _controller.Index(id: null) as ViewResult;
        var vm = result?.Model as AccountViewModel;

        Assert.That(vm, Is.Not.Null);
        Assert.That(vm!.FollowerCount, Is.EqualTo(2));
        Assert.That(vm.FolloweeCount, Is.EqualTo(1));
        Assert.That(vm.Followers.Select(f => f.DisplayName), Is.EquivalentTo(new[] { "Bob", "Carol" }));
        Assert.That(vm.Followees.Select(f => f.DisplayName), Is.EquivalentTo(new[] { "Dave" }));
    }

    [Test]
    public async Task Index_OtherUserProfile_PaginatesFollowerListToTwentyPerPage()
    {
        var me = MakeUser("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "alex@test.com", "Alex");
        var target = MakeUser("cccccccc-cccc-cccc-cccc-cccccccccccc", "lily@test.com", "Lily");

        // 25 followers — page 1 should return 20, page 2 should return 5.
        var followerIds = Enumerable.Range(0, 25).Select(_ => Guid.NewGuid()).ToList();
        foreach (var fid in followerIds)
        {
            var u = MakeUser(fid.ToString(), $"u{fid}@test.com", $"User-{fid:N}".Substring(0, 8));
            _mockUserService.Setup(s => s.GetUserByIdAsync(fid)).ReturnsAsync(u);
        }

        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(me);
        _mockUserManager.Setup(m => m.FindByIdAsync(target.Id)).ReturnsAsync(target);
        _mockFollowService.Setup(s => s.GetFollowerIdsAsync(target.GuidId)).ReturnsAsync(followerIds);
        _mockFollowService.Setup(s => s.GetFolloweeIdsAsync(target.GuidId)).ReturnsAsync(Array.Empty<Guid>());

        var pageOne = (await _controller.Index(target.GuidId, followersPage: 1) as ViewResult)?.Model as AccountViewModel;
        var pageTwo = (await _controller.Index(target.GuidId, followersPage: 2) as ViewResult)?.Model as AccountViewModel;

        Assert.That(pageOne!.FollowerCount, Is.EqualTo(25));
        Assert.That(pageOne.Followers.Count, Is.EqualTo(20));
        Assert.That(pageOne.FollowersTotalPages, Is.EqualTo(2));
        Assert.That(pageTwo!.Followers.Count, Is.EqualTo(5));
    }

    [Test]
    public async Task Index_NewUserWithNoFollows_ReturnsZeroCountsAndEmptyLists()
    {
        var me = MakeUser("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", "alex@test.com", "Alex");
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(me);
        _mockFollowService.Setup(s => s.GetFollowerIdsAsync(me.GuidId)).ReturnsAsync(Array.Empty<Guid>());
        _mockFollowService.Setup(s => s.GetFolloweeIdsAsync(me.GuidId)).ReturnsAsync(Array.Empty<Guid>());

        var result = await _controller.Index(id: null) as ViewResult;
        var vm = result?.Model as AccountViewModel;

        Assert.That(vm, Is.Not.Null);
        Assert.That(vm!.FollowerCount, Is.EqualTo(0));
        Assert.That(vm.FolloweeCount, Is.EqualTo(0));
        Assert.That(vm.Followers, Is.Empty);
        Assert.That(vm.Followees, Is.Empty);
    }

    private static ApplicationUser MakeUser(string id, string email, string displayName) => new ApplicationUser
    {
        Id = id,
        UserName = email,
        Email = email,
        DisplayName = displayName,
        UserBadges = new List<UserBadge>()
    };
}