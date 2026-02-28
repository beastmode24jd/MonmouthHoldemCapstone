using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
public class BadgeServiceTests
{
    private ServiceProvider _serviceProvider;
    private ApplicationDbContext _context;
    

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

        // Add IBadgeService and BadgeService here

        //          Will need to implement in Program.cs later,
        //              as well as mocking in Dashboard Tests

        services.AddScoped<IBadgeService, BadgeServiceTests>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        await _context.Database.EnsureCreatedAsync();
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

        - User data model needs list of badges (similar to list of sightings)
        - Badges should increment the points a user has, by about 10~15 per badge

        Badge data:
        - DateTime timestamp
        - GUID? (Badge ID)
            - User ID as well
        - Badge icon image (public byte[])
        - Point value (int)
        - BadgeType (string???)
        
        Front end ideas (ONLY IMPLEMENT AFTER NUNIT TESTS, EF, AND BACKEND IS GOOD)
        This is here so I don't accidently turn them into tests somehow
        - Custom display page for badges
        - Dashboard display of badges collected
            - If no badges, display placeholder text with hints
        - Display both badges in descending order of chronologic earning
    */

    [Test]
    public async Task AddBadge_ToValidUser_IncrementsUserPoints()
    {
        // Arrange
        var user = new ApplicationUser();
        var badge = new Badge();

        // Add to DB context
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        int badgeID = 1;
        int pointsBeforeBadge = user.Points;

        // Act
        await badgeService.AddBadge(badgeID);

        // Assert
        Assert.That(user.Points, Is.EqualTo(pointsBeforeBadge));
        
    }
}