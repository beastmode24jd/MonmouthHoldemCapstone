using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Controllers;
using MH.Capstone.WebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Controllers;

[TestFixture]
[ExcludeFromCodeCoverage]
public class AnidexControllerTests
{
    private Mock<ILogger<AnidexController>> _mockLogger = null!;
    private Mock<ISightingsService> _mockSightingsService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private AnidexController _controller = null!;

    private static readonly string TestUserId = Guid.NewGuid().ToString();
    private static readonly ApplicationUser TestUser = new()
    {
        Id = TestUserId,
        UserName = "alex@test.com",
        Email = "alex@test.com"
    };

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<AnidexController>>();
        _mockSightingsService = new Mock<ISightingsService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _controller = new AnidexController(
            _mockLogger.Object,
            _mockSightingsService.Object,
            _mockUserManager.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, TestUserId)
                }, "mock"))
            }
        };

        _mockUserManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(TestUser);
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    [Test]
    public async Task Index_AuthenticatedUserWithSightings_ReturnsViewWithEntries()
    {
        // Arrange
        var entries = new List<AnidexEntry>
        {
            new("Coyote",     2, "Common", 1.0, [0x01], DateTimeOffset.UtcNow.AddDays(-1)),
            new("Bald Eagle", 1, "Mythic", 5.0, [0x02], DateTimeOffset.UtcNow.AddDays(-3)),
        };

        _mockSightingsService
            .Setup(s => s.GetUserAnidexAsync(TestUser.GuidId))
            .ReturnsAsync(entries);

        // Act
        var result = await _controller.Index();

        // Assert
        var view = result as ViewResult;
        Assert.That(view, Is.Not.Null);
        var vm = view!.Model as AnidexViewModel;
        Assert.That(vm, Is.Not.Null);
        Assert.That(vm!.TotalSpecies, Is.EqualTo(2));
        Assert.That(vm.IsEmpty, Is.False);
    }

    [Test]
    public async Task Index_AuthenticatedUserWithNoSightings_ReturnsViewWithEmptyVm()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetUserAnidexAsync(TestUser.GuidId))
            .ReturnsAsync([]);

        // Act
        var result = await _controller.Index();

        // Assert
        var view = result as ViewResult;
        Assert.That(view, Is.Not.Null);
        var vm = view!.Model as AnidexViewModel;
        Assert.That(vm, Is.Not.Null);
        Assert.That(vm!.IsEmpty, Is.True);
        Assert.That(vm.TotalSpecies, Is.EqualTo(0));
    }

    [Test]
    public async Task Index_NoAuthenticatedUserResolved_Returns500()
    {
        // Arrange — UserManager returns null even though the cookie is present.
        _mockUserManager
            .Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        var result = await _controller.Index();

        // Assert
        var status = result as StatusCodeResult;
        Assert.That(status, Is.Not.Null);
        Assert.That(status!.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task Index_DelegatesToServiceWithUsersGuidId()
    {
        // Arrange
        _mockSightingsService
            .Setup(s => s.GetUserAnidexAsync(It.IsAny<Guid>()))
            .ReturnsAsync([]);

        // Act
        await _controller.Index();

        // Assert
        _mockSightingsService.Verify(
            s => s.GetUserAnidexAsync(It.Is<Guid>(g => g == TestUser.GuidId)),
            Times.Once);
    }
}
