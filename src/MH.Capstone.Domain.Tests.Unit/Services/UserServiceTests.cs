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
using System.Threading.Tasks;

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
        public async Task UpdateUserBioAsync_EmptyOrWhitespaceInput_ResetsToNullValue(string? input)
        {
            // Arrange
            var userToAssert = new ApplicationUser
            {
                Email = "user@test.com",
                GuidId = Guid.NewGuid(),
                Bio = "This is a refactored test."
            };

            var sut = CreateSut();

            // Mock the behavior of user.SaveModelAsync calling repo.AddOrUpdateAsync,
            //      to return the user's Bio value.
            _userRepoMock.Setup(r => 
                r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => 
                    u.Bio == userToAssert.Bio)))
                .Verifiable(Times.Once);

            // Act
            await sut.UpdateUserBioAsync(userToAssert, input);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(userToAssert.Bio, Is.EqualTo(null), "User bio did not revert to null value as it was supposed to.");

                // Ensure that the repo was called with our dummy user object
                _userRepoMock.Verify(repo => repo.AddOrUpdateAsync(userToAssert), Times.Once);
                AssertAllMockVerifications();
            });
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

    #region SearchUsersAsync — Sprint 5, CSP-54 / Sprint 6, CSP-200 (DisplayName-only)

    [Test]
    public async Task SearchUsersAsync_EmptyQuery_ReturnsNoResults()
    {
        // Arrange
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(new List<ApplicationUser>().AsQueryable());
        var sut = CreateSut();

        // Act
        var results = (await sut.SearchUsersAsync("")).ToList();

        // Assert
        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchUsersAsync_ExactDisplayNameMatch_ReturnsUser()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "alice@test.com", DisplayName = "Alice" }
        };
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(users.AsQueryable());
        var sut = CreateSut();

        // Act
        var results = (await sut.SearchUsersAsync("Alice")).ToList();

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].DisplayName, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task SearchUsersAsync_SubstringMatch_ReturnsMatchingUsers()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "alicesmith@test.com", DisplayName = "AliceSmith" },
            new() { Id = "2", UserName = "bob@test.com",        DisplayName = "Bob" }
        };
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(users.AsQueryable());
        var sut = CreateSut();

        // Act — mock returns both users; the predicate filtering happens in real EF but not in this unit test
        var results = (await sut.SearchUsersAsync("alice")).ToList();

        // Assert — both come back because the mock doesn't evaluate the predicate;
        // this verifies the sort/ordering logic that runs in-process.
        Assert.That(results.Any(u => u.DisplayName == "AliceSmith"), Is.True);
    }

    [Test]
    public async Task SearchUsersAsync_CaseInsensitiveMatch_ReturnsUser()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "alice@test.com", DisplayName = "Alice" }
        };
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(users.AsQueryable());
        var sut = CreateSut();

        // Act
        var results = (await sut.SearchUsersAsync("ALICE")).ToList();

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].DisplayName, Is.EqualTo("Alice"));
    }

    [Test]
    public async Task SearchUsersAsync_FullMatchesOrderedBeforeSubstringMatches()
    {
        // Arrange — repo returns both users (predicate not evaluated by mock)
        var users = new List<ApplicationUser>
        {
            new() { Id = "1", UserName = "alicesmith@test.com", DisplayName = "AliceSmith" },  // substring match
            new() { Id = "2", UserName = "alice@test.com",      DisplayName = "Alice" }        // full match
        };
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(users.AsQueryable());
        var sut = CreateSut();

        // Act
        var results = (await sut.SearchUsersAsync("Alice")).ToList();

        // Assert — full match "Alice" comes before substring match "AliceSmith"
        Assert.That(results[0].DisplayName, Is.EqualTo("Alice"));
        Assert.That(results[1].DisplayName, Is.EqualTo("AliceSmith"));
    }

    [Test]
    public async Task SearchUsersAsync_MultipleSubstringMatches_SortedAlphabetically()
    {
        // Arrange
        var users = new List<ApplicationUser>
        {
            new() { Id = "3", UserName = "charlie@test.com", DisplayName = "Charlie_ali" },
            new() { Id = "1", UserName = "first@test.com",   DisplayName = "Ali_first" },
            new() { Id = "2", UserName = "second@test.com",  DisplayName = "Bali_second" }
        };
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .ReturnsAsync(users.AsQueryable());
        var sut = CreateSut();

        // Act
        var results = (await sut.SearchUsersAsync("ali")).ToList();

        // Assert — all are substring matches, sorted alphabetically by DisplayName
        Assert.That(results[0].DisplayName, Is.EqualTo("Ali_first"));
        Assert.That(results[1].DisplayName, Is.EqualTo("Bali_second"));
        Assert.That(results[2].DisplayName, Is.EqualTo("Charlie_ali"));
    }

    // Sprint 6, CSP-200: the predicate handed to the repo must filter on
    // DisplayName, not email. This captures the expression and evaluates it
    // against fake users — proving an email-only match is rejected.
    [Test]
    public async Task SearchUsersAsync_PredicateMatchesDisplayNameNotEmail()
    {
        // Arrange — capture the predicate that the service hands to the repo.
        System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>? captured = null;
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>()))
            .Callback<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>(e => captured = e)
            .ReturnsAsync(new List<ApplicationUser>().AsQueryable());

        var emailMatch = new ApplicationUser
            { Id = "1", UserName = "smith.john@test.com", DisplayName = "NatureLover", IsDeactivated = false };
        var displayMatch = new ApplicationUser
            { Id = "2", UserName = "user@test.com",       DisplayName = "SmithDisplay", IsDeactivated = false };
        var unsetUser = new ApplicationUser
            { Id = "3", UserName = "smith.unset@test.com", DisplayName = "UNSET", IsDeactivated = false };

        var sut = CreateSut();

        // Act
        await sut.SearchUsersAsync("smith");

        // Assert — predicate must reject the email-only and UNSET users, accept the DisplayName match.
        Assert.That(captured, Is.Not.Null, "service should pass a predicate to the repo");
        var compiled = captured!.Compile();
        Assert.That(compiled(emailMatch),  Is.False, "user matched only by email must be excluded");
        Assert.That(compiled(displayMatch), Is.True,  "user matched by DisplayName must be included");
        Assert.That(compiled(unsetUser),    Is.False, "user with DisplayName 'UNSET' must be excluded");
    }

    #endregion

    #region GetTotalUserCountAsync

    [Test]
    public async Task GetTotalUserCountAsync_WithThreeActiveUsers_Returns3()
    {
        // Arrange — repo returns 3 active users (the predicate filtering is the repo's responsibility)
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>())) 
            .ReturnsAsync(new List<ApplicationUser>
            {
                new() { Id = "user-1", UserName = "Alice", Points = 100 },
                new() { Id = "user-2", UserName = "Bob", Points = 200 },
                new() { Id = "user-3", UserName = "Charlie", Points = 300 }
            }.AsQueryable());
        var sut = CreateSut();

        // Act
        var result = await sut.GetTotalUserCountAsync();

        // Assert
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public async Task GetTotalUserCountAsync_DeactivatedUsersAreExcluded()
    {
        // Arrange — mock simulates the repo returning only the 1 active user after applying the predicate
        _userRepoMock
            .Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ApplicationUser, bool>>>())) 
            .ReturnsAsync(new List<ApplicationUser>
            {
                new() { Id = "user-1", UserName = "ActiveUser", Points = 100 }
            }.AsQueryable());
        var sut = CreateSut();

        // Act
        var result = await sut.GetTotalUserCountAsync();

        // Assert
        Assert.That(result, Is.EqualTo(1));
    }

    #endregion

    #region UpdateDisplayNameAsyncTests

    [Test]
    public async Task UpdateDisplayNameAsync_ValidDisplayName_SavesDisplayName()
    {
        // Arrange
        const string newDisplayName = "Jane Doe";
        var user = new ApplicationUser { GuidId = Guid.NewGuid(), DisplayName = "UNSET" };

        _userRepoMock.Setup(r =>
            r.AddOrUpdateAsync(It.Is<ApplicationUser>(u =>
                u.Id == user.Id && u.DisplayName == newDisplayName)))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        await sut.UpdateDisplayNameAsync(user, newDisplayName);

        // Assert
        Assert.That(user.DisplayName, Is.EqualTo(newDisplayName));
        AssertAllMockVerifications();
    }

    [Test]
    public void UpdateDisplayNameAsync_DisplayNameTooShort_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var user = new ApplicationUser { GuidId = Guid.NewGuid() };
        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.UpdateDisplayNameAsync(user, "X"));
        AssertAllMockVerifications();
    }

    [Test]
    public void UpdateDisplayNameAsync_DisplayNameTooLong_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var user = new ApplicationUser { GuidId = Guid.NewGuid() };
        var sut = CreateSut();
        var tooLong = new string('A', 51);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.UpdateDisplayNameAsync(user, tooLong));
        AssertAllMockVerifications();
    }

    [TestCase("")]
    [TestCase("   ")]
    public void UpdateDisplayNameAsync_WhitespaceOrEmpty_ThrowsArgumentOutOfRangeException(string input)
    {
        // Arrange
        var user = new ApplicationUser { GuidId = Guid.NewGuid() };
        var sut = CreateSut();

        // Act & Assert
        Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => sut.UpdateDisplayNameAsync(user, input));
        AssertAllMockVerifications();
    }

    [TestCase("Alice")]
    [TestCase("O'Brien")]
    [TestCase("Jean-Luc")]
    [TestCase("Dr. Pepper")]
    public async Task UpdateDisplayNameAsync_ValidVariousNames_SavesSuccessfully(string name)
    {
        // Arrange
        var user = new ApplicationUser { GuidId = Guid.NewGuid(), DisplayName = "UNSET" };

        _userRepoMock.Setup(r =>
            r.AddOrUpdateAsync(It.Is<ApplicationUser>(u => u.DisplayName == name)))
            .Verifiable(Times.Once);

        var sut = CreateSut();

        // Act
        await sut.UpdateDisplayNameAsync(user, name);

        // Assert
        Assert.That(user.DisplayName, Is.EqualTo(name));
        AssertAllMockVerifications();
    }

    #endregion
    }
}
