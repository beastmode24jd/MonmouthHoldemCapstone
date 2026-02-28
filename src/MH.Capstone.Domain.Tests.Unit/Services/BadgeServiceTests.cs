using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;
using System.Text;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
public class BadgeServiceTests
{
    private ServiceProvider _serviceProvider;
    private ApplicationDbContext _context;
    private IBadgeService _badgeService;
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

        //          Will need to implement in Program.cs later,
        //              as well as mocking in Dashboard Tests

        services.AddScoped<IBadgeService, BadgeService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        _badgeService = _serviceProvider.GetRequiredService<IBadgeService>();

        await _context.Database.EnsureCreatedAsync();

        // MOCK BADGE
        // Adds a dummy badge to local DB mock, so search feature can find a badge to add.
        var testBadge = new Badge
        { 
            BadgeID = Guid.NewGuid(),
            Title = "Custom Profile Icon", 
            PointValue = 10
        };

        // Store the Guid to the private testBadgeId field
        _testBadgeId = testBadge.BadgeID;
        _context.Set<Badge>().Add(testBadge);

        await _context.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    /*  Test ideas:
        - 3 badge types:
            - First Sighting Upload
            - Custom Bio Upload
            - Custom Profile Icon Upload

        - Badges should increment the points a user has, by about 10~15 per badge
        
        Front end ideas (ONLY IMPLEMENT AFTER NUNIT TESTS, EF, AND BACKEND IS GOOD)
        This is here so I don't accidently turn them into tests somehow
        - Custom display page for badges
        - Placeholder display "icon" jpg, for badges -- Actually, make this a test
        - Dashboard display of badges collected
            - If no badges, display placeholder text with hints
        - Display both badges in descending order of chronologic earning
    */

    [Test]
    public async Task AddBadge_ToValidUser_IncrementsUserPoints()
    {
        // Arrange
        // User's point count defaults to zero on initialization.
        var user = new ApplicationUser();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        await _badgeService.AddBadge(user, _testBadgeId);

        // Assert
        // Test badge is worth 10 points, check to see if point increment is the same
        Assert.That(user.Points, Is.EqualTo(10), "AddBadge() did not add 10 points to the user."); 
    }

    [Test]
    public async Task AddBadge_ToValidUser_IncrementsBadgeList()
    {
        // Arrange
        var user = new ApplicationUser();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        // Add the test badge to the user object.
        await _badgeService.AddBadge(user, _testBadgeId);

        // Assert
        Assert.That(user.UserBadges.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddBadge_ToValidUser_InitializesDefaultBadgeIcon()
    {
        // Arrange
        var user = new ApplicationUser();

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        byte[] defaultIconImg = Encoding.UTF8.GetBytes("~/imgs/BadgeIcon.jpg");

        // Act
        // Add the test badge to the user object.
        await _badgeService.AddBadge(user, _testBadgeId);

        // Assert
        // Check if BadgeIcon data matches the default badge icon in wwwroot imgs folder.
        Assert.Multiple(() =>
        {
            Assert.That(user.UserBadges.Count, Is.EqualTo(1));
            Assert.That(user.UserBadges[0].Badge.BadgeIcon, Is.EqualTo(defaultIconImg));
        });
    }

    [Test]
    public async Task GetBadgeDetails_BadgeExists_ReturnsBadgeDetails()
    {
        // Arrange
        // Custom Profile Badge mock is added to memory during Test Setup().

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
        });
    }

    [Test]
    public async Task GetBadgeDetails_BadgeNotFound_ReturnsNull()
    {
        // Arrange
        Guid fakeId = Guid.NewGuid();

        // I doubt that fakeId would be the same Guid as _testBadgeId, but this checks anyway
        if (fakeId == _testBadgeId)
        {
            fakeId = Guid.NewGuid();
        }

        // Act
        var searchBadge = await _badgeService.GetBadgeDetails(fakeId);

        // Assert
        Assert.That(searchBadge, Is.Null);
    }
}