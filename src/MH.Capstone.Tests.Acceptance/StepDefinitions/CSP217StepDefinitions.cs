// CSP-217: Sightings-map timestamps render in user's local timezone;
// also covers the world-bounds clamp on the map.
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentAssertions;
using MH.Capstone.Tests.Acceptance.Drivers;
using Reqnroll;

namespace MH.Capstone.Tests.Acceptance.StepDefinitions;

[Binding]
[Scope(Tag = "csp217")]
[ExcludeFromCodeCoverage]
public class CSP217StepDefinitions
{
    private readonly MapDriver _mapDriver;

    // Oregon-wide bounding box; covers all of Alex's seeded sightings.
    private const double MinLat = 41.0;
    private const double MaxLat = 47.0;
    private const double MinLng = -125.0;
    private const double MaxLng = -116.0;

    private readonly Dictionary<string, Dictionary<string, string>> _timestampsByZone = new();

    public CSP217StepDefinitions(MapDriver mapDriver)
    {
        _mapDriver = mapDriver;
    }

    [Given("Alex opens the sightings map")]
    public void GivenAlexOpensTheSightingsMap()
    {
        _mapDriver.NavigateToMap();
    }

    [When("Alex fetches sightings with UserTimeZone {string}")]
    public void WhenAlexFetchesSightingsWithUserTimeZone(string ianaId)
    {
        _mapDriver.SetUserTimeZoneCookie(ianaId);
        var raw = _mapDriver.FetchSightingsRaw(MinLat, MaxLat, MinLng, MaxLng);
        raw.Should().NotStartWith("ERROR:", "the fetch must succeed before we compare timestamps");

        var perSighting = new Dictionary<string, string>();
        using var doc = JsonDocument.Parse(raw);
        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var id = element.GetProperty("id").GetString()!;
            var ts = element.GetProperty("timestamp").GetString()!;
            perSighting[id] = ts;
        }

        perSighting.Should().NotBeEmpty(
            "Alex's seeded sightings are within the Oregon bounding box and should be returned");
        _timestampsByZone[ianaId] = perSighting;
    }

    [Then("the two timestamp sets should differ")]
    public void ThenTheTwoTimestampSetsShouldDiffer()
    {
        _timestampsByZone.Should().HaveCount(2, "the scenario fetches with two distinct UserTimeZone values");

        var first = _timestampsByZone.Values.ElementAt(0);
        var second = _timestampsByZone.Values.ElementAt(1);

        // Match on sighting id; the same UTC instant should format differently in two
        // different timezones unless the controller is ignoring the cookie.
        var sharedIds = first.Keys.Intersect(second.Keys).ToList();
        sharedIds.Should().NotBeEmpty("both fetches should return the same Oregon sightings");

        var anyDiffer = sharedIds.Any(id => first[id] != second[id]);
        anyDiffer.Should().BeTrue(
            "timestamps for the same sighting should differ between two distinct UserTimeZone cookies");
    }

    [When("Alex pans the map to longitude {int}")]
    public void WhenAlexPansTheMapToLongitude(int targetLng)
    {
        _mapDriver.PanTo(45.0, targetLng);
    }

    [Then("the map's center longitude should be between {int} and {int}")]
    public void ThenTheMapCenterLongitudeShouldBeBetween(int min, int max)
    {
        var lng = _mapDriver.GetCenterLongitude();
        lng.Should().BeGreaterThanOrEqualTo(min)
            .And.BeLessThanOrEqualTo(max,
                "the world-bounds clamp must prevent the map from drifting into duplicate world copies");
    }
}
