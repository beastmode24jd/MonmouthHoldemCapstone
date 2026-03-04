using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
public class AuthenticationServiceTests
{
    private ServiceProvider _serviceProvider;
    private ApplicationDbContext _context;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;
    private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private Mock<INotificationService> _notificationServiceMock;
    private IAuthenticationService _authService; // Do not mock this one
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;

    [SetUp]
    public async Task Setup()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection();

        // Add logging (required by Identity)
        services.AddLogging();

        services.AddSingleton<IConfiguration>();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .UseSeeding((context, _) => {
                if (context is ApplicationDbContext appSyncContext)
                    {
                        ApplicationDbContextSeeding.SeedDataAsync(appSyncContext, _, CancellationToken.None).GetAwaiter().GetResult();
                    }
                })
                // This is will be the perfered call by any part of EF Core that can support Async calls.
                .UseAsyncSeeding(async (context, _, token) =>
                {
                    if (context is ApplicationDbContext appAsyncContext)
                    {
                        await ApplicationDbContextSeeding.SeedDataAsync(appAsyncContext, _, token);
                    }
                })
            );

        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Register repositories like we do in the actual application
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        // Register real AuthenticationService here when we create it
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Why weren't you mocked *********************

        // Mock the store for _userManagerMock
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // 3. Initialize SignInManager Mock (requires UserManager, IHttpContextAccessor, and ClaimsFactory)
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>().Object,
            null!, null!, null!, null!);

        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();

        // *************************
        
        _notificationServiceMock = new Mock<INotificationService>();

        // Do not mock
        _authService = new AuthenticationService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _notificationServiceMock.Object,
            NullLogger<AuthenticationService>.Instance,
            _userRepoMock.Object
        );

        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        _userManagerMock.Object?.Dispose();
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    private void AssertAllMockVerifySetups()
    {
        _notificationServiceMock.VerifyAll();
        _notificationServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task RegisterUserAsync_WithValidData_CallsUserManagerAndSendsNotif()
    {
        // ARRANGE - Set up our test data ******************
        string email = "newuser@example.com";
        string password = "Test@123!";

        // Set up user manager to return Success with CreateAsync call
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), password))
        .ReturnsAsync(IdentityResult.Success);

        // Set up role assignment for user manager mock
        _userManagerMock.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
        .ReturnsAsync(IdentityResult.Success);

        _notificationServiceMock.Setup(s => s.SendNotificationAsync(
            It.IsAny<Notification>())).Verifiable(Times.Once);

        // ACT - Try to register the user ********************
        var result = await _authService.RegisterUserAsync(email, password);

        // ASSERT - Verify user was created ******************
        Assert.That(result, Is.True, "Registration should succeed");

        // Verify user manager was called with the correct email (we check users with email)
        _userManagerMock.Verify(x => x.CreateAsync(
            It.Is<ApplicationUser>(u => u.Email == email), password), Times.Once);

        // Verify notification was sent
        _notificationServiceMock.Verify();
    }


    // Write tests for ValidateCredentials 
    [Test]
    public async Task ValidateCredentialsAsync_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange - First create a user
        string email = "testuser@example.com";
        string password = "Test@123!";
        await _authService!.RegisterUserAsync(email, password);

        // Act - Try to validate with correct credentials
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert - Should return true
        Assert.That(result, Is.True, "Valid credentials should return true");
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange - Create a user
        string email = "testuser2@example.com";
        string correctPassword = "Test@123!";
        string wrongPassword = "WrongPass@123!";
        await _authService!.RegisterUserAsync(email, correctPassword);

        // Act - Try to validate with wrong password
        var result = await _authService.ValidateCredentialsAsync(email, wrongPassword);

        // Assert - Should return false
        Assert.That(result, Is.False, "Invalid password should return false");
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        string email = "nonexistent@example.com";
        string password = "Test@123!";

        // Act - Try to validate non-existent user
        var result = await _authService!.ValidateCredentialsAsync(email, password);

        // Assert - Should return false
        Assert.That(result, Is.False, "Non-existent user should return false");
    }

    //  ResetPasswordAsync Tests 

    [Test]
    public async Task ResetPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        string email = "nonexistent@example.com";
        string newPassword = "NewPass@456!";

        // Act - Try to reset password for non-existent user
        var result = await _authService!.ResetPasswordAsync(email, newPassword);

        // Assert - Should return false
        Assert.That(result, Is.False, "Reset should fail for non-existent user");
    }

    [Test]
    public void ResetPasswordAsync_WithInvalidPassword_ThrowsArgumentException()
    {
        // Arrange - Create a user first
        string email = "resetuser4@example.com";
        string oldPassword = "OldPass@123!";
        string invalidPassword = "weak"; // Too short, no symbol, no digit

        // Act & Assert - Should throw ArgumentException
        Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await _authService!.RegisterUserAsync(email, oldPassword);
            await _authService.ResetPasswordAsync(email, invalidPassword);
        });
    }

    // ==================== IsPasswordValid Tests ====================

    [Test]
    public void IsPasswordValid_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        string password = "Test@123!";

        // Act
        var result = _authService!.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.True, "Valid password should return true");
    }

    [Test]
    public void IsPasswordValid_WithNoSymbol_ReturnsFalse()
    {
        // Arrange
        string password = "Test12345";

        // Act
        var result = _authService!.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password without symbol should return false");
    }

    [Test]
    public void IsPasswordValid_WithNoDigit_ReturnsFalse()
    {
        // Arrange
        string password = "Test@abcd";

        // Act
        var result = _authService!.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password without digit should return false");
    }

    [Test]
    public void IsPasswordValid_WithNoLetter_ReturnsFalse()
    {
        // Arrange
        string password = "1234@567!";

        // Act
        var result = _authService!.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password without letter should return false");
    }

    [Test]
    public void IsPasswordValid_WithTooShort_ReturnsFalse()
    {
        // Arrange
        string password = "Te@1";

        // Act
        var result = _authService!.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password shorter than 8 chars should return false");
    }

    [Test]
    public void IsPasswordValid_WithNull_ReturnsFalse()
    {
        // Act
        var result = _authService!.IsPasswordValid(null!);

        // Assert
        Assert.That(result, Is.False, "Null password should return false");
    }

    [Test]
    public void IsPasswordValid_WithEmpty_ReturnsFalse()
    {
        // Act
        var result = _authService!.IsPasswordValid("");

        // Assert
        Assert.That(result, Is.False, "Empty password should return false");
    }

    [Test]
    public void IsPasswordValid_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = _authService!.IsPasswordValid("   ");

        // Assert
        Assert.That(result, Is.False, "Whitespace-only password should return false");
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
// == ReactivateAccountAsync Tests ==

    [Test]
    public async Task ReactivateAccountAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange - register and deactivate a user
        await _authService.RegisterUserAsync("reactivate@example.com", "Test@123!");
        await _authService.DeactivateAccountAsync("reactivate@example.com", "Test@123!");

        // Act
        var result = await _authService.ReactivateAccountAsync("reactivate@example.com", "Test@123!");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithValidCredentials_SetsIsDeactivatedToFalse()
    {
        // Arrange
        await _authService.RegisterUserAsync("reactivate2@example.com", "Test@123!");
        await _authService.DeactivateAccountAsync("reactivate2@example.com", "Test@123!");

        // Act
        await _authService.ReactivateAccountAsync("reactivate2@example.com", "Test@123!");

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "reactivate2@example.com");
        Assert.That(user!.IsDeactivated, Is.False);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        await _authService.RegisterUserAsync("reactivate3@example.com", "Test@123!");
        await _authService.DeactivateAccountAsync("reactivate3@example.com", "Test@123!");

        // Act
        var result = await _authService.ReactivateAccountAsync("reactivate3@example.com", "WrongPassword!");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithWrongPassword_AccountRemainsDeactivated()
    {
        // Arrange
        await _authService.RegisterUserAsync("reactivate4@example.com", "Test@123!");
        await _authService.DeactivateAccountAsync("reactivate4@example.com", "Test@123!");

        // Act
        await _authService.ReactivateAccountAsync("reactivate4@example.com", "WrongPassword!");

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "reactivate4@example.com");
        Assert.That(user!.IsDeactivated, Is.True);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Act
        var result = await _authService.ReactivateAccountAsync("nonexistent@example.com", "Test@123!");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithActiveAccount_ReturnsFalse()
    {
        // Arrange - register but don't deactivate
        await _authService.RegisterUserAsync("active@example.com", "Test@123!");

        // Act
        var result = await _authService.ReactivateAccountAsync("active@example.com", "Test@123!");

        // Assert
        Assert.That(result, Is.False, "Should not be able to reactivate an already active account");
    }

    [Test]
    public async Task ValidateCredentialsAsync_AfterReactivation_ReturnsTrue()
    {
        // Arrange - register, deactivate, then reactivate
        await _authService.RegisterUserAsync("reactivate5@example.com", "Test@123!");
        await _authService.DeactivateAccountAsync("reactivate5@example.com", "Test@123!");
        await _authService.ReactivateAccountAsync("reactivate5@example.com", "Test@123!");

        // Act
        var result = await _authService.ValidateCredentialsAsync("reactivate5@example.com", "Test@123!");

        // Assert
        Assert.That(result, Is.True, "Should be able to login after reactivation");
    }
}