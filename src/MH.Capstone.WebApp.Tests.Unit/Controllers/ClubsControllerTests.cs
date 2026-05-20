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
public class ClubsControllerTests
{
    private Mock<ILogger<ClubsController>> _mockLogger = null!;
    private Mock<IClubService> _mockClubService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<INotificationService> _mockNotificationService = null!;
    private ClubsController _controller = null!;

    private static readonly ApplicationUser TestUser = new()
    {
        Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        DisplayName = "Alex",
        UserName = "alex@test.com",
        Email = "alex@test.com"
    };

    private static readonly Guid TestClubId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ClubsController>>();
        _mockClubService = new Mock<IClubService>();
        _mockNotificationService = new Mock<INotificationService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(TestUser);

        _controller = new ClubsController(
            _mockLogger.Object,
            _mockClubService.Object,
            _mockUserManager.Object,
            _mockNotificationService.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, TestUser.Id) }, "mock"))
            }
        };
    }

    [TearDown]
    public void TearDown() => _controller?.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Club MakePublicClub(Guid? id = null) => new()
    {
        Id = id ?? TestClubId,
        Name = "Test Club",
        IsPublic = true,
        OwnerId = TestUser.GuidId
    };

    private Club MakePrivateClub(Guid? id = null) => new()
    {
        Id = id ?? TestClubId,
        Name = "Secret Club",
        IsPublic = false,
        OwnerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
    };

    private void SetUserTimeZoneCookie(string ianaId)
    {
        var ctx = (DefaultHttpContext)_controller.ControllerContext.HttpContext;
        ctx.Request.Headers["Cookie"] = $"UserTimeZone={ianaId}";
    }

    private async Task<ClubMessageViewModel> ChatroomViewModel(Guid clubId)
    {
        var result = (ViewResult)await _controller.Chatroom(clubId);
        return (ClubMessageViewModel)result.Model!;
    }

    // ── Chatroom — routing / access control ───────────────────────────────────

    [Test]
    public async Task Chatroom_UnknownClub_ReturnsNotFound()
    {
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId))
            .ReturnsAsync((Club?)null);

        var result = await _controller.Chatroom(TestClubId);

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Chatroom_PrivateClub_NonMember_ReturnsForbid()
    {
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId))
            .ReturnsAsync(MakePrivateClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId))
            .ReturnsAsync(new List<Club>());

        var result = await _controller.Chatroom(TestClubId);

        Assert.That(result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task Chatroom_PublicClub_NonMember_ReturnsChatroomView()
    {
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId))
            .ReturnsAsync(MakePublicClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId))
            .ReturnsAsync(new List<Club>());
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message>());

        var result = await _controller.Chatroom(TestClubId) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ViewName, Is.EqualTo("Chatroom"));
    }

    [Test]
    public async Task Chatroom_PrivateClub_Member_ReturnsChatroomView()
    {
        var club = MakePrivateClub();
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId)).ReturnsAsync(club);
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId))
            .ReturnsAsync(new List<Club> { club });
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message>());

        var result = await _controller.Chatroom(TestClubId) as ViewResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ViewName, Is.EqualTo("Chatroom"));
    }

    [Test]
    public async Task Chatroom_NullUser_Returns500()
    {
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((ApplicationUser?)null);

        var result = await _controller.Chatroom(TestClubId) as StatusCodeResult;

        Assert.That(result?.StatusCode, Is.EqualTo(500));
    }

    // ── Chatroom — view model ─────────────────────────────────────────────────

    [Test]
    public async Task Chatroom_ViewModelHasCorrectCurrentUserId()
    {
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId))
            .ReturnsAsync(MakePublicClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId))
            .ReturnsAsync(new List<Club>());
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message>());

        var vm = await ChatroomViewModel(TestClubId);

        Assert.That(vm.CurrentUserId, Is.EqualTo(TestUser.Id));
    }

    [Test]
    public async Task Chatroom_IsCurrentUserMember_TrueWhenUserIsInClub()
    {
        var club = MakePublicClub();
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId)).ReturnsAsync(club);
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId))
            .ReturnsAsync(new List<Club> { club });
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message>());

        var vm = await ChatroomViewModel(TestClubId);

        Assert.That(vm.IsCurrentUserMember, Is.True);
    }

    [Test]
    public async Task Chatroom_IsCurrentUserMember_FalseWhenUserIsNotInClub()
    {
        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId))
            .ReturnsAsync(MakePublicClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId))
            .ReturnsAsync(new List<Club>());
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message>());

        var vm = await ChatroomViewModel(TestClubId);

        Assert.That(vm.IsCurrentUserMember, Is.False);
    }

    // ── Chatroom — timezone conversion ────────────────────────────────────────

    [Test]
    public async Task Chatroom_ConvertsSentAtToUserTimezone()
    {
        var utcTime = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var message = new Message
        {
            Id = Guid.NewGuid(), ClubId = TestClubId,
            AuthorIdentityId = TestUser.Id, Content = "Hello", SentAt = utcTime
        };

        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId)).ReturnsAsync(MakePublicClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId)).ReturnsAsync(new List<Club>());
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message> { message });

        SetUserTimeZoneCookie("Asia/Kolkata");

        var vm = await ChatroomViewModel(TestClubId);

        var kolkata = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
        var expected = TimeZoneInfo.ConvertTime(utcTime, kolkata);
        Assert.That(vm.Messages[0].SentAt, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task Chatroom_MissingUserTimeZoneCookie_FallsBackToPacificTime()
    {
        var utcTime = new DateTimeOffset(2026, 1, 15, 20, 0, 0, TimeSpan.Zero);
        var message = new Message
        {
            Id = Guid.NewGuid(), ClubId = TestClubId,
            AuthorIdentityId = TestUser.Id, Content = "Hi", SentAt = utcTime
        };

        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId)).ReturnsAsync(MakePublicClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId)).ReturnsAsync(new List<Club>());
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message> { message });

        // No cookie — should default to Pacific time.

        var vm = await ChatroomViewModel(TestClubId);

        var pacific = TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
        var expected = TimeZoneInfo.ConvertTime(utcTime, pacific);
        Assert.That(vm.Messages[0].SentAt, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task Chatroom_InvalidUserTimeZoneCookie_FallsBackToPacificTime()
    {
        var utcTime = new DateTimeOffset(2026, 3, 10, 6, 0, 0, TimeSpan.Zero);
        var message = new Message
        {
            Id = Guid.NewGuid(), ClubId = TestClubId,
            AuthorIdentityId = TestUser.Id, Content = "Hi", SentAt = utcTime
        };

        _mockClubService.Setup(s => s.GetClubByIdAsync(TestClubId)).ReturnsAsync(MakePublicClub());
        _mockClubService.Setup(s => s.GetUserClubsAsync(TestUser.GuidId)).ReturnsAsync(new List<Club>());
        _mockClubService.Setup(s => s.GetClubMessagesAsync(TestClubId))
            .ReturnsAsync(new List<Message> { message });

        SetUserTimeZoneCookie("Not/A_Real_Zone");

        var vm = await ChatroomViewModel(TestClubId);

        var pacific = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        var expected = TimeZoneInfo.ConvertTime(utcTime, pacific);
        Assert.That(vm.Messages[0].SentAt, Is.EqualTo(expected).Within(TimeSpan.FromSeconds(1)));
    }
}
