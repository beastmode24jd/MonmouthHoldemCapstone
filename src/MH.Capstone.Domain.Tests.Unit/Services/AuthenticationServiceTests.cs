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
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
public class AuthenticationServiceTests
{
    private ServiceProvider _serviceProvider;
    private ApplicationDbContext _context;
    private Mock<UserManager<ApplicationUser>> _userManagerMock;

    private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private Mock<INotificationService> _notificationServiceMock;

    // MS already has an AuthenticationService, and the compiler needs to know
    // which one to use. So we bring out the full filepath here
    private Domain.Services.Abstraction.IAuthenticationService _authService;
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;

    [SetUp]
    public async Task Setup()
    {
        // Create in-memory database for testing
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());

        // Add logging (required by Identity)
        services.AddLogging();

        // Simplify the DbContext, since we aren't testing the database.
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Register repositories like we do in the actual application
        services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));

        // Register userManagerMock with null placeholders.
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Use Fake class from bottom of file to initialize _signInManagerMock object.
        var fakeSignInManager = new FakeSignInManager(_userManagerMock.Object);
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>()
        );

        _notificationServiceMock = new Mock<INotificationService>();
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();

        // Register AuthenticationService
        _authService = new MH.Capstone.Domain.Services.AuthenticationService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _notificationServiceMock.Object,
            NullLogger<MH.Capstone.Domain.Services.AuthenticationService>.Instance,
            _userRepoMock.Object
        );

        await _context.Database.EnsureCreatedAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        // Mocks get reset, they don't need to be in here
        // A failed Setup may set context and serviceProvider to null

        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }

        if (_serviceProvider != null)
        {
            await _serviceProvider.DisposeAsync();
        }
    }

    private void AssertAllMockVerifySetups()
    {
        _notificationServiceMock.VerifyAll();
        _notificationServiceMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task RegisterUserAsync_WithValidData_CreatesUserAndSendsNotif()
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

        var user = new ApplicationUser 
        { 
            Email = email, 
            UserName = email,
            IsDeactivated = false // Active account
        };

        // userManagerMock should return this account when it is searched for
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // signInManagerMock should return successful SignInResult when password is checked
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(SignInResult.Success);

        // Act - Try to validate with correct credentials
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Should return true
            Assert.That(result, Is.True, "Valid credentials should return true");
            
            // Make sure the service used its dependencies as expected
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, password, false), Times.Once);
        });
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange - Create a user
        string email = "testuser2@example.com";
        string wrongPassword = "WrongPass@123!";

        var user = new ApplicationUser 
        { 
            Email = email, 
            UserName = email,
            IsDeactivated = false // Active account
        };

        // _userManagerMock should return this user when searched by email
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Set up _signInManagerMock to return a failure with wrongPassword parameter
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act - Try to validate with wrong password
        var result = await _authService.ValidateCredentialsAsync(email, wrongPassword);

        // Assert - Should return false
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False, "Invalid password should return false");

            // Make sure authService used its dependencies as expected
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false), Times.Once);
            
        });
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        string email = "nonexistent@example.com";
        string password = "Test@123!";

        // _userManagerMock should NOT return this user when searched by email
        // Return null.
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync((ApplicationUser)null!);

        // Act - Try to validate non-existent user
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert - Should return false
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False, "Non-existent user should return false");

            // Check authService calls. signInManagerMock should never be called,
            //      should be caught by guard statements.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<string>(),
                It.IsAny<bool>()),
                Times.Never);
        });
    }

    //  ResetPasswordAsync Tests 

    [Test]
    public async Task ResetPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        string invalidEmail = "nonexistent@example.com";
        string newPassword = "NewPass@456!";

        // Set up userManagerMock to return null when called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(invalidEmail))
            .ReturnsAsync((ApplicationUser)null!);

        // Act - Try to reset password for non-existent user
        var result = await _authService.ResetPasswordAsync(invalidEmail, newPassword);

        // Assert - Should return false, and not try to generate a reset token.
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False, "Reset should fail for non-existent user");

            // Check that UserManagerMock user search was called once.
            _userManagerMock.Verify(um => um.FindByEmailAsync(invalidEmail), Times.Once);

            // Check that a reset token WAS NOT created for the invalid user.
            _userManagerMock.Verify(um => um.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
        });
    }

    [Test]
    public void ResetPasswordAsync_WithInvalidPassword_ThrowsArgumentException()
    {
        // Arrange - Create a user first
        string email = "resetuser4@example.com";
        string invalidPassword = "weak"; // Too short, no symbol, no digit

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            Id = Guid.NewGuid().ToString()
        };

        // Set up userManagerMock to return the user, if it is called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Act: throw the exception.
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
        {
            await _authService.ResetPasswordAsync(email, invalidPassword);
        });

        // Assert: check that exception was thrown, message is correct, and the
        //                   password passed in matches the invalidPassword given.
        Assert.Multiple(() =>
        {
            // Verify thrown exception message matches the message in
            //                              AuthenticationService.cs.
            Assert.That(ex.Message, Does.Contain("does not meet the policy standards"),
                "Exception message should match the exception message in AuthenticationService.cs.");

            // Verify that the invalid password argument is the argument throwing the exception.
            Assert.That(ex.ParamName, Is.EqualTo("newPassword"), 
            "The exception should identify 'newPassword' (locally defined as invalidPassword) as the invalid parameter.");
        });

        // Check that the defined user was never returned by the search,
        //      due to the invalid password being caught.
        _userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    // ==================== IsPasswordValid Tests ====================

    [Test]
    public void IsPasswordValid_WithValidPassword_ReturnsTrue()
    {
        // Arrange
        // Password needs to be >= 8 char, contain a number, uppercase letter,
        //  lowercase letter, and a symbol.
        string validPassword = "SafePassword@123";

        // Act
        var result = _authService.IsPasswordValid(validPassword);

        // Assert
        Assert.That(result, Is.True, "Valid password should return true");
    }

    [Test]
    public void IsPasswordValid_WithNoSymbol_ReturnsFalse()
    {
        // Arrange
        string password = "Test12345";

        // Act
        var result = _authService.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password without symbol should return false");
    }

    [Test]
    public void IsPasswordValid_WithNoDigit_ReturnsFalse()
    {
        // Arrange
        string password = "Test@abcd";

        // Act
        var result = _authService.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password without digit should return false");
    }

    [Test]
    public void IsPasswordValid_WithNoLetters_ReturnsFalse()
    {
        // Arrange
        string password = "1234@567!";

        // Act
        var result = _authService.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password without letter should return false");
    }

    [Test]
    public void IsPasswordValid_PasswordTooShort_ReturnsFalse()
    {
        // Arrange
        string password = "Te@1";

        // Act
        var result = _authService.IsPasswordValid(password);

        // Assert
        Assert.That(result, Is.False, "Password shorter than 8 chars should return false");
    }

    [Test]
    public void IsPasswordValid_WithNull_ReturnsFalse()
    {
        // Act
        var result = _authService.IsPasswordValid(null!);

        // Assert
        Assert.That(result, Is.False, "Null password should return false");
    }

    [Test]
    public void IsPasswordValid_WithEmpty_ReturnsFalse()
    {
        // Act
        var result = _authService.IsPasswordValid("");

        // Assert
        Assert.That(result, Is.False, "Empty password should return false");
    }

    [Test]
    public void IsPasswordValid_WithWhitespace_ReturnsFalse()
    {
        // Act
        var result = _authService.IsPasswordValid("   ");

        // Assert
        Assert.That(result, Is.False, "Whitespace-only password should return false");
    }

    // ==================== DeactivateAccountAsync Tests ====================

    [Test]
    public async Task DeactivateAccountAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange - register a user first
        string email = "test@example.com";
        string password = "ValidPassword123!";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = "Test@123",
            IsDeactivated = false
        };

        // Return user on search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // CheckPasswordSignInAsync needs to return a success
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(SignInResult.Success);

        // UpdateUserAsync needs to return success after test user is saved.
        _userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.DeactivateAccountAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True); // Verify deactivation returned true

            // Check that boolean value of user IsDeactivated field returns true
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == true)), Times.Once);
            
            // Check service calls were made properly.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, password, false), Times.Once);
        });
    }

    [Test]
    public async Task DeactivateAccountAsync_WithValidCredentials_SetsItsDeactivatedFlag()
    {
        // Arrange
        string email = "test@example.com";
        string password = "ValidPassword123!";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = false
        };

        // Return user on search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // CheckPasswordSignInAsync needs to return a success
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(SignInResult.Success);

        // UpdateUserAsync needs to return a success
        _userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.DeactivateAccountAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Check that userManagerMock for updating the user item was called.
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == true)), Times.Once);

            // Check the IsDeactivated field itself returns true
            Assert.That(user.IsDeactivated, Is.True, "User was not set to deactivated after valid authService call.");
        });
    }

    [Test]
    public async Task DeactivateAccountAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        string email = "test@example.com";
        string wrongPassword = "Test@123";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = false
        };

        // Return user on search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // CheckPasswordSignInAsync needs to return a failure for the wrong password
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        var result = await _authService.DeactivateAccountAsync(email, wrongPassword);

        // Assert
        Assert.Multiple(() =>
        {
            // Assert it returns false.
            Assert.That(result, Is.False);

            // Make sure that the call properly returned false, and never got to
            //      calling UpdateAsync.
            _userManagerMock.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);

            // Check the mocked service calls.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false), Times.Once);
        });
    }

    [Test]
    public async Task DeactivateAccountAsync_WithWrongPassword_DoesNotDeactivate()
    {
        // Arrange
        string email = "test@example.com";
        string wrongPassword = "WrongPassword!";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = false
        };

        // Return user on search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // CheckPasswordSignInAsync should return a failure for the wrong password
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false))
            .ReturnsAsync(SignInResult.Failed);

        // Act
        await _authService.DeactivateAccountAsync(email, wrongPassword);

        // Assert
        Assert.Multiple(() =>
        {
            // Assert the main claim.
            Assert.That(user.IsDeactivated, Is.False, "User was deactivated despite invalid authService call.");
            
            // Checking mocked services were called.
            _userManagerMock.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false), Times.Once);

        });
    }

    [Test]
    public async Task DeactivateAccountAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        string invalidEmail = "nonexistent@example.com";
        string password = "TestPass@135";

        // Set up userManagerMock to return null when called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(invalidEmail))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _authService.DeactivateAccountAsync(invalidEmail, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Check that DeactivateAccountAsync return value was false
            Assert.That(result, Is.False, "DeactivateAccountAsync deactivated a non-existent user.");

            // Check the service calls for accuracy.
            _userManagerMock.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
            _userManagerMock.Verify(um => um.FindByEmailAsync(invalidEmail), Times.Once);
        });
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithDeactivatedAccount_ReturnsFalse()
    {
        // Arrange
        string email = "test@example.com";
        string password = "Test@123";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return the user on an email search.
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Check that the result returns false.
            Assert.That(result, Is.False, "ValidateCredentialsAsync validated a deactivated account.");

            // Check the service calls. _signInManagerMock should never be called.
            _signInManagerMock.Verify(um => um.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);

            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
        });
    }
