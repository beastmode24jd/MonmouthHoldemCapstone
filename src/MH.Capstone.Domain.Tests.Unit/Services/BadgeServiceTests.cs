using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;
using System.Text;
using Moq;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
public class BadgeServiceTests
{
    private ServiceProvider _serviceProvider;
    private ApplicationDbContext _context;
    private IBadgeService _badgeService;

    // Add in the IRepositories? ----------
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;
    private Mock<IRepository<Badge, ApplicationDbContext>> _badgeRepoMock;
    private Mock<IRepository<UserBadge, ApplicationDbContext>> _userBadgeRepoMock;
    // ------------------------------------

    private Guid _testBadgeId;
    

    [SetUp]
    public async Task Setup()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection();

        // Add logging (required by Identity)
        services.AddLogging();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddScoped<IBadgeService, BadgeService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Add in the Mocked Repositories
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
        _badgeRepoMock = new Mock<IRepository<Badge, ApplicationDbContext>>();
        _userBadgeRepoMock = new Mock<IRepository<UserBadge, ApplicationDbContext>>();

        _badgeService = new BadgeService(
            _context,
            _badgeRepoMock.Object,
            _userBadgeRepoMock.Object,
            _userRepoMock.Object
        );

        await _context.Database.EnsureCreatedAsync();

        // Store the Guid to the private testBadgeId field
        _testBadgeId = Guid.NewGuid();
        //_context.Set<Badge>().Add(testBadge);

        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task AddBadge_ToValidUser_CallsReposAndUpdatesBadgesAndPoints()
    {
        // Arrange
        // User's point count defaults to zero on initialization.
        var user = new ApplicationUser{ Id = "testId", Points = 0 };

        var badgeTemplate = new Badge{BadgeID = _testBadgeId, PointValue = 15, Title = "Test Badge"};

        _badgeRepoMock.Setup(r => r.FindByIdAsync(_testBadgeId))
                  .ReturnsAsync(badgeTemplate);

        // Act
        await _badgeService.AddBadge(user, _testBadgeId);

        // Assert
        // Check that the Repos were called
        _badgeRepoMock.Verify(r => r.FindByIdAsync(_testBadgeId), Times.Once);

        // Check That UserBadge Id and UserId mock calls match with input values
        _userBadgeRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<UserBadge>(ub => 
        ub.BadgeId == _testBadgeId && ub.UserId == user.Id)), Times.Once);

        // Checks if UserBadge directory was called to save a new object (UserBadge)
        _userBadgeRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<UserBadge>(ub => 
        ub.UserId == user.Id && 
        ub.BadgeId == _testBadgeId)), Times.Once);

        // Test badge is worth 15 points, check to see if point increment is the same
        _userRepoMock.Verify(r => r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => u.Points == 15)), Times.Once);

    }

    [Test]
    public async Task GetBadgeDetails_BadgeExists_ReturnsBadgeDetails()
    {
        // Arrange

        var testBadge = new Badge {
            BadgeID = _testBadgeId,
            Title = "Custom Profile Icon",
            PointValue = 10
        };

        // Set up the Mock to respond properly when called
        _badgeRepoMock.Setup(repo => repo.FindByIdAsync(_testBadgeId))
                  .ReturnsAsync(testBadge);
        
        // Act
        var searchBadge = await _badgeService.GetBadgeDetails(_testBadgeId);

        // Assert
        Assert.Multiple(() =>
        {
            // Check that the method found something...
            Assert.That(searchBadge, Is.Not.Null);

            // Then check that all the initialized details match.
            Assert.That(searchBadge!.BadgeID, Is.EqualTo(_testBadgeId));
            Assert.That(searchBadge!.Title, Is.EqualTo("Custom Profile Icon"));
            Assert.That(searchBadge!.PointValue, Is.EqualTo(10));
            _badgeRepoMock.Verify(r => r.FindByIdAsync(_testBadgeId), Times.Once);
        });
    }

    [Test]
    public async Task GetBadgeDetails_BadgeNotFound_ReturnsNull()
    {
        // Arrange
        Guid fakeId = Guid.NewGuid();

        // Guarantee that the Mock will return a null value for searching fakeId.
        _badgeRepoMock.Setup(r => r.FindByIdAsync(fakeId))
                    .ReturnsAsync((Badge?)null);

        // Act
        var searchBadge = await _badgeService.GetBadgeDetails(fakeId);

        // Assert
        Assert.That(searchBadge, Is.Null);
    }

    [Test]
    public async Task SortBadgesByTime_ValidBadgeList_ReturnsUserBadgeListDescending()
    {
        // Arrange
        // Add DateTime values to a UserBadge List.
        var oldTime = new DateTime(2001, 1, 1);
        var newTime = DateTime.UtcNow;

        var badgeList = new List<UserBadge>
        {
            new UserBadge { BadgeEarned = oldTime, UserBadgeId = Guid.NewGuid() },
            new UserBadge { BadgeEarned = newTime, UserBadgeId = Guid.NewGuid() }
        };

        // Act
        var result = await _badgeService.SortBadgesByTime(badgeList);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(2));

            // First item should be newest date.
            Assert.That(result[0].BadgeEarned, Is.EqualTo(newTime));
            Assert.That(result[1].BadgeEarned, Is.EqualTo(oldTime));
        });
    }

    [Test]
    public async Task EnsureStandardBadgesCreated_SeedsMissingBadges()
    {
        // Arrange
        // Guarantee that the mock will return null when called
        // Simulates empty DB
        _badgeRepoMock.Setup(r => r.FindByIdAsync(It.IsAny<Guid>()))
                    .ReturnsAsync((Badge?)null);

        // Act
        await _badgeService.EnsureStandardBadgesCreated();

        // Assert: Verify AddAsync was called 3 times (for Profile, Bio, and Sighting badges)
        _badgeRepoMock.Verify(r => r.AddOrUpdateAsync(It.IsAny<Badge>()), Times.Exactly(3));
    }
}