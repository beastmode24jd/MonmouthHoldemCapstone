using System.ComponentModel.DataAnnotations;
using MH.Capstone.WebApp.Models.ViewModels;

namespace MH.Capstone.WebApp.Tests.Unit.ViewModels;

[TestFixture]
public class LoginViewModelTests
{
    private List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    [Test]
    public void LoginViewModel_WithValidData_ShouldPassValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Test@123",
            RememberMe = false
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Empty);
    }

    [Test]
    public void LoginViewModel_WithEmptyEmail_ShouldFailValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "",
            Password = "Test@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Email")), Is.True);
    }

    [Test]
    public void LoginViewModel_WithInvalidEmailFormat_ShouldFailValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "notanemail",
            Password = "Test@123"
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Email")), Is.True);
    }

    [Test]
    public void LoginViewModel_WithEmptyPassword_ShouldFailValidation()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = ""
        };

        // Act
        var validationResults = ValidateModel(model);

        // Assert
        Assert.That(validationResults, Is.Not.Empty);
        Assert.That(validationResults.Any(v => v.MemberNames.Contains("Password")), Is.True);
    }

    [Test]
    public void LoginViewModel_RememberMe_DefaultsToFalse()
    {
        // Arrange & Act
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Test@123"
        };

        // Assert
        Assert.That(model.RememberMe, Is.False);
    }

    [Test]
    public void LoginViewModel_ReturnUrl_CanBeSet()
    {
        // Arrange
        var model = new LoginViewModel
        {
            Email = "test@example.com",
            Password = "Test@123",
            ReturnUrl = "/Dashboard"
        };

        // Act & Assert
        Assert.That(model.ReturnUrl, Is.EqualTo("/Dashboard"));
    }
}