// == ReactivateAccountAsync Tests ==

    [Test]
    public async Task ReactivateAccountAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange - initialize a deactivated user.
        string email = "test@example.com";
        string password = "Test@123";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);

        // Return success for password sign-in
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(SignInResult.Success);

        // Return success for UpdateAsync call on the user.
        _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _authService.ReactivateAccountAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Check the end boolean value
            Assert.That(result, Is.True);

            // Check that the service calls to the mocks were used.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, password, false), Times.Once);

            // Verify that UpdateAsync was called with a non-deactive user.
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == false)), Times.Once);
        });
    }

    [Test]
    public async Task ReactivateAccountAsync_WithValidCredentials_SetsIsDeactivatedToFalse()
    {
        // Arrange. Create a de-activated account:
        string email = "reactivate2@example.com";
        string password = "Test@123!";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);

        // Return success for password sign-in
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password, false))
                .ReturnsAsync(SignInResult.Success);

        // Return success for UpdateAsync call on the user.
        _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);


        // Act
        await _authService.ReactivateAccountAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Check deactivated user status.
            Assert.That(user.IsDeactivated, Is.False, "Could not reactivate a valid deactivated user account.");

            // Check that the service calls were made properly.
            // UpdateAsync should be called with non-deactivated user.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, password, false), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == false)), Times.Once);
        });
    }

    [Test]
    public async Task ReactivateAccountAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        string wrongPassword = "WrongPassword!";
        string email = "reactivate3@example.com";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);

        // Return failure for wrongPassword sign-in attempt.
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<string>(),
            It.IsAny<bool>()
            )).ReturnsAsync(SignInResult.Failed);

        // UpdateAsync should never be called,
        //  should return false due to the guard statement.

        // Act
        var result = await _authService.ReactivateAccountAsync(email, wrongPassword);

        // Assert
        Assert.Multiple(() =>
        {
            // Assert false return, and that user is still deactivated.
            Assert.That(result, Is.False);
            Assert.That(user.IsDeactivated, Is.True, "User was Reactivated with incorrect password.");

            // Check that the services were called properly
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false), Times.Once);
        });
    }

    [Test]
    public async Task ReactivateAccountAsync_WithWrongPassword_AccountRemainsDeactivated()
    {
        // Arrange
        string wrongPassword = "WrongPassword!";
        string email = "reactivate4@example.com";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);

        // Return failure for wrongPassword sign-in attempt.
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false))
                .ReturnsAsync(SignInResult.Failed);

        // Act
        await _authService.ReactivateAccountAsync(email, wrongPassword);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(user.IsDeactivated, Is.True, "Attempt to reactivate user with invalid password was successful.");

            // Check the service calls for accuracy.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, wrongPassword, false), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == false)), Times.Never);
        });
    }

    [Test]
    public async Task ReactivateAccountAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Assert
        string invalidEmail = "nonexistent@example.com";
        string password = "TestPass@135";

        // Set up userManagerMock to return null when called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(invalidEmail))
            .ReturnsAsync((ApplicationUser)null!);

        // Act
        var result = await _authService.ReactivateAccountAsync(invalidEmail, password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.False, "Nonexistent user returned true with Reactivation.");

            // Check the service calls.
            _userManagerMock.Verify(um => um.FindByEmailAsync(invalidEmail), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == false)), Times.Never);
        });
    }

    [Test]
    public async Task ReactivateAccountAsync_WithActiveAccount_ReturnsFalse()
    {
        // Arrange - register but don't deactivate
        string email = "reactivate???@example.com";
        string password = "Test@123!";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = false // Should be active user
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user);

        // Act
        var result = await _authService.ReactivateAccountAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            // Assert end condition
            Assert.That(result, Is.False, "Should not be able to reactivate an already active account");

            // Check the service calls.
            _userManagerMock.Verify(um => um.FindByEmailAsync(email), Times.Once);
            _userManagerMock.Verify(um => um.UpdateAsync(It.Is<ApplicationUser>(u => u.IsDeactivated == false)), Times.Never);
        });
    }

    [Test]
    public async Task ValidateCredentialsAsync_AfterReactivation_ReturnsTrue()
    {
        // Arrange - register deactive account
        string email = "reactivate5@example.com";
        string password = "Test@123!";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user);

        // Return success for password sign-in
        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string>(), false))
                .ReturnsAsync(SignInResult.Success);

        // Return success for UpdateAsync call on the user.
        _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

        await _authService.ReactivateAccountAsync(email, password);  // Reactivate.

        // Act
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.True, "Should be able to login after reactivation");
            Assert.That(user.IsDeactivated, Is.False, "User reactivation was not successful.");

            // Verify UpdateAsync was called once during reactivation
            _userManagerMock.Verify(um => um.UpdateAsync(user), Times.Once);
            
            // Verify CheckPasswordSignInAsync was called twice
            //              Once for reactivation, once for validation
            _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(user, password, false), Times.Exactly(2));
        });
    }
}

/// Fake subclass, to bypass (and therefore Mock) the _signInManager constructor.
public class FakeSignInManager : SignInManager<ApplicationUser>
{
    public FakeSignInManager(UserManager<ApplicationUser> userManager) 
        : base(userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<ApplicationUser>>>(),
            Mock.Of<IAuthenticationSchemeProvider>())
    { }
}