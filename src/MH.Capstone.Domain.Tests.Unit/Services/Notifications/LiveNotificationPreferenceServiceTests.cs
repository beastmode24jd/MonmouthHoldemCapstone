using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using MH.Capstone.Domain.DataAccess;
using MH.Capstone.Domain.DataAccess.Repositories;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Notifications;
using Moq;
using NUnit.Framework;

namespace MH.Capstone.Domain.Tests.Unit.Services.Notifications
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class LiveNotificationPreferenceServiceTests
    {
        private const string TestUserId = "test-user-id";
        private Mock<IRepository<LiveNotificationPreference, ApplicationDbContext>> _mockRepo = null!;

        [SetUp]
        public void SetUp()
        {
            _mockRepo = new Mock<IRepository<LiveNotificationPreference, ApplicationDbContext>>();
        }

        private LiveNotificationPreferenceService CreateSut() => new(_mockRepo.Object);

        #region IsEnabledAsync

        [Test]
        public async Task IsEnabledAsync_NoStoredPreference_ReturnsTrueByDefault()
        {
            _mockRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<LiveNotificationPreference, bool>>>()))
                .ReturnsAsync(new List<LiveNotificationPreference>().AsQueryable());

            var result = await CreateSut().IsEnabledAsync(TestUserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsEnabledAsync_StoredTrue_ReturnsTrue()
        {
            var stored = new List<LiveNotificationPreference>
            {
                new() { UserId = TestUserId, LiveUpdatesEnabled = true }
            };
            _mockRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<LiveNotificationPreference, bool>>>()))
                .ReturnsAsync(stored.AsQueryable());

            var result = await CreateSut().IsEnabledAsync(TestUserId);

            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsEnabledAsync_StoredFalse_ReturnsFalse()
        {
            var stored = new List<LiveNotificationPreference>
            {
                new() { UserId = TestUserId, LiveUpdatesEnabled = false }
            };
            _mockRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<LiveNotificationPreference, bool>>>()))
                .ReturnsAsync(stored.AsQueryable());

            var result = await CreateSut().IsEnabledAsync(TestUserId);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task IsEnabledAsync_NullOrWhitespaceUserId_ReturnsTrue()
        {
            var result = await CreateSut().IsEnabledAsync("   ");

            Assert.That(result, Is.True);
        }

        #endregion

        #region SetEnabledAsync

        [Test]
        public async Task SetEnabledAsync_NoExistingRow_InsertsNewWithGivenValue()
        {
            _mockRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<LiveNotificationPreference, bool>>>()))
                .ReturnsAsync(new List<LiveNotificationPreference>().AsQueryable());

            LiveNotificationPreference? captured = null;
            _mockRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<LiveNotificationPreference>()))
                .Callback<LiveNotificationPreference>(p => captured = p)
                .ReturnsAsync((LiveNotificationPreference p) => p);

            await CreateSut().SetEnabledAsync(TestUserId, false);

            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.UserId, Is.EqualTo(TestUserId));
            Assert.That(captured.LiveUpdatesEnabled, Is.False);
        }

        [Test]
        public async Task SetEnabledAsync_ExistingRow_UpdatesValue()
        {
            var stored = new LiveNotificationPreference
            {
                UserId = TestUserId,
                LiveUpdatesEnabled = true
            };
            _mockRepo
                .Setup(r => r.GetAllAsync(It.IsAny<Expression<Func<LiveNotificationPreference, bool>>>()))
                .ReturnsAsync(new[] { stored }.AsQueryable());
            _mockRepo
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<LiveNotificationPreference>()))
                .ReturnsAsync((LiveNotificationPreference p) => p);

            await CreateSut().SetEnabledAsync(TestUserId, false);

            Assert.That(stored.LiveUpdatesEnabled, Is.False);
        }

        [Test]
        public void SetEnabledAsync_NullOrWhitespaceUserId_Throws()
        {
            var sut = CreateSut();

            Assert.ThrowsAsync<ArgumentException>(async () => await sut.SetEnabledAsync(null!, true));
            Assert.ThrowsAsync<ArgumentException>(async () => await sut.SetEnabledAsync("   ", true));
        }

        #endregion
    }
}
