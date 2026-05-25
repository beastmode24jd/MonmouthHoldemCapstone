using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.WebApp.Models;

namespace MH.Capstone.WebApp.Tests.Unit.ViewModels;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingDetailsViewModelTests
{
    private static Sighting MakeSighting() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Description = "desc",
        SpeciesName = "Coyote",
        Timestamp = DateTimeOffset.UtcNow.AddDays(-1),
        ImageBuffer = [0x01]
    };

    [Test]
    public void CanEdit_DefaultsToFalse()
    {
        // CSP-37: the edit button is hidden unless the controller explicitly grants ownership,
        // so a freshly-built details VM must never expose the button by default.
        var vm = new SightingDetailsViewModel(MakeSighting(), funFact: "Fun!");

        Assert.That(vm.CanEdit, Is.False);
    }
}
