using MH.Capstone.Domain.DataAccess.Contexts;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MH.Capstone.Tests.Integration;

[TestFixture]
public class AuthenticationServiceTests
{
    private ServiceProvider _serviceProvider;
    private AuthDbContext _context;
    private UserManager<ApplicationUser> _userManager;
    private SignInManager<ApplicationUser> _signInManager;
    private IAuthenticationService? _authService;
    [SetUp]
    public async Task Setup()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection();
        
        // Add logging (required by Identity)
        services.AddLogging();
        
        services.AddDbContext<AuthDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        
        // Add Identity services
        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();
        
        // Register real AuthenticationService here when we create it
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AuthDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _signInManager = _serviceProvider.GetRequiredService<SignInManager<ApplicationUser>>();
        _authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        _userManager?.Dispose();
        await _context.Database.EnsureDeletedAsync();
        await _context.DisposeAsync();
        await _serviceProvider.DisposeAsync();
    }

    [Test]
    public async Task RegisterUserAsync_WithValidData_CreatesUserInDatabase()
  {
        // Arrange - Set up our test data
        string email = "newuser@example.com";
        string password = "Test@123!";
        
        // Act - Try to register the user
        var result = await _authService.RegisterUserAsync(email, password);
        
        // Assert - Verify user was created
        Assert.That(result, Is.True, "Registration should succeed");
        
        var userInDb = await _userManager.FindByEmailAsync(email);
        Assert.That(userInDb, Is.Not.Null, "User should exist in database");
        Assert.That(userInDb.Email, Is.EqualTo(email), "Email should match");
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

    // Tests for UserExists Verification
    [Test]
    public async Task UserExists_WithRegisteredEmail_ReturnsTrue()
    {
        // Arrange - Create a user
        string email = "existinguser@example.com";
        string password = "Test@123!";
        await _authService!.RegisterUserAsync(email, password);

        // Act - Check if user exists
        var exists = await _authService.UserExistsAsync(email);

        // Assert - Should return true
        Assert.That(exists, Is.True, "Registered user should exist");
    }

    [Test]
    public async Task UserExists_WithUnregisteredEmail_ReturnsFalse()
    {
        // Arrange - Use an email that was never registered
        string email = "nonexistent@example.com";

        // Act - Check if user exists
        var exists = await _authService!.UserExistsAsync(email);

        // Assert - Should return false
        Assert.That(exists, Is.False, "Unregistered user should not exist");
    }
}