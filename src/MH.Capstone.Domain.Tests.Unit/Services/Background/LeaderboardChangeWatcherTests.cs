using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using MH.Capstone.Domain.DataModels;
using MH.Capstone.Domain.Services.Background;
using NUnit.Framework;

namespace MH.Capstone.Domain.Tests.Unit.Services.Background
{
    [TestFixture]
    [Parallelizable]
    [ExcludeFromCodeCoverage]
    public class LeaderboardChangeWatcherTests
    {
        private static ApplicationUser User(string id, string name, int points) =>
            new() { Id = id, DisplayName = name, Points = points };

        [Test]
        public void DetectChanges_EmptyCurrentAndEmptyPrevious_ReturnsNoUpdates()
        {
            var result = LeaderboardChangeWatcher
                .DetectChanges(new List<ApplicationUser>(), new Dictionary<string, int>())
                .ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectChanges_NewUserNotInPrevious_EmitsUpdate()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 100) };
            var previous = new Dictionary<string, int>();

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].UserId, Is.EqualTo("u1"));
            Assert.That(result[0].Points, Is.EqualTo(100));
            Assert.That(result[0].Rank, Is.EqualTo(1));
        }

        [Test]
        public void DetectChanges_PointsUnchanged_EmitsNoUpdate()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 100) };
            var previous = new Dictionary<string, int> { ["u1"] = 100 };

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void DetectChanges_PointsChanged_EmitsUpdateWithNewValue()
        {
            var current = new List<ApplicationUser> { User("u1", "Alex", 150) };
            var previous = new Dictionary<string, int> { ["u1"] = 100 };

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Points, Is.EqualTo(150));
        }

        [Test]
        public void DetectChanges_MixedChanges_EmitsOnlyChangedAndNewEntries()
        {
            var current = new List<ApplicationUser>
            {
                User("u1", "Alex",     150), // changed
                User("u2", "Patricia", 100), // unchanged
                User("u3", "Lily",      80)  // new
            };
            var previous = new Dictionary<string, int>
            {
                ["u1"] = 100,
                ["u2"] = 100
            };

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result.Select(r => r.UserId), Is.EquivalentTo(new[] { "u1", "u3" }));
        }

        [Test]
        public void DetectChanges_RankReflectsPositionInCurrentList()
        {
            var current = new List<ApplicationUser>
            {
                User("u1", "Alex",     150),
                User("u2", "Patricia", 100),
                User("u3", "Lily",      80)
            };
            var previous = new Dictionary<string, int>();

            var result = LeaderboardChangeWatcher.DetectChanges(current, previous).ToList();

            Assert.That(result.Single(r => r.UserId == "u1").Rank, Is.EqualTo(1));
            Assert.That(result.Single(r => r.UserId == "u2").Rank, Is.EqualTo(2));
            Assert.That(result.Single(r => r.UserId == "u3").Rank, Is.EqualTo(3));
        }
    }
}
