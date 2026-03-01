using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services;
using MH.Capstone.Domain.Tools;
using MH.Capstone.Tests.SharedInternals;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics.CodeAnalysis;
using MH.Capstone.Domain.DataAccess.Repositories;

namespace MH.Capstone.Domain.Tests.Unit.Services
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class UserServiceTests
    {
        #region TestOverhead

        private Mock<UserManager<ApplicationUser>> _userManagerMock;
        private Mock<IRepository<ApplicationUser, ApplicationDbContext>> _userRepoMock;

        [SetUp]
        public void SetUp()
        {
            // This is only used for the UserManager Mock.
            // We have to mock it because it has complex dependencies that we don't want to set up for unit testing,
            // but it is only called internally by the UserService, so we don't need to worry about verifying or
            // setting up any of it's methods.
            var userStoreMock = new Mock<IUserStore<ApplicationUser>>();

            // We have to pass in nulls for all the other dependencies of UserManager,
            // but it won't cause any issues as we will be mocking the method outputs of this UserManager and
            // not calling any of the real methods that would use those dependencies.
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(userStoreMock.Object,
                null!, null!, null!, null!, null!, null!, null!, null!);

            _userRepoMock = new Mock<IRepository<ApplicationUser, ApplicationDbContext>>();
        }

        private UserService CreateSut() =>
            new UserService(_userManagerMock.Object, _userRepoMock.Object,
                NullLogger<UserService>.Instance);

        private void AssertAllMockVerifications()
        {

            // Asserts that the methods that were set up in the Moq were called in ways that we set up
            _userManagerMock.VerifyAll();
            _userRepoMock.VerifyAll();

            // Asserts that no other methods were called on the mocks, beyond what we set up in the test.
            // We do not call this for the UserManager as the internal implementation of UserManager calls
            // other methods that we haven't set up, and we don't want to have to set up every single method
            // just to use VerifyNoOtherCalls and cannot see all calls made by the MS team.
            _userRepoMock.VerifyNoOtherCalls();
        }

        #endregion
        #region UserExistsAsyncTests

        [Test]
        public async Task UserExistsAsync_UserWithEmailFound_ReturnsTrue()
        {
            // Arrange
            var userToExist = new ApplicationUser
            {
                Email = "existingUser@mail.net",
                GuidId = Guid.NewGuid()
            };

            _userManagerMock.Setup(um =>
                um.FindByEmailAsync(It.Is<string>(e => e == userToExist.Email))
            ).ReturnsAsync(userToExist).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var exists = await sut.UserExistsAsync(userToExist.Email);

            // Assert
            Assert.That(exists, Is.True, "Registered user should exist");
            AssertAllMockVerifications();
        }

        [Test]
        public async Task UserExistsAsync_UserWithEmailNotFound_ReturnsFalse()
        {
            // Arrange
            const string email = "nonexistent@example.com";

            _userManagerMock.Setup(um =>
                um.FindByEmailAsync(It.Is<string>(e => e == email))
            ).ReturnsAsync(value: null).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var exists = await sut.UserExistsAsync(email);

            // Assert
            Assert.That(exists, Is.False, "Unregistered user should not exist");
            AssertAllMockVerifications();
        }

        #endregion
        #region GetUserByEmailAsyncTests

        [Test]
        public async Task GetUserByEmailAsync_UserWithEmailFound_ReturnsUser()
        {
            // Arrange
            var userToExist = new ApplicationUser
            {
                Email = "existingUser@mail.net",
                GuidId = Guid.NewGuid()
            };

            _userManagerMock.Setup(um =>
                um.FindByEmailAsync(It.Is<string>(e => e == userToExist.Email))
            ).ReturnsAsync(userToExist).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var result = await sut.GetUserByEmailAsync(userToExist.Email);

            // Assert
            Assert.That(result, Is.EqualTo(userToExist));
            AssertAllMockVerifications();
        }

        [Test]
        public async Task GetUserByEmailAsync_UserWithEmailNotFound_ReturnsNull()
        {
            // Arrange
            const string email = "nonexistent@example.com";

            _userManagerMock.Setup(um =>
                um.FindByEmailAsync(It.Is<string>(e => e == email))
            ).ReturnsAsync(value: null).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var exists = await sut.UserExistsAsync(email);

            // Assert
            Assert.That(exists, Is.False, "Unregistered user should not exist");
            AssertAllMockVerifications();
        }

        #endregion
        #region GetUserByIdAsync

        [Test]
        public async Task GetUserByIdAsync_UserWithIdFound_ReturnsUser()
        {
            // Arrange
            var userToExist = new ApplicationUser
            {
                Email = "existingUser@mail.net",
                GuidId = Guid.NewGuid()
            };

            _userManagerMock.Setup(um =>
                um.FindByIdAsync(It.Is<string>(id => id == userToExist.Id))
            ).ReturnsAsync(userToExist).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var result = await sut.GetUserByIdAsync(userToExist.Id);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(userToExist));
            AssertAllMockVerifications();
        }

        [Test]
        public async Task GetUserByIdAsync_UserWithIdNotFound_ReturnsNull()
        {
            // Arrange
            var badId = Guid.NewGuid().ToString();

            _userManagerMock.Setup(um =>
                um.FindByIdAsync(It.Is<string>(id => id == badId))
            ).ReturnsAsync(value: null).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            var exists = await sut.GetUserByIdAsync(badId);

            // Assert
            Assert.That(exists, Is.Null);
            AssertAllMockVerifications();
        }

        #endregion
        #region UpdateUserProfileImageAsyncTests

        [Test]
        public async Task UpdateUserProfileImageAsync_UserExists_UpdatesImageContentAndType()
        {
            // Arrange
            var image = new FakeImageGenerator().GetValidImage();
            var imgContent = image.ToByteArray();
            var imgContentType = image.ContentType;
            var userToAssert = new ApplicationUser
            {
                Email = "test@me.com",
                GuidId = Guid.NewGuid(),
                ProfileImage = null,
                ProfileImageType = null
            };

            _userManagerMock.Setup(um =>
                um.FindByEmailAsync(It.Is<string>(e => e == userToAssert.Email))
            ).ReturnsAsync(userToAssert).Verifiable(Times.Once);

            _userRepoMock.Setup(r =>
                r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => 
                    u.GuidId == userToAssert.GuidId &&
                    u.ProfileImage == imgContent &&
                    u.ProfileImageType == imgContentType))
                ).Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            await sut.UpdateUserProfileImageAsync(userToAssert.Email, imgContent, imgContentType);

            // Assert
            AssertAllMockVerifications();
        }

        [Test]
        public async Task UpdateUserProfileImageAsync_UserDoesNotExist_DoesNotUpdate()
        {
            // Arrange
            const string badEmail = "nonexistent@example.com";

            _userManagerMock.Setup(um =>
                um.FindByEmailAsync(It.Is<string>(e => e == badEmail))
            ).ReturnsAsync(value: null).Verifiable(Times.Once);

            // Verify that UpdateAsync is never called since the user doesn't exist
            // We can't use VerifyNoOtherCalls here because internally UserManager calls
            // other methods we haven't set up, but we can at least verify that UpdateAsync is not called.
            _userRepoMock.Setup(r =>
                r.AddOrUpdateAsync(It.IsAny<ApplicationUser>())
                ).Verifiable(Times.Never);

            var sut = CreateSut();

            // Act
            await sut.UpdateUserProfileImageAsync(badEmail, [], string.Empty);

            // Assert
            AssertAllMockVerifications();
        }

        #endregion

        #region MyRegion

        [Test]
        public async Task UpdateUserBioAsync_ValidInput_SavesTextBio()
        {
            // Arrange
            const string bio = "I am John who works job at place";
            var userToAssert = new ApplicationUser
            {
                Email = "user@test.com",
                GuidId = Guid.NewGuid(),
                Bio = null
            };

            _userRepoMock.Setup(r => 
                r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => 
                    u.Id == userToAssert.Id &&
                    u.Bio == userToAssert.Bio)))
                .Verifiable(Times.Once);

            var sut = CreateSut();

            // Act
            await sut.UpdateUserBioAsync(userToAssert, bio);

            // Assert
            AssertAllMockVerifications();
        }

        // Bio of more than 250 char creates invalid object
        [Test]
        public void UpdateUserBioAsync_Over250CharInput_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                Email = "user@test.com",
                GuidId = Guid.NewGuid(),
                Bio = null
            };

            // String over 250 char (this is 251)
            string longBio = new string('x', 251);

            var sut = CreateSut();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => sut.UpdateUserBioAsync(userToAssert, longBio));
            AssertAllMockVerifications();
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("             ")]
        public void UpdateUserBioAsync_InvalidInput_ThrowsArgumentOutOfRangeException(string? invalidInput)
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                Email = "user@test.com",
                GuidId = Guid.NewGuid(),
                Bio = null
            };

            var sut = CreateSut();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(
                () => sut.UpdateUserBioAsync(userToAssert, invalidInput));
            AssertAllMockVerifications();
        }

        #endregion

        /* Technically not a UserProfileService.cs test, but tests the default value
           of the bio field in a user account. */
        [Test]
        public void ApplicationUser_Initialization_SetsDefaultBio()
        {
            // Arrange and Act
            // Initialize a generic user, without providing a bio.
            var user = new ApplicationUser();

            // Assert
            // Verify it matches the default string given in the data model.
            Assert.Multiple(() =>
            {
                Assert.That(user.Bio, Is.Null, "Default user bio was not set to null.");
                Assert.That(user.Bio!, Is.EqualTo(null));
            });
        }
    }
}
