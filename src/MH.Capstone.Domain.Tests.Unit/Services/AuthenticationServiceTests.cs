using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.Services.Abstraction;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using AuthenticationService = MH.Capstone.Domain.Services.AuthenticationService;
using IAuthenticationService = MH.Capstone.Domain.Services.Abstraction.IAuthenticationService;

namespace MH.Capstone.Domain.Tests.Unit.Services;

[TestFixture]
public class AuthenticationServiceTests
{
    #region TestOverhead

    private Mock<UserManager<ApplicationUser>> _userManagerMock;

    private Mock<SignInManager<ApplicationUser>> _signInManagerMock;
    private Mock<INotificationService> _notificationServiceMock;

    // MS already has an AuthenticationService, and the compiler needs to know
    // which one to use. So we bring out the full filepath here
    private IAuthenticationService _authService;
    private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;

    [SetUp]
    public void Setup()
    {
        // Register userManagerMock with null placeholders.
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // Use Fake class from bottom of file to initialize _signInManagerMock object.
        //var fakeSignInManager = new FakeSignInManager(_userManagerMock.Object);
        _signInManagerMock = new Mock<SignInManager<ApplicationUser>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            NullLogger<SignInManager<ApplicationUser>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>()
        );

        _notificationServiceMock = new Mock<INotificationService>();
        _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();

