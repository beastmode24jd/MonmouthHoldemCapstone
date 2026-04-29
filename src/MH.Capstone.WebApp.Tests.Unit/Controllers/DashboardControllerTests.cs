using System.Reflection;
using System.Security.Claims;
using System.Text;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
public class DashboardControllerTests
{
    // Mocks and method access
    private Mock<IAuthenticationService> _mockAuthService;
    private Mock<IUserService> _mockUserService;
    private Mock<IProfileImageService> _mockProfileImageService;
    private Mock<INotificationService> _mockNotificationService;
    private Mock<IRepository<Notification, ApplicationDbContext>> _mockNotificationRepo;
    private Mock<ILogger<DashboardController>> _mockLogger;
    private DashboardController _controller;
    private Mock<IBadgeService> _mockBadgeService;
    private Mock<INotificationPreferenceService> _mockNotificationPreferenceService;
    private const string TestEmail = "namesNameington@mail.wou";

    [SetUp]
    public void SetUp()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockProfileImageService = new Mock<IProfileImageService>();
        _mockLogger = new Mock<ILogger<DashboardController>>();
        _mockUserService = new Mock<IUserService>();
        _mockNotificationService = new Mock<INotificationService>();
        _mockNotificationRepo = new Mock<IRepository<Notification, ApplicationDbContext>>();
        _mockBadgeService = new Mock<IBadgeService>();
        _mockNotificationPreferenceService = new Mock<INotificationPreferenceService>();

        _controller = new DashboardController(_mockLogger.Object,
            _mockProfileImageService.Object, _mockAuthService.Object,
            _mockBadgeService.Object, _mockNotificationService.Object,
            _mockUserService.Object, _mockNotificationRepo.Object,
            _mockNotificationPreferenceService.Object);
        
