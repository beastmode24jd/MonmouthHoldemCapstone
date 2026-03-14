using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

namespace MH.Capstone.Tests.Integration;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingsGalleryIntegrationTests : IDisposable
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    // Set up the test environment: spin up the app with an in-memory database and create an HTTP client.  
    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove existing DbContext registrations to avoid conflicts.
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                 || d.ServiceType == typeof(DbContextOptions)
                                 || d.ServiceType == typeof(ApplicationDbContext))
                        .ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    // Register a new in memory database for testing.
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseLazyLoadingProxies()
                               .UseInMemoryDatabase("SightingsGalleryIntegrationTestDb"));
                });
            });

        // Create HTTP client for making requests to the test server.
        _client = _factory.CreateClient();
    }

   
    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }


    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    
    // Test that the gallery endpoint returns HTTP 200 OK.
    [Test]
    public async Task GET_Gallery_ReturnsOk()
    {
        var response = await _client.GetAsync("/Sightings/Gallery");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    // Test that the gallery page contains the expected heading.
    [Test]
    public async Task GET_Gallery_ResponseContainsGalleryHeading()
    {
        var response = await _client.GetAsync("/Sightings/Gallery");
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("My Sighting Gallery"));
    }

    
    // Test that the gallery page renders sightings for a specific user.
    [Test]
    public async Task GET_Gallery_UserWithSightings_RendersSightings()
    {
        // Arrange: Seed sightings for a user
        var userId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Sightings.AddRange(new[]
            {
                new Sighting { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-1), Description = "Test Sighting 1", ImageBuffer = Encoding.UTF8.GetBytes("img1") },
                new Sighting { Id = Guid.NewGuid(), UserId = userId, Timestamp = DateTime.UtcNow.AddDays(-2), Description = "Test Sighting 2", ImageBuffer = Encoding.UTF8.GetBytes("img2") }
            });
            db.SaveChanges();
        }

        // Act: Make a GET request to the gallery endpoint for the user
        var response = await _client.GetAsync($"/Sightings/Gallery?userId={userId}");
        var html = await response.Content.ReadAsStringAsync();

        // Assert: Check that the response is OK and sightings are rendered
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(html, Does.Contain("Test Sighting 1"));
        Assert.That(html, Does.Contain("Test Sighting 2"));
    }
}
