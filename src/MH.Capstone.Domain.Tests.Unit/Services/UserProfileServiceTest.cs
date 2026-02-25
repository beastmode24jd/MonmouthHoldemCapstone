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

    /* Writing unit test ideas for userBio upload */

    [Test]
    public async Task setBio_ValidInput_SavesTextBio()
    {   
        // Arrange and Act
        var user = new ApplicationUser();
        user.Email = "email@123.com";
        user.Bio = "I am John who works job at place";

        // Assert
        Assert.That(user.Bio, Is.EqualTo("I am John who works job at place"));
    }

    // Bio of more than 250 char creates invalid object
    

    /* Everything else I can think of is more granular integration testing,
        please reference "userBioTests.cs" with Reqnroll for more */

    
}