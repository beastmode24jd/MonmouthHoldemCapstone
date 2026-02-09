using System.ComponentModel.DataAnnotations;
using MH.Capstone.WebApp.Models.ViewModels;

namespace MH.Capstone.Domain.Tests.Unit;

[TestFixture]
public class RegisterViewModelTests
{
    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    [Test]
    public void RegisterViewModel_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Empty);
    }

    [Test]
    public void RegisterViewModel_WithEmptyEmail_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Email")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithInvalidEmailFormat_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "notanemail",
            Password = "Test@123",
            ConfirmPassword = "Test@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Email")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithWeakPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithoutUppercase_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "test@123",
            ConfirmPassword = "test@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithoutLowercase_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "TEST@123",
            ConfirmPassword = "TEST@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithoutDigit_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "Test@Pass",
            ConfirmPassword = "Test@Pass"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithoutSpecialCharacter_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "Test1234",
            ConfirmPassword = "Test1234"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithMismatchedPasswords_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "Test@123",
            ConfirmPassword = "Test@456"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("ConfirmPassword")), Is.True);
    }

    [Test]
    public void RegisterViewModel_WithShortPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new RegisterViewModel
        {
            Email = "test@example.com",
            Password = "Tst@12",
            ConfirmPassword = "Tst@12"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }
}
