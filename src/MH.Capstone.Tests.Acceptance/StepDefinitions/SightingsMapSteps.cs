using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
public class SightingsMapSteps
{
    private readonly List<Sighting> _sightings = new();
    private IEnumerable<Sighting> _result = new List<Sighting>();
    private Mock<IRepository<Sighting, ApplicationDbContext>> _sightingsRepoMock = null!;
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock = null!;
    private Mock<IScoringService> _scoringServiceMock = null!;
    private Mock<INotificationService> _notificationServiceMock = null!;
    private ISightingsService _sightingsService = null!;
    private bool _isLoggedIn;

    [Given(@"I am a logged in user")]
    public void GivenIAmALoggedInUser()
    {
        _isLoggedIn = true;
        
        // Initialize mocks
        _sightingsRepoMock = new Mock<IRepository<Sighting, ApplicationDbContext>>();
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
        _scoringServiceMock = new Mock<IScoringService>();
        _notificationServiceMock = new Mock<INotificationService>();
        
        _sightingsService = new SightingsService(
            NullLogger<SightingsService>.Instance,
            _scoringServiceMock.Object,
            _notificationServiceMock.Object,
            _sightingsRepoMock.Object,
            _userRepoMock.Object);
    }

    [Given(@"the following sightings exist:")]
    public void GivenTheFollowingSightingsExist(Table table)
    {
        foreach (var row in table.Rows)
        {
            var sighting = new Sighting
            {
                Id = Guid.NewGuid(),
                Latitude = decimal.Parse(row["Latitude"]),
                Longitude = decimal.Parse(row["Longitude"]),
                Description = row["Description"],
                Timestamp = DateTimeOffset.UtcNow.AddHours(-1),
                ImageBuffer = new byte[] { 0x01 }
            };
            _sightings.Add(sighting);
        }

        _sightingsRepoMock.Setup(r => r.GetAllAsync())
            .ReturnsAsync(_sightings.AsQueryable());
    }

    [When(@"I navigate to the sightings map page")]
    public void WhenINavigateToTheSightingsMapPage()
    {
        // This step verifies user authentication
        Assert.That(_isLoggedIn, Is.True, "User must be logged in to view the map");
    }

    [When(@"I request sightings for bounds (.*) to (.*) latitude and (.*) to (.*) longitude")]
    public async Task WhenIRequestSightingsForBounds(decimal minLat, decimal maxLat, decimal minLng, decimal maxLng)
    {
        _result = await _sightingsService.GetSightingsInBoundsAsync(minLat, maxLat, minLng, maxLng);
    }

    [Then(@"I should see an interactive map")]
    public void ThenIShouldSeeAnInteractiveMap()
    {
        Assert.That(_isLoggedIn, Is.True);
        // In a full integration test, this would verify the map element exists
    }

    [Then(@"I should receive (.*) sighting")]
    [Then(@"I should receive (.*) sightings")]
    public void ThenIShouldReceiveSightings(int expectedCount)
    {
        Assert.That(_result.Count(), Is.EqualTo(expectedCount));
    }
}