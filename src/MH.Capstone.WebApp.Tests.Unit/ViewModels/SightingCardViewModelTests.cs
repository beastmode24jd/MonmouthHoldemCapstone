using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.WebApp.Models;

namespace MH.Capstone.WebApp.Tests.Unit.ViewModels;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingCardViewModelTests
{
    private static Sighting MakeSighting(ApplicationUser? user = null)
    {
        return new Sighting
        {
            Id = Guid.NewGuid(),
            UserIdentityId = user?.Id ?? "user-1",
            User = user!,
            ImageBuffer = new byte[] { 0x01 },
            Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
        };
    }

    [Test]
    public void Attribution_WhenUserHasDisplayName_ShowsDisplayName()
    {
        var user = new ApplicationUser { Id = "u1", DisplayName = "Alice", Email = "alice@test.com", UserName = "alice@test.com" };
        var vm = new SightingCardViewModel(MakeSighting(user));

        Assert.That(vm.SubmittedByUsername, Is.EqualTo("Alice"));
    }

    [Test]
    public void Attribution_WhenUserHasDisplayName_DoesNotContainAtSymbol()
    {
        var user = new ApplicationUser { Id = "u1", DisplayName = "Alice", Email = "alice@test.com", UserName = "alice@test.com" };
        var vm = new SightingCardViewModel(MakeSighting(user));

        Assert.That(vm.SubmittedByUsername, Does.Not.Contain("@"),
            "attribution must not expose the user's email address");
    }

    [Test]
    public void Attribution_WhenDisplayNameIsNull_DoesNotFallBackToUserName()
    {
        // CSP-168 guarantees DisplayName is set, but null is tested to confirm
        // we no longer use UserName as a fallback (which could expose email-format usernames)
        var user = new ApplicationUser { Id = "u1", DisplayName = null!, UserName = "alice@test.com", Email = "alice@test.com" };
        var vm = new SightingCardViewModel(MakeSighting(user));

        Assert.That(vm.SubmittedByUsername, Is.EqualTo("Unknown"),
            "when DisplayName is null, attribution should fall back to 'Unknown', not UserName");
    }

    [Test]
    public void Attribution_WhenUserIsNull_ReturnsUnknown()
    {
        var sighting = MakeSighting(user: null);
        var vm = new SightingCardViewModel(sighting);

        Assert.That(vm.SubmittedByUsername, Is.EqualTo("Unknown"));
    }
}
