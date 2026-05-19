using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Abstraction;
using MH.Capstone.WebApp.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MH.Capstone.WebApp.Tests.Unit.Hubs;

[TestFixture]
[ExcludeFromCodeCoverage]
public class ChatHubTests
{
    private Mock<IClubService> _mockClubService = null!;
    private Mock<UserManager<ApplicationUser>> _mockUserManager = null!;
    private Mock<IHubCallerClients> _mockClients = null!;
    private Mock<IGroupManager> _mockGroups = null!;
    private Mock<HubCallerContext> _mockContext = null!;

    private static readonly string TestConnectionId = "test-connection-id";
    private static readonly Guid TestClubId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private static readonly ApplicationUser TestUser = new()
    {
        Id = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        DisplayName = "Alex",
        UserName = "alex@test.com",
        Email = "alex@test.com"
    };

    [SetUp]
    public void SetUp()
    {
        _mockClubService = new Mock<IClubService>();

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _mockUserManager = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _mockClients = new Mock<IHubCallerClients>();
        _mockGroups = new Mock<IGroupManager>();
        _mockContext = new Mock<HubCallerContext>();
        _mockContext.Setup(c => c.ConnectionId).Returns(TestConnectionId);
    }

    // GetHttpContext() is an extension method — Moq cannot mock extension methods.
    // ChatHub exposes GetClubIdQueryParam() as a protected virtual method so tests
    // can override it directly without needing to wire up the ASP.NET feature collection.
    private sealed class TestableChatHub(
        IClubService clubService,
        UserManager<ApplicationUser> userManager,
        string? clubIdQueryParam) : ChatHub(clubService, userManager)
    {
        protected override string? GetClubIdQueryParam() => clubIdQueryParam;
    }
    
    // DefaultHttpContext registers itself as IHttpContextFeature in its own Features
    // collection, so returning httpContext.Features from the mock makes GetHttpContext()
    // resolve to that same HttpContext without needing to reference IHttpContextFeature directly.
    private void SetHttpContext(DefaultHttpContext httpContext)
    {
        _mockContext.Setup(c => c.Features).Returns(httpContext.Features);
    }

    private ChatHub CreateSut(string? clubIdQueryParam = null)
    {
        var queryValue = clubIdQueryParam ?? TestClubId.ToString();
        httpContext.Request.QueryString = new QueryString($"?clubId={queryValue}");
        SetHttpContext(httpContext);

        return new TestableChatHub(_mockClubService.Object, _mockUserManager.Object, queryValue)
        {
            Clients = _mockClients.Object,
            Groups = _mockGroups.Object,
            Context = _mockContext.Object
        };
    }

    // ── OnConnectedAsync ──────────────────────────────────────────────────────

    [Test]
    public async Task OnConnectedAsync_WithValidClubId_AddsConnectionToClubGroup()
    {
        var hub = CreateSut();

        await hub.OnConnectedAsync();

        _mockGroups.Verify(g =>
            g.AddToGroupAsync(TestConnectionId, $"club-{TestClubId}", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task OnConnectedAsync_WithInvalidClubId_DoesNotAddToGroup()
    {
        var hub = CreateSut(clubIdQueryParam: "not-a-guid");

        await hub.OnConnectedAsync();

        _mockGroups.Verify(g =>
            g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task OnConnectedAsync_WithMissingClubId_DoesNotAddToGroup()
    {
        // No clubId — TestableChatHub returns null from GetClubIdQueryParam.
        var hub = new TestableChatHub(_mockClubService.Object, _mockUserManager.Object, null)
        {
            Clients = _mockClients.Object,
            Groups = _mockGroups.Object,
            Context = _mockContext.Object
        };

        await hub.OnConnectedAsync();

        _mockGroups.Verify(g =>
            g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── SendMessage ───────────────────────────────────────────────────────────

    private ClaimsPrincipal MakeClaimsPrincipal(string userId) =>
        new(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "mock"));

    [Test]
    public async Task SendMessage_ValidInput_CallsClubServiceWithCorrectClubIdAndAuthor()
    {
        var principal = MakeClaimsPrincipal(TestUser.Id);
        _mockContext.Setup(c => c.User).Returns(principal);
        _mockUserManager.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestUser);

        var mockProxy = new Mock<IClientProxy>();
        _mockClients.Setup(c => c.Group($"club-{TestClubId}")).Returns(mockProxy.Object);

        var hub = CreateSut();
        await hub.SendMessage(TestClubId, "Hello world");

        _mockClubService.Verify(s =>
            s.SendMessageAsync(TestClubId, TestUser.GuidId, "Hello world"),
            Times.Once);
    }

    [Test]
    public async Task SendMessage_ValidInput_BroadcastsReceiveMessageEventToClubGroup()
    {
        var principal = MakeClaimsPrincipal(TestUser.Id);
        _mockContext.Setup(c => c.User).Returns(principal);
        _mockUserManager.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestUser);

        var mockProxy = new Mock<IClientProxy>();
        _mockClients.Setup(c => c.Group($"club-{TestClubId}")).Returns(mockProxy.Object);

        var hub = CreateSut();
        await hub.SendMessage(TestClubId, "Hello!");

        mockProxy.Verify(p =>
            p.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object?[]>(args => args.Length == 1),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SendMessage_ValidInput_BroadcastsCorrectAuthorDisplayName()
    {
        var principal = MakeClaimsPrincipal(TestUser.Id);
        _mockContext.Setup(c => c.User).Returns(principal);
        _mockUserManager.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestUser);

        object? capturedPayload = null;
        var mockProxy = new Mock<IClientProxy>();
        mockProxy
            .Setup(p => p.SendCoreAsync("ReceiveMessage", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Callback<string, object?[], CancellationToken>((_, args, _) => capturedPayload = args[0])
            .Returns(Task.CompletedTask);
        _mockClients.Setup(c => c.Group($"club-{TestClubId}")).Returns(mockProxy.Object);

        var hub = CreateSut();
        await hub.SendMessage(TestClubId, "Hi");

        Assert.That(capturedPayload, Is.Not.Null);
        var payloadJson = System.Text.Json.JsonSerializer.Serialize(capturedPayload);
        Assert.That(payloadJson, Does.Contain("Alex"));
    }

    [Test]
    public async Task SendMessage_UserNotFound_DoesNotCallClubServiceOrBroadcast()
    {
        var principal = MakeClaimsPrincipal("ghost-user");
        _mockContext.Setup(c => c.User).Returns(principal);
        _mockUserManager.Setup(m => m.GetUserAsync(principal))
            .ReturnsAsync((ApplicationUser?)null);

        var mockProxy = new Mock<IClientProxy>();
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockProxy.Object);

        var hub = CreateSut();
        await hub.SendMessage(TestClubId, "Hello!");

        _mockClubService.Verify(s =>
            s.SendMessageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);

        mockProxy.Verify(p =>
            p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public void SendMessage_ServiceThrows_ExceptionPropagates()
    {
        var principal = MakeClaimsPrincipal(TestUser.Id);
        _mockContext.Setup(c => c.User).Returns(principal);
        _mockUserManager.Setup(m => m.GetUserAsync(principal)).ReturnsAsync(TestUser);

        _mockClubService
            .Setup(s => s.SendMessageAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Message content cannot be empty."));

        var mockProxy = new Mock<IClientProxy>();
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockProxy.Object);

        var hub = CreateSut();
        Assert.ThrowsAsync<ArgumentException>(() => hub.SendMessage(TestClubId, ""));
    }
}