        // Mock the user, so the display name isn't null while testing
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.Name, TestEmail),
        }, "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };

        var tempDataProvider = new Mock<ITempDataProvider>();
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, tempDataProvider.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Dispose of controller to satisfy line 20 being unhappy
        _controller?.Dispose();
    }

    [Test]
    public void Settings_ReturnsView()
    {
        var result = _controller.Settings();

        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task UpdateDisplayName_ValidName_RedirectsToSettings()
    {
        var user = new ApplicationUser { Email = TestEmail, DisplayName = "OldName" };
        _mockUserService.Setup(s => s.GetUserByEmailAsync(TestEmail)).ReturnsAsync(user);

        var result = await _controller.UpdateDisplayName("NewName");

        var redirect = result as RedirectToActionResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.ActionName, Is.EqualTo("Settings"));
    }

    [Test]
    public async Task UpdateDisplayName_TooShort_RedirectsToSettings()
    {
        var user = new ApplicationUser { Email = TestEmail, DisplayName = "OldName" };
        _mockUserService.Setup(s => s.GetUserByEmailAsync(TestEmail)).ReturnsAsync(user);

        var result = await _controller.UpdateDisplayName("X");

        var redirect = result as RedirectToActionResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.ActionName, Is.EqualTo("Settings"));
        Assert.That(_controller.TempData["DisplayNameError"], Is.Not.Null);
    }

    [Test]
    public async Task UploadProfileImage_Successful_ByteArrayToService()
    {
        // Arrange
        var fileMock = CreateMockFile("test.jpg", "image/jpeg", "content");
        var dummyBytes = new byte[] { 0x20, 0x21, 0x22 }; // Creates dummy data

        // Tells service to return dummy bytes
        _mockProfileImageService.Setup(s => s.ConvertToBytesAsync(It.IsAny<IFormFile>()))
                    .ReturnsAsync(dummyBytes);

        // Mocking the user lookup, which is required for the new UploadImage Badge
        _mockUserService.Setup(s => s.GetUserByEmailAsync(TestEmail))
                .ReturnsAsync(new ApplicationUser { Email = TestEmail });

        // Act
        await _controller.UploadProfileImage(fileMock.Object);

        // Assert
        // Verify that the Auth Service was told to update the user's profile image
        // in localDB
        _mockUserService.Verify(s => s.UpdateUserProfileImageAsync(
        TestEmail,
        It.Is<byte[]>(b => b.Length > 0),
        fileMock.Object.ContentType), 
    Times.Once);
    }

    // Helper method, for mocking:
    private Mock<IFormFile> CreateMockFile(string fileName, string contentType, string content)
    {
        var fileMock = new Mock<IFormFile>();
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;

        fileMock.Setup(_ => _.FileName).Returns(fileName);
        fileMock.Setup(_ => _.Length).Returns(ms.Length);
        fileMock.Setup(_ => _.ContentType).Returns(contentType);
        fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
        fileMock.Setup(_ => _.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream stream, CancellationToken token) => ms.CopyToAsync(stream, token));

        return fileMock;
    }

    #region NotificationPreferences

    [Test]
    public async Task NotificationPreferences_IncludesClubActivity()
    {
        var user = new ApplicationUser { Email = TestEmail };
        _mockUserService.Setup(s => s.GetUserByClaimsPrincipleAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.FromResult<ApplicationUser?>(user));
        _mockNotificationPreferenceService
            .Setup(s => s.GetPreferencesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(Enumerable.Empty<UserNotificationPreference>());

        var result = await _controller.NotificationPreferences();

        var view = result as ViewResult;
        var model = view?.Model as NotificationPreferencesViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Preferences.Any(p => p.NotificationType == NotificationType.ClubActivity), Is.True);
    }

    [Test]
    public async Task NotificationPreferences_ExcludesSystemCritical()
    {
        var user = new ApplicationUser { Email = TestEmail };
        _mockUserService.Setup(s => s.GetUserByClaimsPrincipleAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.FromResult<ApplicationUser?>(user));
        _mockNotificationPreferenceService
            .Setup(s => s.GetPreferencesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(Enumerable.Empty<UserNotificationPreference>());

        var result = await _controller.NotificationPreferences();

        var view = result as ViewResult;
        var model = view?.Model as NotificationPreferencesViewModel;
        Assert.That(model, Is.Not.Null);
        Assert.That(model!.Preferences.Any(p => p.NotificationType == NotificationType.SystemCritical), Is.False);
    }

    [Test]
    public async Task NotificationPreferences_ContainsEntryForEveryDisplayAnnotatedEnumValue()
    {
        var user = new ApplicationUser { Email = TestEmail };
        _mockUserService.Setup(s => s.GetUserByClaimsPrincipleAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.FromResult<ApplicationUser?>(user));
        _mockNotificationPreferenceService
            .Setup(s => s.GetPreferencesAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(Enumerable.Empty<UserNotificationPreference>());

        var result = await _controller.NotificationPreferences();

        var view = result as ViewResult;
        var model = view?.Model as NotificationPreferencesViewModel;
        Assert.That(model, Is.Not.Null);

        var expectedTypes = Enum.GetValues<NotificationType>()
            .Where(t => typeof(NotificationType)
                .GetField(t.ToString())
                ?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>() != null)
            .ToList();

        Assert.That(model!.Preferences.Select(p => p.NotificationType),
            Is.EquivalentTo(expectedTypes));
    }

    [Test]
    public async Task NotificationPreferences_StoredClubActivityPreference_ReflectedInViewModel()
    {
        var user = new ApplicationUser { Email = TestEmail };
        _mockUserService.Setup(s => s.GetUserByClaimsPrincipleAsync(It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.FromResult<ApplicationUser?>(user));
        _mockNotificationPreferenceService
            .Setup(s => s.GetPreferencesAsync(user))
            .ReturnsAsync(new[]
            {
                new UserNotificationPreference(user.Id, NotificationType.ClubActivity, NotificationDeliveryChannel.EmailOnly)
            });

        var result = await _controller.NotificationPreferences();

        var view = result as ViewResult;
        var model = view?.Model as NotificationPreferencesViewModel;
        var clubPref = model?.Preferences.FirstOrDefault(p => p.NotificationType == NotificationType.ClubActivity);
        Assert.That(clubPref, Is.Not.Null);
        Assert.That(clubPref!.SelectedChannel, Is.EqualTo(NotificationDeliveryChannel.EmailOnly));
    }

    #endregion
}