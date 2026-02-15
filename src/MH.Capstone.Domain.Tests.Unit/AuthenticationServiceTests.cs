using MH.Capstone.WebApp.Services;
using Microsoft.Extensions.Logging;
using Moq;
using MH.Capstone.Domain.DataAccess.Contexts;
using MH.Capstone.Domain.DataModels;
using Microsoft.EntityFrameworkCore;


namespace MH.Capstone.Domain.Tests.Unit;

[TestFixture]
public class AuthenticationServiceTests
{
    private ApplicationDbContext _context;
    private Mock<ILogger<EfAuthenticationService>> _mockLogger;
    private EfAuthenticationService _authService;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockLogger = new Mock<ILogger<EfAuthenticationService>>();
        _authService = new EfAuthenticationService(_context, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
    }

    // ==================== IsPasswordValid Tests ====================

    [Test]
    public void IsPasswordValid_WithValidPassword_ReturnsTrue()
    {
        var result = _authService.IsPasswordValid("Test@123");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsPasswordValid_WithNoSymbol_ReturnsFalse()
    {
        var result = _authService.IsPasswordValid("Test1234");
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsPasswordValid_WithNoDigit_ReturnsFalse()
    {
        var result = _authService.IsPasswordValid("Test@abc");
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsPasswordValid_WithNoLetter_ReturnsFalse()
    {
        var result = _authService.IsPasswordValid("1234@567");
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsPasswordValid_WithTooShort_ReturnsFalse()
    {
        var result = _authService.IsPasswordValid("Te@1");
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsPasswordValid_WithNull_ReturnsFalse()
    {
        var result = _authService.IsPasswordValid(null!);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsPasswordValid_WithEmpty_ReturnsFalse()
    {
        var result = _authService.IsPasswordValid("");
        Assert.That(result, Is.False);
    }

    // ==================== DeactivateAccountAsync Tests ====================

    [Test]
    public async Task DeactivateAccountAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange - register a user first
        await _authService.RegisterUserAsync("test@example.com", "Test@123");

        // Act
        var result = await _authService.DeactivateAccountAsync("test@example.com", "Test@123");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task DeactivateAccountAsync_WithValidCredentials_SetsIsDeactivatedFlag()
    {
        // Arrange
        await _authService.RegisterUserAsync("test@example.com", "Test@123");

        // Act
        await _authService.DeactivateAccountAsync("test@example.com", "Test@123");

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.That(user!.IsDeactivated, Is.True);
    }

    [Test]
    public async Task DeactivateAccountAsync_WithValidCredentials_SetsDisplayNameToDeactivated()
    {
        // Arrange
        await _authService.RegisterUserAsync("test@example.com", "Test@123");

        // Act
        await _authService.DeactivateAccountAsync("test@example.com", "Test@123");

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.That(user!.DisplayName, Is.EqualTo("Deactivated User"));
    }

    [Test]
    public async Task DeactivateAccountAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        await _authService.RegisterUserAsync("test@example.com", "Test@123");

        // Act
        var result = await _authService.DeactivateAccountAsync("test@example.com", "WrongPassword!");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeactivateAccountAsync_WithWrongPassword_DoesNotDeactivate()
    {
        // Arrange
        await _authService.RegisterUserAsync("test@example.com", "Test@123");

        // Act
        await _authService.DeactivateAccountAsync("test@example.com", "WrongPassword!");

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.That(user!.IsDeactivated, Is.False);
    }

    [Test]
    public async Task DeactivateAccountAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Act
        var result = await _authService.DeactivateAccountAsync("nonexistent@example.com", "Test@123");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithDeactivatedAccount_ReturnsFalse()
    {
        // Arrange
        await _authService.RegisterUserAsync("test@example.com", "Test@123");
        await _authService.DeactivateAccountAsync("test@example.com", "Test@123");

        // Act
        var result = await _authService.ValidateCredentialsAsync("test@example.com", "Test@123");

        // Assert
        Assert.That(result, Is.False);
    }
}
