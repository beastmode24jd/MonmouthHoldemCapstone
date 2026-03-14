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
public class ReportIntegrationTests : IDisposable
{
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
                    var descriptors = services
                        .Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                                 || d.ServiceType == typeof(DbContextOptions)
                                 || d.ServiceType == typeof(ApplicationDbContext)
                                 || d.ServiceType == typeof(IDbContextOptionsConfiguration<ApplicationDbContext>))
                        .ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseLazyLoadingProxies()
                               .UseInMemoryDatabase("ReportIntegrationTestDb"));
                });
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false // to inspect the redirect response directly
        });
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
    public async Task POST_Report_Submit_Unauthenticated_RedirectsToLogin()
    {
        // Arrange
        var formData = new Dictionary<string, string>
        {
            { "ReportedPageUrl", "/Sighting/123" },
            { "Reason", "Inappropriate content" },
            { "Description", "" }
        };

        // Act
        var response = await _client.PostAsync("/Report/Submit",
            new FormUrlEncodedContent(formData));

        // Assert (unauthenticated users must be redirected to login)
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.ToString(), Does.Contain("/Account/Login"));
    }
}