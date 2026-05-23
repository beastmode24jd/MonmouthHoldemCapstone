using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.WebApp.Models;

namespace MH.Capstone.WebApp.Tests.Unit.ViewModels;

[TestFixture]
[ExcludeFromCodeCoverage]
public class SightingEditViewModelTests
{
    private static List<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model, null, null), results, true);
        return results;
    }

    private static Sighting MakeSighting() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Description = "A coyote trotting along the ridge",
        SpeciesName = "Coyote",
        Latitude = 44.5m,
        Longitude = -123.25m,
        Timestamp = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),
        ImageBuffer = [0x01]
    };

    [Test]
    public void Constructor_FromSighting_PrePopulatesEditableFields()
    {
        var sighting = MakeSighting();

        var vm = new SightingEditViewModel(sighting);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Id, Is.EqualTo(sighting.Id));
            Assert.That(vm.Description, Is.EqualTo("A coyote trotting along the ridge"));
            Assert.That(vm.SpeciesName, Is.EqualTo("Coyote"));
        });
    }

    [Test]
    public void Constructor_FromSighting_PopulatesReadOnlyContext()
    {
        var sighting = MakeSighting();

        var vm = new SightingEditViewModel(sighting);

        Assert.Multiple(() =>
        {
            Assert.That(vm.Latitude, Is.EqualTo(44.5m));
            Assert.That(vm.Longitude, Is.EqualTo(-123.25m));
            Assert.That(vm.Timestamp, Is.EqualTo(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));
            Assert.That(vm.ImageDataUrl, Does.StartWith("data:image"));
        });
    }

    [Test]
    public void Constructor_FromSightingWithNullDescription_YieldsEmptyString()
    {
        var sighting = MakeSighting();
        sighting.Description = null;

        var vm = new SightingEditViewModel(sighting);

        Assert.That(vm.Description, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ValidModel_PassesValidation()
    {
        var model = new SightingEditViewModel { Description = "Updated", SpeciesName = "Gray Wolf" };

        Assert.That(ValidateModel(model), Is.Empty);
    }

    [Test]
    public void EmptyDescription_FailsValidation()
    {
        var model = new SightingEditViewModel { Description = "", SpeciesName = "Gray Wolf" };

        var results = ValidateModel(model);

        Assert.That(results.Any(v => v.MemberNames.Contains(nameof(SightingEditViewModel.Description))), Is.True);
    }

    [Test]
    public void EmptySpeciesName_FailsValidation()
    {
        var model = new SightingEditViewModel { Description = "Updated", SpeciesName = "" };

        var results = ValidateModel(model);

        Assert.That(results.Any(v => v.MemberNames.Contains(nameof(SightingEditViewModel.SpeciesName))), Is.True);
    }

    [Test]
    public void DescriptionOver500Chars_FailsValidation()
    {
        var model = new SightingEditViewModel { Description = new string('x', 501), SpeciesName = "Gray Wolf" };

        var results = ValidateModel(model);

        Assert.That(results.Any(v => v.MemberNames.Contains(nameof(SightingEditViewModel.Description))), Is.True);
    }

    [Test]
    public void SpeciesNameOver100Chars_FailsValidation()
    {
        var model = new SightingEditViewModel { Description = "Updated", SpeciesName = new string('x', 101) };

        var results = ValidateModel(model);

        Assert.That(results.Any(v => v.MemberNames.Contains(nameof(SightingEditViewModel.SpeciesName))), Is.True);
    }
}
