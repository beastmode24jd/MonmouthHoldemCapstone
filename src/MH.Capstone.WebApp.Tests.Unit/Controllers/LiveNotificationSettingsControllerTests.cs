using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
[ExcludeFromCodeCoverage]
public class LiveNotificationSettingsControllerTests
{
    private const string TestUserId = "user-1";

    private Mock<ILiveNotificationPreferenceService> _mockPreferences = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private LiveNotificationSettingsController _controller = null!;
    private Mock<ITempDataDictionary> _mockTempData = null!;

    [SetUp]
    public void SetUp()
    {
        _mockPreferences = new Mock<ILiveNotificationPreferenceService>();

        var store = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(TestUserId);

        _controller = new LiveNotificationSettingsController(
            _mockPreferences.Object,
            _mockUserManager.Object);

        _mockTempData = new Mock<ITempDataDictionary>();
        _controller.TempData = _mockTempData.Object;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("test")) }
        };
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    [Test]
    public async Task Index_Get_ReturnsViewWithCurrentEnabledValue()
    {
        _mockPreferences.Setup(p => p.IsEnabledAsync(TestUserId)).ReturnsAsync(false);

        var result = await _controller.Index();

        var view = result as ViewResult;
        Assert.That(view, Is.Not.Null);
        var model = view!.Model as LiveNotificationSettingsViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Enabled, Is.False);
    }

    [Test]
    public async Task Index_Get_DefaultEnabledTrue_ReturnsViewWithEnabledTrue()
    {
        _mockPreferences.Setup(p => p.IsEnabledAsync(TestUserId)).ReturnsAsync(true);

        var result = await _controller.Index();

        var view = result as ViewResult;
        var model = view!.Model as LiveNotificationSettingsViewModel;
        Assert.That(model!.Enabled, Is.True);
    }

    [Test]
    public async Task Index_Post_SavesPreferenceAndRedirectsToIndex()
    {
        var posted = new LiveNotificationSettingsViewModel { Enabled = false };

        var result = await _controller.Index(posted);

        _mockPreferences.Verify(p => p.SetEnabledAsync(TestUserId, false), Times.Once);
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.ActionName, Is.EqualTo(nameof(LiveNotificationSettingsController.Index)));
    }

    [Test]
    public async Task Index_Get_NoUser_ReturnsChallenge()
    {
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        var result = await _controller.Index();

        Assert.That(result, Is.InstanceOf<ChallengeResult>());
    }

    [Test]
    public async Task Index_Post_NoUser_ReturnsChallenge()
    {
        _mockUserManager.Setup(m => m.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string?)null);

        var result = await _controller.Index(new LiveNotificationSettingsViewModel());

        _mockPreferences.Verify(p => p.SetEnabledAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        Assert.That(result, Is.InstanceOf<ChallengeResult>());
    }
}
