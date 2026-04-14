using System.ComponentModel.DataAnnotations;
using MH.Capstone.Domain.Tools;

namespace MH.Capstone.Domain.Tests.Unit.Tools;

[TestFixture]
public class NotDefaultCoordinatesAttributeTests
{
    private class TestModel
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }

    [Test]
    public void IsValid_BothLatitudeAndLongitudeAreZero_ReturnsValidationError()
    {
        // Arrange
        var attribute = new NotDefaultCoordinatesAttribute();
        var model = new TestModel { Latitude = 0.0m, Longitude = 0.0m };
        var context = new ValidationContext(model);

        // Act
        var result = attribute.GetValidationResult(model, context);

        // Assert
        Assert.That(result, Is.Not.EqualTo(ValidationResult.Success));
        Assert.That(result!.ErrorMessage, Does.Contain("cannot both be 0"));
    }

    [Test]
    public void IsValid_LatitudeIsZeroLongitudeIsNot_ReturnsSuccess()
    {
        // Arrange
        var attribute = new NotDefaultCoordinatesAttribute();
        var model = new TestModel { Latitude = 0.0m, Longitude = -123.0351m };
        var context = new ValidationContext(model);

        // Act
        var result = attribute.GetValidationResult(model, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_LongitudeIsZeroLatitudeIsNot_ReturnsSuccess()
    {
        // Arrange
        var attribute = new NotDefaultCoordinatesAttribute();
        var model = new TestModel { Latitude = 44.9429m, Longitude = 0.0m };
        var context = new ValidationContext(model);

        // Act
        var result = attribute.GetValidationResult(model, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_BothLatitudeAndLongitudeAreNonZero_ReturnsSuccess()
    {
        // Arrange
        var attribute = new NotDefaultCoordinatesAttribute();
        var model = new TestModel { Latitude = 44.9429m, Longitude = -123.0351m };
        var context = new ValidationContext(model);

        // Act
        var result = attribute.GetValidationResult(model, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }

    [Test]
    public void IsValid_NullValue_ReturnsSuccess()
    {
        // Arrange
        var attribute = new NotDefaultCoordinatesAttribute();
        var context = new ValidationContext(new object());

        // Act
        var result = attribute.GetValidationResult(null, context);

        // Assert
        Assert.That(result, Is.EqualTo(ValidationResult.Success));
    }
}