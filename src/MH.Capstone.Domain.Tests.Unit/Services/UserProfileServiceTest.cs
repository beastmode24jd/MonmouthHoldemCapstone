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
public class UserProfileServiceTests
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
        // Register repositories like we do in the actual application
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        // Register real AuthenticationService here when we create it
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

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

    [Test]
    public async Task setBio_ValidInput_SavesTextBio()
    {   
        // Arrange
        var profileService = _serviceProvider.GetRequiredService<IUserProfileService>();
        var user = new ApplicationUser
        {
            Email = "email@123.com",
            UserName = "johnDoe",
        };

        string bio = "I am John who works job at place";

        // Act
        await profileService.UpdateUserBio(user, bio);

        // Assert
        Assert.That(user.Bio, Is.EqualTo("I am John who works job at place"));
    }

    // Bio of more than 250 char creates invalid object
    [Test]
    public async Task UpdateUserBio_Over250CharInput_Rejected()
    {
        // Arrange
        var profileService = _serviceProvider.GetRequiredService<IUserProfileService>();
        var user = new ApplicationUser();

        // String over 250 char (this is 251)
        string longBio = new string ('Y', 251);

        // Act
        await profileService.UpdateUserBio(user, longBio);

        // Assert
        Assert.That(user.Bio, Is.EqualTo("Enter a unique profile bio."));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("             ")]
    public async Task setBio_InvalidInput_UsesDefaultValue(string? invalidInput)
    {
        // Arrange
        var profileService = _serviceProvider.GetRequiredService<IUserProfileService>();
        var user = new ApplicationUser();

        // Act
        await profileService.UpdateUserBio(user, invalidInput);

        Assert.That(user.Bio, Is.EqualTo("Enter a unique profile bio."));
    }
    

    /* Everything else I can think of is more granular integration testing,
        please reference "userBioTests.cs" with Gherkin scenarios from Jira for more */

    [Test]
    public async Task UpdateUserBio_ValidInput_SavesTextBioToDB()
    {   
        // Arrange
        var profileService = _serviceProvider.GetRequiredService<IUserProfileService>();
        var user = new ApplicationUser
        {
            Email = "email@123.com",
            UserName = "johnDoe",
            Bio = "Enter a unique profile bio."

        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        string updatedBio = "I am John who works job at place";
        await profileService.UpdateUserBio(user, updatedBio);

        // Assert
        // Fetch from _context to assert that it has been saved to the DB
        var userFromDb = await _context.Users.FindAsync(user.Id);

        // Check that the user is in the DB, before comparing userFromDb.Bio to updatedBio
        // Stops the test and displays the error message if not found
        Assert.That(userFromDb, Is.Not.Null, "User was not found in the database after update.");
        Assert.That(userFromDb.Bio, Is.EqualTo(updatedBio));
    }
    
}