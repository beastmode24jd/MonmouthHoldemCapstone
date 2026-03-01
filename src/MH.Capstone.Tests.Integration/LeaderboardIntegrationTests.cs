using MH.Capstone.Domain.DataAccess;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MH.Capstone.Tests.Integration;

[TestFixture]
[ExcludeFromCodeCoverage]
public class LeaderboardIntegrationTests : IDisposable
{
    // WebApplicationFactory spins up the real app (Program.cs, all DI, all middleware) 
    // but allows to replace the real SQL Server DbContext with an InMemory one for testing.
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove ALL DbContext-related registrations to prevent EF Core
                    // from seeing both SQL Server and InMemory providers simultaneously.
                    // IDbContextOptionsConfiguration<T> must also be removed: EF Core 9 stores
                    // the AddDbContext lambda there, and it will re-apply UseSqlServer on top of
                    // the new InMemory registration if left behind.
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                 || d.ServiceType == typeof(DbContextOptions)
                                 || d.ServiceType == typeof(ApplicationDbContext)
                                 || d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))
                        .ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    // Register a clean InMemory database with lazy loading to match Program.cs.
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseLazyLoadingProxies()
                               .UseInMemoryDatabase("LeaderboardIntegrationTestDb"));
                });
            });

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

    [Test]
    public async Task GET_Leaderboard_ReturnsOk()
    {
        // Act 
        var response = await _client.GetAsync("/Leaderboard"); // HTTP GET to /Leaderboard as an anonymous visitor.

        // Assert  
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK)); // page must be publicly accessible, no redirect to login.
    }

    [Test]
    public async Task GET_Leaderboard_ResponseContainsLeaderboardHeading()
    {
        // Act
        var response = await _client.GetAsync("/Leaderboard");
        var html = await response.Content.ReadAsStringAsync();

        // Assert 
        Assert.That(html, Does.Contain("Leaderboard")); // the page must contain the leaderboard table heading.
    }
}