        // Register AuthenticationService
        _authService = new AuthenticationService(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _notificationServiceMock.Object,
            NullLogger<AuthenticationService>.Instance,
            _userRepoMock.Object
        );
    }

    private void AssertAllMockVerifySetups()
    {
        _userManagerMock.VerifyAll();
        //_userManagerMock.VerifyNoOtherCalls();

        _signInManagerMock.VerifyAll();
        //_signInManagerMock.VerifyNoOtherCalls();

        _notificationServiceMock.VerifyAll();
        _notificationServiceMock.VerifyNoOtherCalls();
    }

    #endregion

    #region RegisterUserAsyncTests

    [Test]
    public async Task RegisterUserAsync_WithValidData_CreatesUserAndSendsNotif()
    {
        // ARRANGE - Set up our test data ******************
        const string email = "newuser@example.com";
        const string password = "Test@123!";

        // Set up user manager to return Success with CreateAsync call
        _userManagerMock.Setup(x => x.CreateAsync(
                It.Is<ApplicationUser>(u => string.Equals(u.Email, email)), password))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        // Set up role assignment for user manager mock
        _userManagerMock.Setup(x => x.AddToRoleAsync(
                It.Is<ApplicationUser>(u => string.Equals(u.Email, email)), "User"))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        _notificationServiceMock.Setup(s => s.SendNotificationAsync(
            It.IsAny<Notification>())).Verifiable(Times.Once);

        // ACT - Try to register the user ********************
        var result = await _authService.RegisterUserAsync(email, password);

        // ASSERT - Verify user was created ******************
        Assert.That(result, Is.True, "Registration should succeed");
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task RegisterUserAsync_WithDisplayName_SetsDisplayNameOnUser()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Test@123!";
        const string displayName = "Jane Doe";

        _userManagerMock.Setup(x => x.CreateAsync(
                It.Is<ApplicationUser>(u =>
                    string.Equals(u.Email, email) &&
                    string.Equals(u.DisplayName, displayName)), password))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        _userManagerMock.Setup(x => x.AddToRoleAsync(
                It.Is<ApplicationUser>(u => string.Equals(u.Email, email)), "User"))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        _notificationServiceMock.Setup(s => s.SendNotificationAsync(
            It.IsAny<Notification>())).Verifiable(Times.Once);

        // Act
        var result = await _authService.RegisterUserAsync(email, password, displayName);

        // Assert
        Assert.That(result, Is.True, "Registration with display name should succeed");
        AssertAllMockVerifySetups();
    }

    #endregion
    #region ValidateCredentialsAsyncTests

    [Test]
    public async Task ValidateCredentialsAsync_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange - First create a user
        const string email = "testuser@example.com";
        const string password = "Test@123!";

        var user = new ApplicationUser 
        { 
            Email = email, 
            UserName = email,
            IsDeactivated = false // Active account
        };

        // userManagerMock should return this account when it is searched for
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        // signInManagerMock should return successful SignInResult when password is checked
        _signInManagerMock.Setup(sm => 
                sm.CheckPasswordSignInAsync(user, password, false))
            .ReturnsAsync(SignInResult.Success).Verifiable(Times.Once);

        // Act - Try to validate with correct credentials
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.That(result, Is.True, "Valid credentials should return true");
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange - Create a user
        const string email = "testuser2@example.com";
        const string wrongPassword = "WrongPass@123!";

        var user = new ApplicationUser 
        { 
            Email = email, 
            UserName = email,
            IsDeactivated = false // Active account
        };

        // _userManagerMock should return this user when searched by email
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        // Set up _signInManagerMock to return a failure with wrongPassword parameter
        _signInManagerMock.Setup(sm => 
                sm.CheckPasswordSignInAsync(user, wrongPassword, false))
            .ReturnsAsync(SignInResult.Failed).Verifiable(Times.Once);

        // Act - Try to validate with wrong password
        var result = await _authService.ValidateCredentialsAsync(email, wrongPassword);

        // Assert - Should return false
        Assert.That(result, Is.False, "Invalid password should return false");
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ValidateCredentialsAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        const string email = "nonexistent@example.com";
        const string password = "Test@123!";

        // _userManagerMock should NOT return this user when searched by email
        // Return null.
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act - Try to validate non-existent user
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert - Should return false
        Assert.That(result, Is.False, "Non-existent user should return false");
        AssertAllMockVerifySetups();

        // Special verification to ensure that the sign-in manager was never called, as the user doesn't exist.
        _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(
                It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Test]
    public async Task ValidateCredentialsAsync_DeactivatedUser_ReturnsFalse()
    {
        // Arrange - register deactivate account
        const string email = "reactivate5@example.com";
        const string password = "Test@123!";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        // Act
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifySetups();

        // Special verification to ensure that the sign-in manager was never called,
        // as the account is deactivated and the guard statement should have prevented it.
        _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), 
            Times.Never);
    }

    #endregion
    #region GenerateEmailConfirmationTokenAsyncTests

    [Test]
    public async Task GenerateEmailConfirmationTokenAsync_WithExistingUser_ReturnsToken()
    {
        // Arrange
        const string email = "confirm@example.com";
        const string expectedToken = "email-confirm-token";
        var user = new ApplicationUser { Email = email, UserName = email };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);
        _userManagerMock.Setup(um => um.GenerateEmailConfirmationTokenAsync(user))
            .ReturnsAsync(expectedToken).Verifiable(Times.Once);

        // Act
        var result = await _authService.GenerateEmailConfirmationTokenAsync(email);

        // Assert
        Assert.That(result, Is.EqualTo(expectedToken));
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task GenerateEmailConfirmationTokenAsync_WithNonExistentUser_ReturnsNull()
    {
        // Arrange
        const string email = "nobody@example.com";

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act
        var result = await _authService.GenerateEmailConfirmationTokenAsync(email);

        // Assert
        Assert.That(result, Is.Null);
        AssertAllMockVerifySetups();
        _userManagerMock.Verify(um =>
            um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion
    #region ConfirmEmailAsyncTests

    [Test]
    public async Task ConfirmEmailAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        const string email = "confirm@example.com";
        const string token = "valid-confirm-token";
        var user = new ApplicationUser { Email = email, UserName = email };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);
        _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, token))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        // Act
        var result = await _authService.ConfirmEmailAsync(email, token);

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ConfirmEmailAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        const string email = "confirm@example.com";
        const string badToken = "invalid-token";
        var user = new ApplicationUser { Email = email, UserName = email };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);
        _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, badToken))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }))
            .Verifiable(Times.Once);

        // Act
        var result = await _authService.ConfirmEmailAsync(email, badToken);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ConfirmEmailAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        const string email = "nobody@example.com";
        const string token = "some-token";

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act
        var result = await _authService.ConfirmEmailAsync(email, token);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifySetups();
        _userManagerMock.Verify(um =>
            um.ConfirmEmailAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()), Times.Never);
    }

    #endregion
    #region ResetPasswordWithTokenAsyncTests

    [Test]
    public async Task ResetPasswordWithTokenAsync_WithValidToken_ReturnsTrue()
    {
        // Arrange
        const string email = "tokenreset@example.com";
        const string token = "valid-reset-token";
        const string newPassword = "NewPass@456!";

        var user = new ApplicationUser { Email = email, UserName = email };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        _userManagerMock.Setup(um => um.ResetPasswordAsync(user, token, newPassword))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        // Act
        var result = await _authService.ResetPasswordWithTokenAsync(email, token, newPassword);

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ResetPasswordWithTokenAsync_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        const string email = "tokenreset@example.com";
        const string badToken = "expired-or-invalid-token";
        const string newPassword = "NewPass@456!";

        var user = new ApplicationUser { Email = email, UserName = email };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        _userManagerMock.Setup(um => um.ResetPasswordAsync(user, badToken, newPassword))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Invalid token." }))
            .Verifiable(Times.Once);

        // Act
        var result = await _authService.ResetPasswordWithTokenAsync(email, badToken, newPassword);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ResetPasswordWithTokenAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        const string email = "nobody@example.com";
        const string token = "some-token";
        const string newPassword = "NewPass@456!";

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act
        var result = await _authService.ResetPasswordWithTokenAsync(email, token, newPassword);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifySetups();

        _userManagerMock.Verify(um =>
            um.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    #endregion
    #region ResetPasswordAsyncTests

    [Test]
    public async Task ResetPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        const string invalidEmail = "nonexistent@example.com";
        const string newPassword = "NewPass@456!";

        // Set up userManagerMock to return null when called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(invalidEmail))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act - Try to reset password for non-existent user
        var result = await _authService.ResetPasswordAsync(invalidEmail, newPassword);

        // Assert - Should return false, and not try to generate a reset token.
        Assert.That(result, Is.False, "Reset should fail for non-existent user");
        AssertAllMockVerifySetups();

        // Check that a reset token WAS NOT created for the invalid user.
        _userManagerMock.Verify(um => um.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public void ResetPasswordAsync_WithInvalidPassword_ThrowsArgumentException()
    {
        // Arrange - Create a user first
        const string email = "resetuser4@example.com";
        const string invalidPassword = "weak"; // Too short, no symbol, no digit

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            Id = Guid.NewGuid().ToString()
        };

        // Act: throws the exception.
            var ex = Assert.ThrowsAsync<ArgumentException>(() => 
                _authService.ResetPasswordAsync(email, invalidPassword));

        // Assert
        Assert.That(ex.Message, Does.Contain("does not meet the policy standards"),
            "Exception message should match the exception message in AuthenticationService.cs.");

        // Verify that the invalid password argument is the argument throwing the exception.
        Assert.That(ex.ParamName, Is.EqualTo("newPassword"),
            "The exception should identify 'newPassword' (locally defined as invalidPassword) " +
            "as the invalid parameter.");
        AssertAllMockVerifySetups();

        // Check that the defined user was never returned by the search,
        //      due to the invalid password being caught.
        _userManagerMock.Verify(um => um.FindByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    #endregion
    #region IsPasswordValidTests

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

    #endregion
    #region DeactivateAccountAsyncTests

    [Test]
    public async Task DeactivateAccountAsync_WithValidCredentials_ReturnsTrueAndSetsItsDeactivatedFlag()
    {
        // Arrange
        const string email = "test@example.com";

        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = false
        };

        // Return user on search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        // UpdateUserAsync needs to return a success
        _userManagerMock.Setup(um => um.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        // Act
        var result = await _authService.DeactivateAccountAsync(email);

        // Assert
        Assert.That(user.IsDeactivated, Is.True, "User was not set to deactivated after valid authService call.");
        Assert.That(result, Is.True);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task DeactivateAccountAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Arrange - Use an email that doesn't exist
        const string invalidEmail = "nonexistent@example.com";
        //const string password = "TestPass@135";

        // Set up userManagerMock to return null when called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(invalidEmail))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act
        var result = await _authService.DeactivateAccountAsync(invalidEmail);

        // Assert
        Assert.That(result, Is.False, "DeactivateAccountAsync deactivated a non-existent user.");
        AssertAllMockVerifySetups();

        // Special verification to ensure that the user manager Update method was never called, as the user doesn't exist.
        _userManagerMock.Verify(um => um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion
    #region ValidateCredentialsAsyncTests

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
            .ReturnsAsync(user).Verifiable(Times.Once);

        // Act
        var result = await _authService.ValidateCredentialsAsync(email, password);

        // Assert
        Assert.That(result, Is.False, "ValidateCredentialsAsync validated a deactivated account.");
        AssertAllMockVerifySetups();

        // Special verification to ensure that the sign-in manager was never called,
        // as the account is deactivated and the guard statement should have prevented it.
        _signInManagerMock.Verify(um => um.CheckPasswordSignInAsync(
            It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    #endregion
    #region ReactivateAccountAsyncTests

    [Test]
    public async Task ReactivateAccountAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange - initialize a deactivated user.
        const string email = "test@example.com";
        const string password = "Test@123";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user).Verifiable(Times.Once);

        // Return success for password sign-in
        _signInManagerMock.Setup(sm => 
                sm.CheckPasswordSignInAsync(user, password, It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success).Verifiable(Times.Once);

        // Return success for UpdateAsync call on the user.
        _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);

        // Act
        var result = await _authService.ReactivateAccountAsync(email, password);

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ReactivateAccountAsync_WithValidCredentials_SetsIsDeactivatedToFalse()
    {
        // Arrange. Create a de-activated account:
        const string email = "reactivate2@example.com";
        const string password = "Test@123!";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user).Verifiable(Times.Once);

        // Return success for password sign-in
        _signInManagerMock.Setup(sm => 
                sm.CheckPasswordSignInAsync(user, password, It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Success).Verifiable(Times.Once);

        // Return success for UpdateAsync call on the user.
        _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success).Verifiable(Times.Once);


        // Act
        await _authService.ReactivateAccountAsync(email, password);

        // Assert
        Assert.That(user.IsDeactivated, Is.False, "Could not reactivate a valid deactivated user account.");
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task ReactivateAccountAsync_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        const string wrongPassword = "WrongPassword!";
        const string email = "reactivate3@example.com";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user).Verifiable(Times.Once);

        // Return failure for wrongPassword sign-in attempt.
        _signInManagerMock.Setup(sm => 
            sm.CheckPasswordSignInAsync(user, wrongPassword, It.IsAny<bool>()))
            .ReturnsAsync(SignInResult.Failed).Verifiable(Times.Once);
        

        // Act
        var result = await _authService.ReactivateAccountAsync(email, wrongPassword);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(user.IsDeactivated, Is.True, "User was Reactivated with incorrect password.");
        AssertAllMockVerifySetups();

        // Special verification to ensure that the user manager Update method was never called, as the user password was invalid.
        _userManagerMock.Verify(um =>
            um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithWrongPassword_AccountRemainsDeactivated()
    {
        // Arrange
        const string wrongPassword = "WrongPassword!";
        const string email = "reactivate4@example.com";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = true
        };

        // Return user object with email search
        _userManagerMock.Setup(um => um.FindByEmailAsync(email)).ReturnsAsync(user).Verifiable(Times.Once);

        // Return failure for wrongPassword sign-in attempt.
        _signInManagerMock.Setup(sm => 
                sm.CheckPasswordSignInAsync(user, wrongPassword, It.IsAny<bool>()))
                .ReturnsAsync(SignInResult.Failed).Verifiable(Times.Once);

        // Act
        var result = await _authService.ReactivateAccountAsync(email, wrongPassword);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(user.IsDeactivated, Is.True, "Attempt to reactivate user with invalid password was successful.");
        AssertAllMockVerifySetups();
        
        // Special verification to ensure that the user manager Update method was never called, as the user password was invalid.
        _userManagerMock.Verify(um =>
            um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithNonexistentUser_ReturnsFalse()
    {
        // Assert
        const string invalidEmail = "nonexistent@example.com";
        const string password = "TestPass@135";

        // Set up userManagerMock to return null when called to search
        _userManagerMock.Setup(um => um.FindByEmailAsync(invalidEmail))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act
        var result = await _authService.ReactivateAccountAsync(invalidEmail, password);

        // Assert
        Assert.That(result, Is.False, "Nonexistent user returned true with Reactivation.");
        AssertAllMockVerifySetups();

        // Special verification to ensure that the user manager Update method was never called, as the user doesn't exist.
        _userManagerMock.Verify(um => 
            um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Test]
    public async Task ReactivateAccountAsync_WithActiveAccount_ReturnsFalse()
    {
        // Arrange - register but don't deactivate
        const string email = "reactivate???@example.com";
        const string password = "Test@123!";
        var user = new ApplicationUser
        {
            Email = email,
            UserName = email,
            IsDeactivated = false // Should be active user
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        // Act
        var result = await _authService.ReactivateAccountAsync(email, password);

        // Assert
        Assert.That(result, Is.False, "Should not be able to reactivate an already active account");
        AssertAllMockVerifySetups();

        // Special verification to ensure that the user manager Update method was never called,
        // as the account is not deactivated and the guard statement should have prevented it.
        _userManagerMock.Verify(um => 
            um.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    #endregion
    #region CheckPasswordAsync Tests

    [Test]
    public async Task CheckPasswordAsync_WithValidCredentials_ReturnsTrue()
    {
        // Arrange
        const string password = "Test123!@#";
        var user = new ApplicationUser
        {
            Email = "checkpass@test.com",
            UserName = "checkpass@test.com",
            IsDeactivated = false
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(user.Email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password, 
            It.IsAny<bool>())).ReturnsAsync(SignInResult.Success).Verifiable(Times.Once);

        // Act
        var result = await _authService.CheckPasswordAsync(user.Email, password);

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task CheckPasswordAsync_WithInvalidPassword_ReturnsFalse()
    {
        // Arrange
        const string password = "TheWrongPwd!";
        var user = new ApplicationUser
        {
            Email = "checkpass@test.com",
            UserName = "checkpass@test.com",
            IsDeactivated = false
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(user.Email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password,
            It.IsAny<bool>())).ReturnsAsync(SignInResult.Failed).Verifiable(Times.Once);

        // Act
        var result = await _authService.CheckPasswordAsync(user.Email, password);

        // Assert
        Assert.That(result, Is.False);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task CheckPasswordAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        const string email = "nouser@test.com";
        var password = "IDoNotExist!";

        _userManagerMock.Setup(um => um.FindByEmailAsync(email))
            .ReturnsAsync(value: null!).Verifiable(Times.Once);

        // Act
        var result = await _authService.CheckPasswordAsync(email, password);

        // Assert
        Assert.That(result, Is.False);
        // Special verification to ensure that the sign-in manager was never called, as the user doesn't exist.
        _signInManagerMock.Verify(sm => sm.CheckPasswordSignInAsync(
            It.IsAny<ApplicationUser>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
        AssertAllMockVerifySetups();
    }

    [Test]
    public async Task CheckPasswordAsync_WithDeactivatedAccount_StillReturnsTrue()
    {
        // Arrange
        const string password = "Test123!@#";
        var user = new ApplicationUser
        {
            Email = "checkpass@test.com",
            UserName = "checkpass@test.com",
            IsDeactivated = true
        };

        _userManagerMock.Setup(um => um.FindByEmailAsync(user.Email))
            .ReturnsAsync(user).Verifiable(Times.Once);

        _signInManagerMock.Setup(sm => sm.CheckPasswordSignInAsync(user, password,
            It.IsAny<bool>())).ReturnsAsync(SignInResult.Success).Verifiable(Times.Once);

        // Act
        var result = await _authService.CheckPasswordAsync(user.Email, password);

        // Assert
        Assert.That(result, Is.True);
        AssertAllMockVerifySetups();
    }

    #endregion
}